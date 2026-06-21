# DCS Live Universal Missile Advisor

**Version 1.0** — Live missile shot-quality advisor for [DCS World](https://www.dcs.world/). Reads real telemetry from DCS via `Export.lua`, estimates probability of kill (PK), and streams updates to a tactical dashboard while you fly.

No mock data. No manual sliders. No fake targets. If the sim does not provide a value, the UI says so.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Features

- **Live aircraft detection** — identifies your airframe from `LoGetSelfData()`
- **Live weapon detection** — selected station and missile from `LoGetPayloadInfo()` + `LoGetNameByType()`
- **Target lock awareness** — range, aspect, closure, jamming when sensor export allows
- **Profile-driven PK** — missile and aircraft data in JSON, not hardcoded in the engine
- **Shot recommendations** — Fire, Wait, Get Closer, Maintain Lock, Abort, and more
- **Dynamic range bar** — min / NEZ / ideal / max envelope for the selected missile
- **Real-time UI** — SignalR push to a dark tactical React dashboard
- **Optional Electron overlay** — transparent always-on-top window

---

## How it works

```
┌─────────────┐   UDP JSON     ┌──────────────────┐   SignalR   ┌─────────────────┐
│  DCS World  │ ──────────────►│  .NET 8 API      │ ───────────►│  React Dashboard│
│  Export.lua │   localhost    │  Telemetry + PK  │   live      │  (Vite + Tailwind)│
│  ~8 Hz      │   port 17777   │  calculation     │             │                 │
└─────────────┘                └──────────────────┘             └─────────────────┘
```

1. `Export.lua` (LuaSocket) samples DCS export APIs and sends JSON packets to UDP `127.0.0.1:17777`.
2. The backend normalizes units, resolves aircraft/missile profiles, and computes shot quality.
3. The frontend subscribes via SignalR and updates every packet.

---

## Quick start

### Prerequisites

| Requirement | Notes |
|-------------|--------|
| DCS World | Steam or standalone |
| [.NET 8 SDK](https://dotnet.microsoft.com/download) | Backend API |
| [Node.js 18+](https://nodejs.org/) | Frontend dev server |

### 1. Install DCS export scripts

**Enable scripts** — edit `Saved Games/DCS/Config/Export/Config.lua`:

```lua
EnableExportScript = true
```

**Copy scripts** — copy everything from `dcs-export/` to:

```
Saved Games/DCS/Scripts/DcsMissileAdvisor/
```

Required files:

- `Export.lua`
- `json.lua`

**Hook Export.lua** — edit or create `Saved Games/DCS/Scripts/Export.lua`:

```lua
dofile(lfs.writedir() .. "Scripts/DcsMissileAdvisor/Export.lua")
```

If you already use Tacview, DCS-BIOS, or other exporters, **chain** additional `dofile` lines. Do not remove existing hooks.

Default telemetry target: `127.0.0.1:17777` at ~8 Hz. Change `UDP_HOST` / `UDP_PORT` at the top of `Export.lua` if needed.

### 2. Start the backend

```bash
cd backend/DcsMissileAdvisor.Api
dotnet run
```

Runs at `http://localhost:5000`.

| Endpoint | Description |
|----------|-------------|
| `GET /api/health` | Health check |
| `GET /api/snapshot` | Current advisor state |
| `GET /api/profiles/missiles` | Loaded missile profiles |
| `GET /api/profiles/aircraft` | Loaded aircraft profiles |
| `WS /hubs/advisor` | SignalR hub (`AdvisorUpdate` events) |

### 3. Start the frontend

```bash
cd frontend
npm install
npm run dev
```

Open **http://localhost:5173**

### 4. Fly in DCS

Spawn in a mission with export allowed, select a missile, lock a target (when the module supports target export), and watch the dashboard update live.

---

## Dashboard

The UI shows:

- DCS and SignalR connection status
- Export permission badges (ownship / sensor / object)
- Aircraft raw name and matched profile
- Selected weapon, station, missile type
- Target lock status, range, altitude, aspect, closure, jamming
- Estimated PK %, shot category, recommendation, explanation
- Missing-data warnings when calculation is not possible
- Range envelope bar (min, NEZ, ideal, max)

### Status messages (no guessing)

| Message | Meaning |
|---------|---------|
| **No target locked** | Sensor export works but no locked target in telemetry |
| **Missile profile not found** | Weapon detected; add an alias in `missile_profiles.json` |
| **Target lock export unavailable in this mission/server** | Sensor export disabled or API returned nothing |
| **Ownship export unavailable** | Ownship export disabled on server |
| **Insufficient data for PK calculation** | Required fields missing; see missing-data banner |

---

## Profiles

Missile and aircraft behavior is defined in JSON. The calculation service does **not** embed missile range or sensitivity values.

### Missile profiles (`profiles/missile_profiles.json`)

```json
{
  "missileName": "AIM-120C AMRAAM",
  "aliases": ["AIM-120C", "AIM_120C", "AIM-120C-7"],
  "missileType": "Fox3",
  "minimumRangeNm": 2.0,
  "idealRangeNm": 25.0,
  "noEscapeRangeNm": 12.0,
  "maxEffectiveRangeNm": 45.0,
  "needsRadarSupport": true,
  "supportsPitbull": true,
  "highOffBoresight": false,
  "chaffSensitivity": 0.7,
  "flareSensitivity": 0.0,
  "aspectSensitivity": 0.6,
  "altitudeSensitivity": 0.5,
  "closureSensitivity": 0.5,
  "notes": "Active radar BVR missile."
}
```

**`missileType` values:** `Fox1`, `Fox2`, `Fox3`, `RadarBvr`, `InfraredWvr`, `Unknown`

Restart the backend after editing profiles.

### Aircraft profiles (`profiles/aircraft_profiles.json`)

```json
{
  "aircraftName": "F/A-18C Hornet",
  "aliases": ["F/A-18C", "FA-18C", "Hornet"],
  "supportedMissileAliases": ["AIM-120C", "AIM-9X"],
  "radarType": "AN/APG-73",
  "hasBvrRadar": true,
  "hasHmd": true,
  "notes": ""
}
```

### Matching raw DCS weapon names

DCS reports names via `LoGetNameByType()` on the selected station. Strings vary by module and locale.

1. Select the weapon in the sim.
2. Inspect the **Raw Weapon** field on the dashboard, or log `name` from payload stations in `Export.lua`.
3. Add exact strings and common variants to `aliases` in `missile_profiles.json`.

Matching rules:

- Case-insensitive
- Spaces and underscores normalized to hyphens
- Exact alias preferred; substring match only when exactly one profile matches

---

## Multiplayer and module limitations

DCS servers expose three export tiers:

| Server setting | Data affected |
|----------------|---------------|
| Allow player export (ownship) | Airspeed, Mach, altitude, payload, `LoGetSelfData` |
| Sensor export | `LoGetLockedTargetInformation`, `LoGetTargetInformation`, `LoGetTWSInfo` |
| Object export | `LoGetObjectById`, `LoGetWorldObjects` |

**Always available** (even when tiers are off): `LoGetNameByType`, `LoGetModelTime`, coordinate helpers.

### Known limitations

- Many **public servers** disable player export → no ownship or target data.
- **F/A-18C and F-16C:** `LoGetLockedTargetInformation` is often `nil` even in single-player. This is an FC3-era API limitation, not a bug in this tool. Su-27, F-15C, and similar modules tend to work better for target lock export.
- This advisor **never** uses `LoGetWorldObjects` or hidden global object positions.

---

## Optional: Electron overlay

```bash
cd frontend && npm run build
cd ../electron && npm install
ELECTRON_DEV=1 npm run dev   # loads http://localhost:5173
```

Or load the production build:

```bash
cd electron && npm run dev
```

Transparent, frameless, always-on-top window for in-game overlay use.

---

## Project structure

```
profiles/                    Editable JSON profiles
dcs-export/                  Copy to Saved Games/DCS/Scripts/DcsMissileAdvisor/
backend/DcsMissileAdvisor.Api/ .NET 8 Minimal API + SignalR
frontend/                    React + TypeScript + Vite + Tailwind
electron/                    Optional overlay wrapper
```

---

## Contributing

Contributions are welcome, especially:

- New missile and aircraft profiles
- Alias fixes for module-specific weapon names
- Documentation and issue reports from real missions

Please do not submit mock telemetry or cheats that bypass DCS export rules.

---

## License

MIT License — see [LICENSE](LICENSE).

---

## Disclaimer

This is a community flight-aid tool using officially exposed DCS export APIs. It does not modify game files, inject into the sim process, or read hidden enemy positions. PK estimates are **heuristic** guides based on profile data, not guaranteed combat outcomes. Always follow server rules and export policies.
