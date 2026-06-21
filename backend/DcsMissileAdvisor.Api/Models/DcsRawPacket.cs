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
    public List<DcsTargetRaw>? TargetsLocked { get; set; }
    public List<DcsTargetRaw>? TargetsInfo { get; set; }
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
    public string? UnitName { get; set; }
    public List<int>? Type { get; set; }
    public double? HeadingRad { get; set; }
}

public class DcsFlightRaw
{
    public double? Mach { get; set; }
    public double? IasMps { get; set; }
    public double? TasMps { get; set; }
    public double? AltMslM { get; set; }
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
    public int? CurrentStation { get; set; }
    public List<DcsStationRaw>? Stations { get; set; }
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
    public long? Id { get; set; }
    public double? DistanceM { get; set; }
    public double? ConvergenceMps { get; set; }
    public double? DeltaPsiRad { get; set; }
    public double? CourseRad { get; set; }
    public bool? IsJamming { get; set; }
    public int? Flags { get; set; }
    public double? PosYM { get; set; }
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
