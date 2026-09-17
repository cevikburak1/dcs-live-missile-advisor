using DcsMissileAdvisor.Api.Models;

namespace DcsMissileAdvisor.Api.Services;

public class WeaponDetectionService
{
    private readonly MissileProfileResolver _resolver;

    public WeaponDetectionService(MissileProfileResolver resolver)
    {
        _resolver = resolver;
    }

    public (string? RawName, MissileProfile? Profile, int? Station) Detect(
        DcsPayloadRaw? payload, string? selectedWeaponName = null)
    {
        var stationIdx = payload?.CurrentStation is > 0 ? payload.CurrentStation : null;

        DcsStationRaw? selected = null;
        if (stationIdx.HasValue && payload?.Stations is not null)
        {
            selected = payload.Stations.FirstOrDefault(s => s.Idx == stationIdx);
        }

        if (selected is null)
            return (selectedWeaponName, _resolver.Resolve(selectedWeaponName, null), stationIdx);

        var rawName = selected.Name;
        if (string.IsNullOrWhiteSpace(rawName) && selected.Type is { Count: >= 4 })
        {
            rawName = string.Join("-", selected.Type);
        }

        var profile = _resolver.Resolve(rawName, selected.Type);
        return (rawName, profile, stationIdx);
    }
}
