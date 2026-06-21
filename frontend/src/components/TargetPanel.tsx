import type { TargetState } from "../types/advisor";

interface Props {
  target: TargetState;
}

function fmt(value?: number, digits = 1, suffix = "") {
  if (value === undefined || value === null) return "—";
  return `${value.toFixed(digits)}${suffix}`;
}

export function TargetPanel({ target }: Props) {
  const statusText =
    target.statusMessage ??
    (target.isLocked ? "Target locked" : "No target locked");

  return (
    <div className="rounded-lg border border-slate-700 bg-slate-900/80 p-4">
      <h2 className="text-xs font-semibold uppercase tracking-widest text-cyan-500">
        Target
      </h2>
      <p
        className={`mt-2 text-sm font-mono ${
          target.isLocked ? "text-emerald-400" : "text-amber-400"
        }`}
      >
        {statusText}
      </p>
      <dl className="mt-3 space-y-2 font-mono text-sm">
        <Row label="Range" value={fmt(target.targetRangeNm, 1, " nm")} />
        <Row label="Altitude" value={fmt(target.targetAltitudeFt, 0, " ft")} />
        <Row label="Mach" value={fmt(target.targetMach, 2)} />
        <Row
          label="Closure"
          value={fmt(target.targetClosureRateKnots, 0, " kts")}
        />
        <Row label="Aspect" value={fmt(target.targetAspectDeg, 0, "°")} />
        <Row label="Aspect Cat" value={target.targetAspectCategory} highlight />
        <Row label="Course" value={fmt(target.targetCourseDeg, 0, "°")} />
        <Row
          label="Jamming"
          value={target.targetIsJamming ? "YES" : "NO"}
          warn={target.targetIsJamming}
        />
        <Row label="Track Mode" value={target.trackingMode} />
      </dl>
    </div>
  );
}

function Row({
  label,
  value,
  highlight,
  warn,
}: {
  label: string;
  value: string;
  highlight?: boolean;
  warn?: boolean;
}) {
  return (
    <div className="flex justify-between gap-4">
      <dt className="text-slate-400">{label}</dt>
      <dd
        className={
          warn
            ? "text-red-400"
            : highlight
              ? "text-cyan-300"
              : "text-slate-100"
        }
      >
        {value}
      </dd>
    </div>
  );
}
