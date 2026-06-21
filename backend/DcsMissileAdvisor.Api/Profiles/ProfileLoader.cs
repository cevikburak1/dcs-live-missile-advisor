using System.Text.Json;
using System.Text.Json.Serialization;
using DcsMissileAdvisor.Api.Models;

namespace DcsMissileAdvisor.Api.Profiles;

public class ProfileLoader
{
    private readonly List<MissileProfile> _missiles = new();
    private readonly List<AircraftProfile> _aircraft = new();

    public IReadOnlyList<MissileProfile> Missiles => _missiles;
    public IReadOnlyList<AircraftProfile> Aircraft => _aircraft;

    public void Load(string profilesDirectory)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        var missilePath = Path.Combine(profilesDirectory, "missile_profiles.json");
        var aircraftPath = Path.Combine(profilesDirectory, "aircraft_profiles.json");

        if (!File.Exists(missilePath))
            throw new FileNotFoundException($"Missile profiles not found: {missilePath}");

        if (!File.Exists(aircraftPath))
            throw new FileNotFoundException($"Aircraft profiles not found: {aircraftPath}");

        _missiles.Clear();
        _aircraft.Clear();

        var missiles = JsonSerializer.Deserialize<List<MissileProfile>>(File.ReadAllText(missilePath), options);
        var aircraft = JsonSerializer.Deserialize<List<AircraftProfile>>(File.ReadAllText(aircraftPath), options);

        if (missiles != null) _missiles.AddRange(missiles);
        if (aircraft != null) _aircraft.AddRange(aircraft);
    }
}
