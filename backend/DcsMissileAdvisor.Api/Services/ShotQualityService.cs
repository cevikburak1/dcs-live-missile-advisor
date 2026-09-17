using DcsMissileAdvisor.Api.Models;

namespace DcsMissileAdvisor.Api.Services;

public class ShotQualityService
{
    public ShotQualityResult Calculate(
        AircraftState aircraft,
        TargetState target,
        MissileProfile? missile,
        AircraftProfile? aircraftProfile)
    {
        var result = new ShotQualityResult
        {
            ActiveMissileProfile = missile
        };

        if (missile is null)
        {
            result.CanCalculate = false;
            var noSelection = string.IsNullOrWhiteSpace(aircraft.SelectedWeaponRawName);
            result.Explanation = noSelection ? "Selected weapon not reported by DCS" : "Missile profile not found";
            result.MissingDataFields.Add(noSelection ? "selectedWeapon" : "missileProfile");
            return result;
        }

        if (!target.SensorExportAvailable)
        {
            result.CanCalculate = false;
            result.Explanation = "Target lock export unavailable in this mission/server";
            result.MissingDataFields.Add("sensorExport");
            return result;
        }

        if (!target.IsLocked)
        {
            result.CanCalculate = false;
            result.Explanation = target.StatusMessage ?? "No target lock reported by DCS";
            result.MissingDataFields.Add("targetLock");
            return result;
        }

        var missing = CollectMissingFields(aircraft, target, missile, aircraftProfile);
        if (missing.Count > 0)
        {
            result.CanCalculate = false;
            result.MissingDataFields = missing;
            result.Explanation = "Insufficient data for PK calculation";
            return result;
        }

        var range = target.TargetRangeNm!.Value;
        var aspect = target.TargetAspectCategory;
        var closure = target.TargetClosureRateKnots!.Value;
        var ownMach = aircraft.OwnMach!.Value;
        var ownAlt = aircraft.OwnAltitudeFt;
        var targetAlt = target.TargetAltitudeFt;

        var basePk = ComputeRangeScore(missile, range);
        var modifiers = new List<(string Label, double Factor)>();

        modifiers.Add(("aspect", AspectModifier(missile, aspect)));
        modifiers.Add(("closure", ClosureModifier(missile, closure)));
        if (ownAlt.HasValue && targetAlt.HasValue)
            modifiers.Add(("altitude", AltitudeModifier(missile, ownAlt.Value, targetAlt.Value)));

        modifiers.Add(("mach", MachModifier(ownMach)));

        if (target.TargetIsJamming == true && missile.ChaffSensitivity > 0)
            modifiers.Add(("ECM", 1.0 - missile.ChaffSensitivity * 0.5));

        if (missile.NeedsRadarSupport && aircraftProfile is { HasBvrRadar: false })
            modifiers.Add(("radar", 0.0));

        if (IsInfrared(missile.MissileType) && missile.HighOffBoresight && aspect == TargetAspectCategory.Flanking)
            modifiers.Add(("HOBS", 1.1));

        var pk = basePk;
        var explanations = new List<string>();

        foreach (var (label, factor) in modifiers)
        {
            pk *= factor;
            if (factor < 0.85)
                explanations.Add($"{label} penalty");
            else if (factor > 1.05)
                explanations.Add($"{label} bonus");
        }

        if (range > missile.MaxEffectiveRangeNm)
            explanations.Add("Outside max effective range");
        if (range < missile.MinimumRangeNm)
            explanations.Add("Inside minimum range");
        if (range > missile.NoEscapeRangeNm && range <= missile.MaxEffectiveRangeNm)
            explanations.Add("Outside NEZ");
        if (target.TargetIsJamming == true)
            explanations.Add("ECM active");
        if (aspect == TargetAspectCategory.Beaming)
            explanations.Add("Target beaming");
        if (aspect == TargetAspectCategory.Cold && IsInfrared(missile.MissileType))
            explanations.Add("Cold aspect for IR missile");

        pk = Math.Clamp(pk, 0, 95);

        result.CanCalculate = true;
        result.EstimatedPkPercent = Math.Round(pk, 1);
        result.ShotCategory = CategorizeShot(pk, range, missile);
        result.Recommendation = Recommend(pk, range, aspect, target, missile);
        result.Explanation = explanations.Count > 0
            ? string.Join("; ", explanations)
            : "Favorable shot parameters";

        return result;
    }

    private static List<string> CollectMissingFields(
        AircraftState aircraft,
        TargetState target,
        MissileProfile missile,
        AircraftProfile? aircraftProfile)
    {
        var missing = new List<string>();

        if (!target.TargetRangeNm.HasValue) missing.Add("targetRange");
        if (!target.TargetAspectDeg.HasValue && target.TargetAspectCategory == TargetAspectCategory.Unknown)
            missing.Add("targetAspect");
        if (!target.TargetClosureRateKnots.HasValue) missing.Add("closureRate");
        if (!aircraft.OwnMach.HasValue) missing.Add("ownMach");

        if (missile.NeedsRadarSupport)
        {
            if (target.TrackingMode is not (TrackingMode.RadarLock or TrackingMode.RadarTrack or TrackingMode.TWS))
                missing.Add("radarTrackMode");
            if (aircraftProfile is null) missing.Add("aircraftProfile");
            else if (!aircraftProfile.HasBvrRadar) missing.Add("bvrRadar");
        }

        if (IsFox3OrRadarBvr(missile.MissileType) && !aircraft.OwnAltitudeFt.HasValue)
            missing.Add("ownAltitude");

        return missing;
    }

    private static double ComputeRangeScore(MissileProfile missile, double rangeNm)
    {
        if (rangeNm < missile.MinimumRangeNm || rangeNm > missile.MaxEffectiveRangeNm)
            return 0;

        if (Math.Abs(rangeNm - missile.IdealRangeNm) < missile.IdealRangeNm * 0.15)
            return 85;

        if (rangeNm <= missile.NoEscapeRangeNm)
            return 70;

        var span = missile.MaxEffectiveRangeNm - missile.NoEscapeRangeNm;
        if (span <= 0) return 40;

        var ratio = (missile.MaxEffectiveRangeNm - rangeNm) / span;
        return 30 + ratio * 40;
    }

    private static double AspectModifier(MissileProfile missile, TargetAspectCategory aspect)
    {
        var sens = missile.AspectSensitivity;
        switch (aspect)
        {
            case TargetAspectCategory.Hot:
                return IsInfrared(missile.MissileType) ? 1.0 + sens * 0.2 : 1.0 - sens * 0.1;
            case TargetAspectCategory.Flanking:
                return 1.0 - sens * 0.15;
            case TargetAspectCategory.Beaming:
                return 1.0 - sens * 0.35;
            case TargetAspectCategory.Cold:
                return IsInfrared(missile.MissileType) ? 1.0 - sens * 0.5 : 1.0 - sens * 0.2;
            default:
                return 0.85;
        }
    }

    private static double ClosureModifier(MissileProfile missile, double closureKnots)
    {
        var sens = missile.ClosureSensitivity;
        if (closureKnots > 200) return 1.0 + sens * 0.15;
        if (closureKnots > 50) return 1.0;
        if (closureKnots > -50) return 1.0 - sens * 0.1;
        return 1.0 - sens * 0.25;
    }

    private static double AltitudeModifier(MissileProfile missile, double ownAltFt, double targetAltFt)
    {
        var sens = missile.AltitudeSensitivity;
        var delta = ownAltFt - targetAltFt;
        if (delta > 5000) return 1.0 + sens * 0.1;
        if (delta < -5000) return 1.0 - sens * 0.15;
        return 1.0;
    }

    private static double MachModifier(double mach)
    {
        if (mach >= 0.8 && mach <= 1.2) return 1.05;
        if (mach < 0.5) return 0.85;
        if (mach > 1.5) return 0.9;
        return 1.0;
    }

    private static ShotCategory CategorizeShot(double pk, double rangeNm, MissileProfile missile)
    {
        if (rangeNm < missile.MinimumRangeNm || pk < 15)
            return ShotCategory.DoNotFire;
        if (pk >= 70) return ShotCategory.HighPk;
        if (pk >= 50) return ShotCategory.MediumPk;
        if (pk >= 30) return ShotCategory.LowPk;
        if (pk >= 15) return ShotCategory.PressureShot;
        return ShotCategory.DoNotFire;
    }

    private static ShotRecommendation Recommend(
        double pk,
        double rangeNm,
        TargetAspectCategory aspect,
        TargetState target,
        MissileProfile missile)
    {
        if (rangeNm < missile.MinimumRangeNm) return ShotRecommendation.Abort;
        if (rangeNm > missile.MaxEffectiveRangeNm) return ShotRecommendation.Wait;

        if (target.TargetIsJamming == true && missile.ChaffSensitivity > 0.5)
            return ShotRecommendation.Wait;

        if (IsInfrared(missile.MissileType) && aspect == TargetAspectCategory.Cold)
            return ShotRecommendation.Wait;

        if (rangeNm > missile.NoEscapeRangeNm && rangeNm <= missile.MaxEffectiveRangeNm)
            return ShotRecommendation.MaintainLock;

        if (rangeNm > missile.IdealRangeNm * 1.2)
            return ShotRecommendation.GetCloser;

        if (pk >= 70) return ShotRecommendation.Fire;
        if (pk >= 50) return ShotRecommendation.MaintainLock;
        if (pk >= 30) return ShotRecommendation.Wait;

        return ShotRecommendation.Abort;
    }

    private static bool IsInfrared(MissileType type) =>
        type is MissileType.InfraredWvr or MissileType.Fox2;

    private static bool IsFox3OrRadarBvr(MissileType type) =>
        type is MissileType.Fox3 or MissileType.RadarBvr;
}
