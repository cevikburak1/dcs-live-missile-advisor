import { useCallback, useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import type { AdvisorSnapshot, DcsConnectionStatus } from "../types/advisor";

const HUB_URL = import.meta.env.VITE_HUB_URL ?? "http://localhost:5000/hubs/advisor";

export function useAdvisorHub() {
  const [snapshot, setSnapshot] = useState<AdvisorSnapshot | null>(null);
  const [hubStatus, setHubStatus] = useState<"connecting" | "connected" | "disconnected">(
    "connecting",
  );

  const refreshSnapshot = useCallback(async () => {
    try {
      const res = await fetch("/api/snapshot");
      if (res.ok) {
        const data = (await res.json()) as AdvisorSnapshot;
        setSnapshot(data);
      }
    } catch {
      // backend may not be running yet
    }
  }, []);

  useEffect(() => {
    refreshSnapshot();
    const statusPoll = window.setInterval(refreshSnapshot, 2000);

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build();

    connection.on("AdvisorUpdate", (data: AdvisorSnapshot) => {
      setSnapshot(data);
    });

    connection.onreconnecting(() => setHubStatus("connecting"));
    connection.onreconnected(() => setHubStatus("connected"));
    connection.onclose(() => setHubStatus("disconnected"));

    connection
      .start()
      .then(() => setHubStatus("connected"))
      .catch(() => setHubStatus("disconnected"));

    return () => {
      window.clearInterval(statusPoll);
      connection.stop();
    };
  }, [refreshSnapshot]);

  const dcsStatus: DcsConnectionStatus = snapshot?.connectionStatus ?? "Disconnected";

  return { snapshot, hubStatus, dcsStatus, refreshSnapshot };
}
