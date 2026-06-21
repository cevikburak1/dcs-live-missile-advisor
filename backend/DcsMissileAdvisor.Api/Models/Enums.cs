namespace DcsMissileAdvisor.Api.Models;

public enum MissileType
{
    Fox1,
    Fox2,
    Fox3,
    RadarBvr,
    InfraredWvr,
    Unknown
}

public enum TargetAspectCategory
{
    Hot,
    Flanking,
    Beaming,
    Cold,
    Unknown
}

public enum TrackingMode
{
    RadarLock,
    RadarTrack,
    TWS,
    EOS,
    Unknown
}

public enum ShotCategory
{
    HighPk,
    MediumPk,
    LowPk,
    PressureShot,
    DoNotFire,
    Unknown
}

public enum ShotRecommendation
{
    Fire,
    Wait,
    GetCloser,
    Climb,
    MaintainLock,
    Abort,
    Unknown
}

public enum DcsConnectionStatus
{
    Disconnected,
    Connected,
    Stale
}
