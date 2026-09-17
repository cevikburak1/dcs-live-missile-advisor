using DcsMissileAdvisor.Api.Hubs;
using DcsMissileAdvisor.Api.Models;
using Microsoft.AspNetCore.SignalR;

namespace DcsMissileAdvisor.Api.Services;

public class AdvisorStateAggregator
{
    private readonly AircraftDetectionService _aircraftDetection;
    private readonly WeaponDetectionService _weaponDetection;
    private readonly TargetLockDetectionService _targetLockDetection;
    private readonly ShotQualityService _shotQuality;
    private readonly IHubContext<AdvisorHub> _hub;

    private AdvisorSnapshot _lastSnapshot = new();

    public AdvisorStateAggregator(
        AircraftDetectionService aircraftDetection,
        WeaponDetectionService weaponDetection,
        TargetLockDetectionService targetLockDetection,
        ShotQualityService shotQuality,
        IHubContext<AdvisorHub> hub)
    {
        _aircraftDetection = aircraftDetection;
        _weaponDetection = weaponDetection;
        _targetLockDetection = targetLockDetection;
        _shotQuality = shotQuality;
        _hub = hub;
    }

    public AdvisorSnapshot GetLastSnapshot() => _lastSnapshot;

    public async Task ProcessPacketAsync(DcsRawPacket packet, DcsConnectionStatus connectionStatus)
    {
        var snapshot = BuildSnapshot(packet, connectionStatus);
        _lastSnapshot = snapshot;
        await _hub.Clients.All.SendAsync("AdvisorUpdate", snapshot);
    }

    public AdvisorSnapshot BuildSnapshot(DcsRawPacket? packet, DcsConnectionStatus connectionStatus)
    {
        if (packet is null)
        {
            return new AdvisorSnapshot
            {
                ConnectionStatus = connectionStatus,
                Timestamp = DateTime.UtcNow
            };
        }

        var perm = packet.Perm ?? new DcsPermRaw();
        var aircraftProfile = _aircraftDetection.Resolve(packet.Self?.Name, packet.Self?.UnitName);
        var (weaponRaw, missileProfile, station) = _weaponDetection.Detect(packet.Payload, packet.WeaponSelectedName);

        var aircraftState = BuildAircraftState(packet, perm, aircraftProfile, weaponRaw, missileProfile, station);
        var targetState = _targetLockDetection.Build(
            perm,
            packet.TargetsLocked,
            packet.TargetsInfo,
            packet.Tws,
            packet.CockpitTarget,
            packet.TargetApiAvailable);

        var shotQuality = _shotQuality.Calculate(aircraftState, targetState, missileProfile, aircraftProfile);

        return new AdvisorSnapshot
        {
            ConnectionStatus = connectionStatus,
            Permissions = new ExportPermissions
            {
                Ownship = perm.Ownship,
                Sensor = perm.Sensor,
                Object = perm.Object
            },
            Aircraft = aircraftState,
            Target = targetState,
            ShotQuality = shotQuality,
            Timestamp = DateTime.UtcNow,
            PacketSeq = packet.Seq,
            ModelTime = packet.T
        };
    }

    private static AircraftState BuildAircraftState(
        DcsRawPacket packet,
        DcsPermRaw perm,
        AircraftProfile? aircraftProfile,
        string? weaponRaw,
        MissileProfile? missileProfile,
        int? station)
    {
        var state = new AircraftState
        {
            OwnshipExportAvailable = perm.Ownship,
            AircraftRawName = packet.Self?.Name,
            AircraftProfileName = aircraftProfile?.AircraftName,
            SelectedStation = station,
            SelectedWeaponRawName = weaponRaw,
            SelectedWeaponProfileName = missileProfile?.MissileName,
            SelectedWeaponType = missileProfile?.MissileType ?? MissileType.Unknown
        };

        if (!perm.Ownship) return state;

        state.OwnMach = packet.Flight?.Mach;
        state.OwnAirspeedKnots = UnitConversion.MpsToKnotsNullable(packet.Flight?.IasMps);
        state.OwnTrueAirspeedKnots = UnitConversion.MpsToKnotsNullable(packet.Flight?.TasMps);
        state.OwnAltitudeFt = UnitConversion.MetersToFeetNullable(packet.Flight?.AltMslM ?? packet.Self?.AltMslM);
        state.OwnHeadingDeg = UnitConversion.RadToDegNullable(packet.Self?.HeadingRad);

        if (packet.Flight?.Vel is not null)
        {
            state.OwnVelocityVector = new VelocityVector
            {
                X = packet.Flight.Vel.X,
                Y = packet.Flight.Vel.Y,
                Z = packet.Flight.Vel.Z
            };
        }

        return state;
    }
}
