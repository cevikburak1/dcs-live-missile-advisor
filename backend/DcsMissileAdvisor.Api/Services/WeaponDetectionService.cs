using DcsMissileAdvisor.Api.Models;

namespace DcsMissileAdvisor.Api.Services;

public class WeaponDetectionService
{
    private readonly MissileProfileResolver _resolver;

    public WeaponDetectionService(MissileProfileResolver resolver)
    {
        _resolver = resolver;
    }

    public (string? RawName, MissileProfile? Profile, int? Station) Detect(DcsPayloadRaw? payload)
    {
        if (payload is null)
            return (null, null, null);

        var stationIdx = payload.CurrentStation;
        if (stationIdx is null or 0)
            return (null, null, stationIdx);

        DcsStationRaw? selected = null;
        if (payload.Stations is not null)
        {
            selected = payload.Stations.FirstOrDefault(s => s.Idx == stationIdx);
        }

        if (selected is null)
            return (null, null, stationIdx);

        var rawName = selected.Name;
        if (string.IsNullOrWhiteSpace(rawName) && selected.Type is { Count: >= 4 })
        {
            rawName = string.Join("-", selected.Type);
        }

        var profile = _resolver.Resolve(rawName, selected.Type);
        return (rawName, profile, stationIdx);
    }
}
