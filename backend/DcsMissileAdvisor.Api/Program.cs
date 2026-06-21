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

var profilesDir = Path.Combine(builder.Environment.ContentRootPath, "profiles");
if (!Directory.Exists(profilesDir))
{
    profilesDir = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "profiles"));
}

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

app.MapHub<AdvisorHub>("/hubs/advisor");

app.Run("http://localhost:5000");
