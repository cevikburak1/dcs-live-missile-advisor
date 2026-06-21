namespace DcsMissileAdvisor.Api.Models;

public class MissileProfile
{
    public string MissileName { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = new();
    public MissileType MissileType { get; set; } = MissileType.Unknown;
    public double MinimumRangeNm { get; set; }
    public double IdealRangeNm { get; set; }
    public double NoEscapeRangeNm { get; set; }
    public double MaxEffectiveRangeNm { get; set; }
    public bool NeedsRadarSupport { get; set; }
    public bool SupportsPitbull { get; set; }
    public bool HighOffBoresight { get; set; }
    public double ChaffSensitivity { get; set; }
    public double FlareSensitivity { get; set; }
    public double AspectSensitivity { get; set; }
    public double AltitudeSensitivity { get; set; }
    public double ClosureSensitivity { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class AircraftProfile
{
    public string AircraftName { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = new();
    public List<string> SupportedMissileAliases { get; set; } = new();
    public string RadarType { get; set; } = string.Empty;
    public bool HasBvrRadar { get; set; }
    public bool HasHmd { get; set; }
    public string Notes { get; set; } = string.Empty;
}
