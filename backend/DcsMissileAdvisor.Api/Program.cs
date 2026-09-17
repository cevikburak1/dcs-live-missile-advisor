using System.Text.Json.Serialization;
using DcsMissileAdvisor.Api.Hubs;
using DcsMissileAdvisor.Api.Models;
using DcsMissileAdvisor.Api.Profiles;
using DcsMissileAdvisor.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var profileDirCandidates = new[]
{
    Path.Combine(builder.Environment.ContentRootPath, "profiles"),
    Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "profiles")),
    Path.Combine(AppContext.BaseDirectory, "profiles")
};

var profilesDir = profileDirCandidates.FirstOrDefault(path =>
    File.Exists(Path.Combine(path, "missile_profiles.json")) &&
    File.Exists(Path.Combine(path, "aircraft_profiles.json")))
    ?? profileDirCandidates[0];

var profileLoader = new ProfileLoader();
profileLoader.Load(profilesDir);

builder.Services.AddSingleton(profileLoader);
builder.Services.AddSingleton<MissileProfileResolver>();
builder.Services.AddSingleton<AircraftDetectionService>();
builder.Services.AddSingleton<WeaponDetectionService>();
builder.Services.AddSingleton<TargetLockDetectionService>();
builder.Services.AddSingleton<ShotQualityService>();
builder.Services.AddSingleton<AdvisorStateAggregator>();
builder.Services.AddSingleton<DcsTelemetryReceiver>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DcsTelemetryReceiver>());

var app = builder.Build();

app.UseCors();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", profilesDir }));

app.MapGet("/api/profiles/missiles", (ProfileLoader loader) => Results.Ok(loader.Missiles));

app.MapGet("/api/profiles/aircraft", (ProfileLoader loader) => Results.Ok(loader.Aircraft));

app.MapGet("/api/snapshot", (
    AdvisorStateAggregator aggregator,
    DcsTelemetryReceiver receiver) =>
{
    var packet = receiver.GetState().LastPacket;
    var status = receiver.GetConnectionStatus();
    return Results.Ok(aggregator.BuildSnapshot(packet, status));
});

app.MapGet("/api/telemetry", (DcsTelemetryReceiver receiver) =>
{
    var state = receiver.GetState();
    return Results.Ok(new
    {
        connectionStatus = receiver.GetConnectionStatus(),
        lastReceivedUtc = state.LastPacket is null ? (DateTime?)null : state.LastReceivedUtc,
        packet = state.RawPacket
    });
});

app.MapHub<AdvisorHub>("/hubs/advisor");

app.Run("http://localhost:5000");
