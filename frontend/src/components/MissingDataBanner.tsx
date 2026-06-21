import type { ShotQualityResult } from "../types/advisor";

interface Props {
  shotQuality: ShotQualityResult;
}

export function MissingDataBanner({ shotQuality }: Props) {
  if (shotQuality.canCalculate || shotQuality.missingDataFields.length === 0) {
    return null;
  }

  return (
    <div className="rounded-lg border border-amber-700/50 bg-amber-950/40 p-4">
      <h3 className="text-sm font-semibold text-amber-400">Missing Data</h3>
      <ul className="mt-2 flex flex-wrap gap-2">
        {shotQuality.missingDataFields.map((field) => (
          <li
            key={field}
            className="rounded bg-amber-900/40 px-2 py-1 font-mono text-xs text-amber-200"
          >
            {field}
          </li>
        ))}
      </ul>
    </div>
  );
}
