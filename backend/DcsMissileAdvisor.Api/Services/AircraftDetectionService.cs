using DcsMissileAdvisor.Api.Models;
using DcsMissileAdvisor.Api.Profiles;

namespace DcsMissileAdvisor.Api.Services;

public class AircraftDetectionService
{
    private readonly ProfileLoader _profiles;

    public AircraftDetectionService(ProfileLoader profiles)
    {
        _profiles = profiles;
    }

    public AircraftProfile? Resolve(string? rawName, string? unitName)
    {
        var candidates = new List<(AircraftProfile Profile, int Score)>();

        foreach (var profile in _profiles.Aircraft)
        {
            var score = ScoreAircraft(profile, rawName, unitName);
            if (score > 0)
                candidates.Add((profile, score));
        }

        if (candidates.Count == 0)
            return null;

        var maxScore = candidates.Max(c => c.Score);
        var top = candidates.Where(c => c.Score == maxScore).ToList();

        if (top.Count > 1)
            return null;

        return top[0].Profile;
    }

    private static int ScoreAircraft(AircraftProfile profile, string? rawName, string? unitName)
    {
        int best = 0;
        var normRaw = MissileProfileResolver.Normalize(rawName);
        var normUnit = MissileProfileResolver.Normalize(unitName);
        var normName = MissileProfileResolver.Normalize(profile.AircraftName);

        if (normRaw is not null && normName == normRaw) best = Math.Max(best, 100);
        if (normUnit is not null && normName == normUnit) best = Math.Max(best, 100);

        foreach (var alias in profile.Aliases)
        {
            var normAlias = MissileProfileResolver.Normalize(alias);
            if (normAlias is null) continue;

            if (normRaw is not null && (normAlias == normRaw || normRaw.Contains(normAlias) || normAlias.Contains(normRaw)))
                best = Math.Max(best, 80);

            if (normUnit is not null && (normAlias == normUnit || normUnit.Contains(normAlias) || normAlias.Contains(normUnit)))
                best = Math.Max(best, 80);
        }

        return best;
    }
}
