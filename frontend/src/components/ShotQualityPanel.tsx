import type { ShotQualityResult } from "../types/advisor";

interface Props {
  shotQuality: ShotQualityResult;
}

const categoryColors: Record<string, string> = {
  HighPk: "bg-emerald-600",
  MediumPk: "bg-cyan-600",
  LowPk: "bg-amber-600",
  PressureShot: "bg-orange-600",
  DoNotFire: "bg-red-600",
  Unknown: "bg-slate-600",
};

const categoryLabels: Record<string, string> = {
  HighPk: "High PK",
  MediumPk: "Medium PK",
  LowPk: "Low PK",
  PressureShot: "Pressure Shot",
  DoNotFire: "Do Not Fire",
  Unknown: "Unknown",
};

export function ShotQualityPanel({ shotQuality }: Props) {
  const pk = shotQuality.estimatedPkPercent;
  const categoryClass =
    categoryColors[shotQuality.shotCategory] ?? categoryColors.Unknown;
  const categoryLabel =
    categoryLabels[shotQuality.shotCategory] ?? shotQuality.shotCategory;

  return (
    <div className="rounded-lg border border-slate-700 bg-slate-900/80 p-4">
      <h2 className="text-xs font-semibold uppercase tracking-widest text-cyan-500">
        Shot Quality
      </h2>

      {!shotQuality.canCalculate && (
        <p className="mt-2 text-sm text-amber-400">
          {shotQuality.explanation || "Insufficient data for PK calculation"}
        </p>
      )}

      <div className="mt-4 flex items-center gap-6">
        <div className="text-center">
          <div className="text-4xl font-bold font-mono text-cyan-300">
            {pk !== undefined ? `${pk.toFixed(1)}%` : "—"}
          </div>
          <div className="text-xs text-slate-400 uppercase tracking-wide">
            Est. PK
          </div>
        </div>
        <div>
          <span
            className={`inline-block rounded px-3 py-1 text-sm font-semibold text-white ${categoryClass}`}
          >
            {categoryLabel}
          </span>
          <div className="mt-2 font-mono text-sm text-cyan-200">
            {shotQuality.recommendation}
          </div>
        </div>
      </div>

      {shotQuality.explanation && shotQuality.canCalculate && (
        <p className="mt-4 text-sm text-slate-300">{shotQuality.explanation}</p>
      )}
    </div>
  );
}
