import type { MissileProfile } from "../types/advisor";

interface Props {
  profile?: MissileProfile;
  currentRangeNm?: number;
}

export function RangeBar({ profile, currentRangeNm }: Props) {
  if (!profile) {
    return (
      <div className="rounded-lg border border-slate-700 bg-slate-900/80 p-4">
        <h2 className="text-xs font-semibold uppercase tracking-widest text-cyan-500">
          Range Envelope
        </h2>
        <p className="mt-2 text-sm text-slate-400 font-mono">
          Select a missile with a known profile
        </p>
      </div>
    );
  }

  const max = profile.maxEffectiveRangeNm;
  const min = 0;
  const span = max - min;

  const pct = (nm: number) => ((nm - min) / span) * 100;
  const currentPct =
    currentRangeNm != null
      ? Math.min(100, Math.max(0, pct(currentRangeNm)))
      : null;

  return (
    <div className="rounded-lg border border-slate-700 bg-slate-900/80 p-4">
      <h2 className="text-xs font-semibold uppercase tracking-widest text-cyan-500">
        Range Envelope — {profile.missileName}
      </h2>
      {profile.notes && (
        <p className="mt-2 text-xs text-slate-400">{profile.notes}</p>
      )}

      <div className="relative mt-6 h-8 rounded bg-slate-800 overflow-hidden">
        <div
          className="absolute top-0 bottom-0 bg-red-900/60"
          style={{
            left: "0%",
            width: `${pct(profile.minimumRangeNm)}%`,
          }}
        />
        <div
          className="absolute top-0 bottom-0 bg-emerald-800/50"
          style={{
            left: `${pct(profile.minimumRangeNm)}%`,
            width: `${pct(profile.noEscapeRangeNm) - pct(profile.minimumRangeNm)}%`,
          }}
        />
        <div
          className="absolute top-0 bottom-0 bg-emerald-600/40"
          style={{
            left: `${pct(profile.idealRangeNm) - 5}%`,
            width: "10%",
          }}
        />
        <div
          className="absolute top-0 bottom-0 bg-slate-700/40"
          style={{
            left: `${pct(profile.noEscapeRangeNm)}%`,
            width: `${100 - pct(profile.noEscapeRangeNm)}%`,
          }}
        />

        {currentPct !== null && (
          <div
            className="absolute top-0 bottom-0 w-0.5 bg-cyan-400 shadow-[0_0_8px_cyan]"
            style={{ left: `${currentPct}%` }}
          />
        )}
      </div>

      <div className="mt-3 grid grid-cols-4 gap-2 font-mono text-xs text-slate-400">
        <Legend label="MIN" value={`${profile.minimumRangeNm} nm`} />
        <Legend label="NEZ" value={`${profile.noEscapeRangeNm} nm`} />
        <Legend label="IDEAL" value={`${profile.idealRangeNm} nm`} />
        <Legend label="MAX" value={`${profile.maxEffectiveRangeNm} nm`} />
      </div>

      {currentRangeNm != null && (
        <p className="mt-2 font-mono text-sm text-cyan-300">
          Current range: {currentRangeNm.toFixed(1)} nm
        </p>
      )}
    </div>
  );
}

function Legend({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="text-slate-500">{label}</div>
      <div className="text-slate-200">{value}</div>
    </div>
  );
}
