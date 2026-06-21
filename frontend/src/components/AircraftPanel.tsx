import type { AircraftState } from "../types/advisor";

interface Props {
  aircraft: AircraftState;
}

function fmt(value?: number, digits = 1, suffix = "") {
  if (value === undefined || value === null) return "—";
  return `${value.toFixed(digits)}${suffix}`;
}

export function AircraftPanel({ aircraft }: Props) {
  return (
    <div className="rounded-lg border border-slate-700 bg-slate-900/80 p-4">
      <h2 className="text-xs font-semibold uppercase tracking-widest text-cyan-500">
        Aircraft
      </h2>
      {!aircraft.ownshipExportAvailable && (
        <p className="mt-2 text-sm text-amber-400">Ownship export unavailable</p>
      )}
      <dl className="mt-3 space-y-2 font-mono text-sm">
        <Row label="Raw Name" value={aircraft.aircraftRawName ?? "—"} />
        <Row label="Profile" value={aircraft.aircraftProfileName ?? "—"} highlight />
        <Row label="Altitude" value={fmt(aircraft.ownAltitudeFt, 0, " ft")} />
        <Row label="IAS" value={fmt(aircraft.ownAirspeedKnots, 0, " kts")} />
        <Row label="TAS" value={fmt(aircraft.ownTrueAirspeedKnots, 0, " kts")} />
        <Row label="Mach" value={fmt(aircraft.ownMach, 2)} />
        <Row label="Heading" value={fmt(aircraft.ownHeadingDeg, 0, "°")} />
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
