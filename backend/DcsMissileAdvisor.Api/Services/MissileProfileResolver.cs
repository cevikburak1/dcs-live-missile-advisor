using DcsMissileAdvisor.Api.Models;
using DcsMissileAdvisor.Api.Profiles;

namespace DcsMissileAdvisor.Api.Services;

public class MissileProfileResolver
{
    private readonly ProfileLoader _profiles;

    public MissileProfileResolver(ProfileLoader profiles)
    {
        _profiles = profiles;
    }

    public MissileProfile? Resolve(string? rawName, List<int>? typeTuple)
    {
        var candidates = new List<(MissileProfile Profile, int Score)>();

        foreach (var profile in _profiles.Missiles)
        {
            var score = ScoreProfile(profile, rawName, typeTuple);
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

    private static int ScoreProfile(MissileProfile profile, string? rawName, List<int>? typeTuple)
    {
        var normalizedRaw = Normalize(rawName);
        var typeStr = typeTuple is { Count: >= 4 }
            ? Normalize(string.Join("-", typeTuple))
            : null;

        int best = 0;

        foreach (var alias in profile.Aliases)
        {
            var normAlias = Normalize(alias);
            if (string.IsNullOrEmpty(normAlias)) continue;

            if (normalizedRaw is not null)
            {
                if (normAlias == normalizedRaw)
                    best = Math.Max(best, 100);
                else if (normalizedRaw.Contains(normAlias) || normAlias.Contains(normalizedRaw))
                    best = Math.Max(best, 50);
            }

            if (typeStr is not null && (typeStr.Contains(normAlias) || normAlias.Contains(typeStr)))
                best = Math.Max(best, 30);
        }

        var normName = Normalize(profile.MissileName);
        if (normalizedRaw is not null && normName == normalizedRaw)
            best = Math.Max(best, 100);

        return best;
    }

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().ToUpperInvariant()
            .Replace("_", "-")
            .Replace(" ", "-");
    }
}
