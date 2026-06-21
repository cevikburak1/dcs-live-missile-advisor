using DcsMissileAdvisor.Api.Models;

namespace DcsMissileAdvisor.Api.Services;

public class TargetLockDetectionService
{
    private const int WhTargetRadarLock = 0x0008;
    private const int WhTargetRadarTrack = 0x0020;
    private const int WhTargetEosLock = 0x0010;
    private const int WhTargetEosTrack = 0x0040;
    private const int WhTargetLockOnJammer = 0x0800;

    public TargetState Build(
        DcsPermRaw? perm,
        List<DcsTargetRaw>? targetsLocked,
        List<DcsTargetRaw>? targetsInfo,
        DcsTwsRaw? tws)
    {
        var state = new TargetState
        {
            SensorExportAvailable = perm?.Sensor ?? false
        };

        if (!state.SensorExportAvailable)
        {
            state.IsLocked = false;
            state.StatusMessage = "Target lock export unavailable in this mission/server";
            state.TrackingMode = TrackingMode.Unknown;
            return state;
        }

        var target = PickBestTarget(targetsLocked) ?? PickBestTarget(targetsInfo);

        if (target is null)
        {
            state.IsLocked = false;
            state.StatusMessage = "No target locked";
            state.TrackingMode = InferTrackingModeFromTws(tws);
            return state;
        }

        state.IsLocked = true;
        state.StatusMessage = null;
        state.TargetRangeNm = UnitConversion.MetersToNmNullable(target.DistanceM);
        state.TargetMach = target.Mach;

        var altM = target.AltMslM ?? target.PosYM;
        state.TargetAltitudeFt = UnitConversion.MetersToFeetNullable(altM);

        state.TargetClosureRateKnots = UnitConversion.MpsToKnotsNullable(target.ConvergenceMps);
        state.TargetAspectDeg = UnitConversion.RadToDegNullable(target.DeltaPsiRad);
        state.TargetAspectCategory = CategorizeAspect(state.TargetAspectDeg);
        state.TargetCourseDeg = UnitConversion.RadToDegNullable(target.CourseRad);

        state.TargetIsJamming = target.IsJamming == true
            || (target.Flags.HasValue && (target.Flags.Value & WhTargetLockOnJammer) != 0);

        state.TrackingMode = InferTrackingMode(target.Flags) ?? InferTrackingModeFromTws(tws);

        return state;
    }

    private static DcsTargetRaw? PickBestTarget(List<DcsTargetRaw>? targets)
    {
        if (targets is null || targets.Count == 0)
            return null;

        return targets
            .Where(t => t.DistanceM.HasValue || (t.Id.HasValue && t.Id.Value != 0))
            .OrderByDescending(t => t.DistanceM ?? 0)
            .FirstOrDefault();
    }

    private static TargetAspectCategory CategorizeAspect(double? aspectDeg)
    {
        if (!aspectDeg.HasValue)
            return TargetAspectCategory.Unknown;

        var abs = Math.Abs(aspectDeg.Value) % 360;
        if (abs > 180) abs = 360 - abs;

        if (abs < 45) return TargetAspectCategory.Hot;
        if (abs < 135) return TargetAspectCategory.Flanking;
        if (abs < 165) return TargetAspectCategory.Beaming;
        return TargetAspectCategory.Cold;
    }

    private static TrackingMode? InferTrackingMode(int? flags)
    {
        if (!flags.HasValue) return null;

        if ((flags.Value & WhTargetRadarLock) != 0) return TrackingMode.RadarLock;
        if ((flags.Value & WhTargetRadarTrack) != 0) return TrackingMode.RadarTrack;
        if ((flags.Value & WhTargetEosLock) != 0) return TrackingMode.EOS;
        if ((flags.Value & WhTargetEosTrack) != 0) return TrackingMode.EOS;

        return null;
    }

    private static TrackingMode InferTrackingModeFromTws(DcsTwsRaw? tws)
    {
        if (tws?.Emitters is null || tws.Emitters.Count == 0)
            return TrackingMode.Unknown;

        var hasTrack = tws.Emitters.Any(e =>
            e.Signal is "track_while_scan" or "lock");

        return hasTrack ? TrackingMode.TWS : TrackingMode.Unknown;
    }
}
