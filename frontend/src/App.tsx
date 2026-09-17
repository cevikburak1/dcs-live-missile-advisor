import { useAdvisorHub } from "./hooks/useAdvisorHub";
import { ConnectionStatus } from "./components/ConnectionStatus";
import { AircraftPanel } from "./components/AircraftPanel";
import { WeaponPanel } from "./components/WeaponPanel";
import { TargetPanel } from "./components/TargetPanel";
import { ShotQualityPanel } from "./components/ShotQualityPanel";
import { RangeBar } from "./components/RangeBar";
import { MissingDataBanner } from "./components/MissingDataBanner";

export default function App() {
  const { snapshot, hubStatus, dcsStatus } = useAdvisorHub();

  const aircraft = snapshot?.aircraft ?? {
    selectedWeaponType: "Unknown" as const,
    ownshipExportAvailable: false,
  };
  const target = snapshot?.target ?? {
    isLocked: false,
    sensorExportAvailable: false,
    targetAspectCategory: "Unknown" as const,
    targetIsJamming: false,
    trackingMode: "Unknown" as const,
  };
  const shotQuality = snapshot?.shotQuality ?? {
    canCalculate: false,
    shotCategory: "Unknown" as const,
    recommendation: "Unknown" as const,
    explanation: "",
    missingDataFields: [],
  };

  return (
    <div className="min-h-screen p-4 md:p-6">
      <header className="mb-6 border-b border-slate-800 pb-4">
        <h1 className="text-xl font-bold tracking-tight text-slate-100">
          DCS Live Universal Missile Advisor
        </h1>
        <p className="mt-1 text-sm text-slate-400 font-mono">
          Live telemetry · No mock data · Profile-driven PK
        </p>
      </header>

      <div className="grid gap-4 lg:grid-cols-3">
        <ConnectionStatus
          dcsStatus={dcsStatus}
          hubStatus={hubStatus}
          permissions={snapshot?.permissions}
        />
        <AircraftPanel aircraft={aircraft} />
        <WeaponPanel aircraft={aircraft} />
      </div>

      <div className="mt-4 grid gap-4 lg:grid-cols-2">
        <TargetPanel target={target} />
        <ShotQualityPanel shotQuality={shotQuality} />
      </div>

      <div className="mt-4">
        <MissingDataBanner shotQuality={shotQuality} />
      </div>

      <div className="mt-4">
        <RangeBar
          profile={shotQuality.activeMissileProfile}
          currentRangeNm={target.targetRangeNm}
        />
      </div>

      {snapshot?.packetSeq != null && (
        <footer className="mt-6 text-center font-mono text-xs text-slate-600">
          pkt #{snapshot.packetSeq}
          {snapshot.modelTime != null && ` · t=${snapshot.modelTime.toFixed(1)}s`}
        </footer>
      )}
    </div>
  );
}
