using System.Text.Json.Serialization;

namespace DcsMissileAdvisor.Api.Models;

public class DcsRawPacket
{
    public int V { get; set; }
    public long Seq { get; set; }
    public double? T { get; set; }
    public DcsPermRaw? Perm { get; set; }
    public DcsSelfRaw? Self { get; set; }
    public DcsFlightRaw? Flight { get; set; }
    public DcsPayloadRaw? Payload { get; set; }
    [JsonPropertyName("targets_locked")]
    public List<DcsTargetRaw>? TargetsLocked { get; set; }
    [JsonPropertyName("targets_info")]
    public List<DcsTargetRaw>? TargetsInfo { get; set; }
    [JsonPropertyName("weapon_selected_name")]
    public string? WeaponSelectedName { get; set; }
    [JsonPropertyName("cockpit_target")]
    public DcsTargetRaw? CockpitTarget { get; set; }
    [JsonPropertyName("target_api_available")]
    public bool? TargetApiAvailable { get; set; }
    public DcsTwsRaw? Tws { get; set; }
}

public class DcsPermRaw
{
    public bool Ownship { get; set; }
    public bool Sensor { get; set; }
    public bool Object { get; set; }
}

public class DcsSelfRaw
{
    public string? Name { get; set; }
    [JsonPropertyName("unit_name")]
    public string? UnitName { get; set; }
    public List<int>? Type { get; set; }
    [JsonPropertyName("heading_rad")]
    public double? HeadingRad { get; set; }
    [JsonPropertyName("alt_msl_m")]
    public double? AltMslM { get; set; }
}

public class DcsFlightRaw
{
    public double? Mach { get; set; }
    [JsonPropertyName("ias_mps")]
    public double? IasMps { get; set; }
    [JsonPropertyName("tas_mps")]
    public double? TasMps { get; set; }
    [JsonPropertyName("alt_msl_m")]
    public double? AltMslM { get; set; }
    [JsonPropertyName("alt_agl_m")]
    public double? AltAglM { get; set; }
    public DcsVec3Raw? Vel { get; set; }
}

public class DcsVec3Raw
{
    public double? X { get; set; }
    public double? Y { get; set; }
    public double? Z { get; set; }
}

public class DcsPayloadRaw
{
    [JsonPropertyName("current_station")]
    public int? CurrentStation { get; set; }
    public List<DcsStationRaw>? Stations { get; set; }
    [JsonPropertyName("cannon_shells")]
    public int? CannonShells { get; set; }
}

public class DcsStationRaw
{
    public int Idx { get; set; }
    public int? Count { get; set; }
    public List<int>? Type { get; set; }
    public string? Name { get; set; }
    public bool? Container { get; set; }
}

public class DcsTargetRaw
{
    public string? Source { get; set; }
    public long? Id { get; set; }
    [JsonPropertyName("distance_m")]
    public double? DistanceM { get; set; }
    [JsonPropertyName("convergence_mps")]
    public double? ConvergenceMps { get; set; }
    [JsonPropertyName("delta_psi_rad")]
    public double? DeltaPsiRad { get; set; }
    [JsonPropertyName("course_rad")]
    public double? CourseRad { get; set; }
    [JsonPropertyName("is_jamming")]
    public bool? IsJamming { get; set; }
    public int? Flags { get; set; }
    [JsonPropertyName("pos_y_m")]
    public double? PosYM { get; set; }
    [JsonPropertyName("alt_msl_m")]
    public double? AltMslM { get; set; }
    public double? Mach { get; set; }
}

public class DcsTwsRaw
{
    public int? Mode { get; set; }
    public List<DcsEmitterRaw>? Emitters { get; set; }
}

public class DcsEmitterRaw
{
    public long? Id { get; set; }
    public double? Azimuth { get; set; }
    public string? Signal { get; set; }
    public int? Priority { get; set; }
}
