import type { DcsConnectionStatus, ExportPermissions } from "../types/advisor";

interface Props {
  dcsStatus: DcsConnectionStatus;
  hubStatus: "connecting" | "connected" | "disconnected";
  permissions?: ExportPermissions;
}

function statusColor(status: DcsConnectionStatus) {
  switch (status) {
    case "Connected":
      return "text-emerald-400";
    case "Stale":
      return "text-amber-400";
    default:
      return "text-red-400";
  }
}

export function ConnectionStatus({ dcsStatus, hubStatus, permissions }: Props) {
  return (
    <div className="rounded-lg border border-slate-700 bg-slate-900/80 p-4">
      <h2 className="text-xs font-semibold uppercase tracking-widest text-cyan-500">
        Connection
      </h2>
      <div className="mt-3 grid gap-2 font-mono text-sm">
        <div className="flex justify-between">
          <span className="text-slate-400">DCS Telemetry</span>
          <span className={statusColor(dcsStatus)}>{dcsStatus}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-slate-400">SignalR Hub</span>
          <span
            className={
              hubStatus === "connected" ? "text-emerald-400" : "text-amber-400"
            }
          >
            {hubStatus}
          </span>
        </div>
      </div>
      {permissions && (
        <div className="mt-4 flex flex-wrap gap-2">
          <Badge label="Ownship" active={permissions.ownship} />
          <Badge label="Sensor" active={permissions.sensor} />
          <Badge label="Object" active={permissions.object} />
        </div>
      )}
    </div>
  );
}

function Badge({ label, active }: { label: string; active: boolean }) {
  return (
    <span
      className={`rounded px-2 py-0.5 text-xs font-mono ${
        active
          ? "bg-emerald-900/50 text-emerald-300 border border-emerald-700"
          : "bg-slate-800 text-slate-500 border border-slate-700"
      }`}
    >
      {label}: {active ? "ON" : "OFF"}
    </span>
  );
}
