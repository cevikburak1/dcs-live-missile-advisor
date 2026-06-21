namespace DcsMissileAdvisor.Api.Models;

public class AircraftState
{
    public string? AircraftRawName { get; set; }
    public string? AircraftProfileName { get; set; }
    public double? OwnAltitudeFt { get; set; }
    public double? OwnAirspeedKnots { get; set; }
    public double? OwnTrueAirspeedKnots { get; set; }
    public double? OwnMach { get; set; }
    public double? OwnHeadingDeg { get; set; }
    public VelocityVector? OwnVelocityVector { get; set; }
    public int? SelectedStation { get; set; }
    public string? SelectedWeaponRawName { get; set; }
    public string? SelectedWeaponProfileName { get; set; }
    public MissileType SelectedWeaponType { get; set; } = MissileType.Unknown;
    public bool OwnshipExportAvailable { get; set; }
}

public class VelocityVector
{
    public double? X { get; set; }
    public double? Y { get; set; }
    public double? Z { get; set; }
}

public class TargetState
{
    public bool IsLocked { get; set; }
    public bool SensorExportAvailable { get; set; }
    public string? StatusMessage { get; set; }
    public double? TargetRangeNm { get; set; }
    public double? TargetAltitudeFt { get; set; }
    public double? TargetMach { get; set; }
    public double? TargetClosureRateKnots { get; set; }
    public double? TargetAspectDeg { get; set; }
    public TargetAspectCategory TargetAspectCategory { get; set; } = TargetAspectCategory.Unknown;
    public double? TargetCourseDeg { get; set; }
    public bool TargetIsJamming { get; set; }
    public TrackingMode TrackingMode { get; set; } = TrackingMode.Unknown;
}

public class ShotQualityResult
{
    public bool CanCalculate { get; set; }
    public double? EstimatedPkPercent { get; set; }
    public ShotCategory ShotCategory { get; set; } = ShotCategory.Unknown;
    public ShotRecommendation Recommendation { get; set; } = ShotRecommendation.Unknown;
    public string Explanation { get; set; } = string.Empty;
    public List<string> MissingDataFields { get; set; } = new();
    public MissileProfile? ActiveMissileProfile { get; set; }
}

public class ExportPermissions
{
    public bool Ownship { get; set; }
    public bool Sensor { get; set; }
    public bool Object { get; set; }
}

public class AdvisorSnapshot
{
    public DcsConnectionStatus ConnectionStatus { get; set; } = DcsConnectionStatus.Disconnected;
    public ExportPermissions Permissions { get; set; } = new();
    public AircraftState Aircraft { get; set; } = new();
    public TargetState Target { get; set; } = new();
    public ShotQualityResult ShotQuality { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public long? PacketSeq { get; set; }
    public double? ModelTime { get; set; }
}
