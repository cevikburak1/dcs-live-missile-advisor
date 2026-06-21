import type { AircraftState } from "../types/advisor";

interface Props {
  aircraft: AircraftState;
}

export function WeaponPanel({ aircraft }: Props) {
  const profileMissing =
    aircraft.selectedWeaponRawName &&
    !aircraft.selectedWeaponProfileName;

  return (
    <div className="rounded-lg border border-slate-700 bg-slate-900/80 p-4">
      <h2 className="text-xs font-semibold uppercase tracking-widest text-cyan-500">
        Weapon
      </h2>
      {profileMissing && (
        <p className="mt-2 text-sm text-amber-400">Missile profile not found</p>
      )}
      <dl className="mt-3 space-y-2 font-mono text-sm">
        <Row label="Station" value={aircraft.selectedStation?.toString() ?? "—"} />
        <Row label="Raw Weapon" value={aircraft.selectedWeaponRawName ?? "—"} />
        <Row
          label="Profile"
          value={aircraft.selectedWeaponProfileName ?? "—"}
          highlight
        />
        <Row label="Type" value={aircraft.selectedWeaponType} />
      </dl>
    </div>
  );
}

function Row({
  label,
  value,
  highlight,
}: {
  label: string;
  value: string;
  highlight?: boolean;
}) {
  return (
    <div className="flex justify-between gap-4">
      <dt className="text-slate-400">{label}</dt>
      <dd className={highlight ? "text-cyan-300" : "text-slate-100"}>{value}</dd>
    </div>
  );
}
