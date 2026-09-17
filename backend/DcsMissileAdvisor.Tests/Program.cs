using System.Text.Json;
using DcsMissileAdvisor.Api.Hubs;
using DcsMissileAdvisor.Api.Models;
using DcsMissileAdvisor.Api.Profiles;
using DcsMissileAdvisor.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
void Near(double? actual, double expected, string message) =>
    Check(actual.HasValue && Math.Abs(actual.Value - expected) < 0.01, message);

var root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var packet = JsonSerializer.Deserialize<DcsRawPacket>(
    File.ReadAllText(Path.Combine(root, ".artifacts/export-fixture.json")), options)!;
var profiles = new ProfileLoader();
profiles.Load(Path.Combine(root, "profiles"));
var resolver = new MissileProfileResolver(profiles);
var weapons = new WeaponDetectionService(resolver);
var targets = new TargetLockDetectionService();
var quality = new ShotQualityService();
var services = new ServiceCollection();
services.AddLogging();
services.AddSignalR();
using var provider = services.BuildServiceProvider();
var aggregator = new AdvisorStateAggregator(new AircraftDetectionService(profiles), weapons, targets,
    quality, provider.GetRequiredService<IHubContext<AdvisorHub>>());
var snapshot = aggregator.BuildSnapshot(packet, DcsConnectionStatus.Connected);

Near(snapshot.Aircraft.OwnAirspeedKnots, 388.768, "IAS survives Lua -> JSON -> snapshot");
Near(snapshot.Aircraft.OwnTrueAirspeedKnots, 485.96, "TAS units");
Near(snapshot.Aircraft.OwnAltitudeFt, 16404.2, "altitude units");
Near(snapshot.Aircraft.OwnHeadingDeg, 90, "heading units");
Check(packet.Self?.UnitName == "Test", "unit_name mapped");
Check(packet.Payload?.CannonShells == 578, "cannon_shells mapped");
Check(snapshot.Aircraft.SelectedStation == 4, "current_station mapped");
Check(snapshot.Aircraft.SelectedWeaponProfileName == "AIM-7P Sparrow", "AIM-7P distinct profile");
Check(snapshot.Target.IsLocked, "targets_locked mapped");
Check(packet.TargetsLocked![0].Id == 16777217, "large DCS object IDs retain exact integer representation");
Near(snapshot.Target.TargetRangeNm, 10, "target range mapped");
Near(snapshot.Target.TargetAltitudeFt, 19685.04, "pos_y_m mapped");
Near(snapshot.Target.TargetClosureRateKnots, 291.576, "closure mapped");
Near(snapshot.Target.TargetAspectDeg, 5.72957795, "aspect mapped");
Near(snapshot.Target.TargetCourseDeg, 120.32113698, "course mapped");
Check(snapshot.ShotQuality.CanCalculate, "full exported target produces shot quality");

packet.Payload!.CurrentStation = 0;
packet.WeaponSelectedName = "AIM-7P";
snapshot = aggregator.BuildSnapshot(packet, DcsConnectionStatus.Connected);
Check(snapshot.Aircraft.SelectedWeaponProfileName == "AIM-7P Sparrow", "cockpit selection fallback");
Check(snapshot.Aircraft.SelectedStation == null, "unknown station is not invented");
packet.WeaponSelectedName = null;
snapshot = aggregator.BuildSnapshot(packet, DcsConnectionStatus.Connected);
Check(snapshot.Aircraft.SelectedWeaponRawName == null, "loaded stores alone never select a missile");
Check(snapshot.ShotQuality.MissingDataFields.Contains("selectedWeapon"), "missing selection distinguished from missing profile");

var permissions = new DcsPermRaw { Sensor = true, Ownship = true };
var contact = new DcsTargetRaw { Id = 1, DistanceM = 10000, Flags = 2 };
var rwr = new DcsTwsRaw { Emitters = new() { new() { Signal = "lock" } } };
var target = targets.Build(permissions, new(), new() { contact }, rwr);
Check(!target.IsLocked && target.TrackingMode == TrackingMode.Unknown, "search contacts and enemy RWR locks are not own locks");
contact.Flags = 8;
Check(targets.Build(permissions, new(), new() { contact }, null).IsLocked, "explicit lock in targets_info accepted");
var cockpit = new DcsTargetRaw { DistanceM = 10000, ConvergenceMps = 100, Source = "Hornet HUD/DDI" };
target = targets.Build(permissions, new(), new(), null, cockpit, false);
Check(target.IsLocked && target.DataSource == cockpit.Source, "cockpit fallback used");
Check(target.TargetIsJamming == null, "missing ECM stays unknown");
Check(target.TrackingMode == TrackingMode.Unknown, "cockpit range alone does not prove radar STT");
var shot = quality.Calculate(snapshot.Aircraft, target, resolver.Resolve("AIM-7P", null), profiles.Aircraft[0]);
Check(!shot.CanCalculate && shot.MissingDataFields.Contains("targetAspect")
    && shot.MissingDataFields.Contains("radarTrackMode"), "incomplete cockpit data cannot produce a made-up PK");
permissions.Sensor = false;
Check(!targets.Build(permissions, new(), new(), null, cockpit).IsLocked, "sensor restrictions apply to cockpit fallback");
permissions.Sensor = true;
permissions.Ownship = false;
Check(!targets.Build(permissions, new(), new(), null, cockpit).IsLocked, "ownship restrictions apply to cockpit fallback");
permissions.Ownship = true;
target = targets.Build(permissions, new(), new(), null, null, false);
Check(target.StatusMessage!.Contains("not exported"), "unavailable API is not reported as no lock");
packet.Flight!.AltMslM = null;
Near(aggregator.BuildSnapshot(packet, DcsConnectionStatus.Connected).Aircraft.OwnAltitudeFt,
    16404.2, "self altitude fallback");
contact.DeltaPsiRad = Math.PI / 2;
Check(targets.Build(permissions, new() { contact }, null, null).TargetAspectCategory == TargetAspectCategory.Beaming,
    "90-degree target aspect is beaming");
Console.WriteLine($"PASS: {checks} backend contract checks");
