export type MissileType =
  | "Fox1"
  | "Fox2"
  | "Fox3"
  | "RadarBvr"
  | "InfraredWvr"
  | "Unknown";

export type TargetAspectCategory =
  | "Hot"
  | "Flanking"
  | "Beaming"
  | "Cold"
  | "Unknown";

export type TrackingMode =
  | "RadarLock"
  | "RadarTrack"
  | "TWS"
  | "EOS"
  | "Unknown";

export type ShotCategory =
  | "HighPk"
  | "MediumPk"
  | "LowPk"
  | "PressureShot"
  | "DoNotFire"
  | "Unknown";

export type ShotRecommendation =
  | "Fire"
  | "Wait"
  | "GetCloser"
  | "Climb"
  | "MaintainLock"
  | "Abort"
  | "Unknown";

export type DcsConnectionStatus = "Disconnected" | "Connected" | "Stale";

export interface VelocityVector {
  x?: number;
  y?: number;
  z?: number;
}

export interface MissileProfile {
  missileName: string;
  aliases: string[];
  missileType: MissileType;
  minimumRangeNm: number;
  idealRangeNm: number;
  noEscapeRangeNm: number;
  maxEffectiveRangeNm: number;
  needsRadarSupport: boolean;
  supportsPitbull: boolean;
  highOffBoresight: boolean;
  chaffSensitivity: number;
  flareSensitivity: number;
  aspectSensitivity: number;
  altitudeSensitivity: number;
  closureSensitivity: number;
  notes: string;
}

export interface AircraftState {
  aircraftRawName?: string;
  aircraftProfileName?: string;
  ownAltitudeFt?: number;
  ownAirspeedKnots?: number;
  ownTrueAirspeedKnots?: number;
  ownMach?: number;
  ownHeadingDeg?: number;
  ownVelocityVector?: VelocityVector;
  selectedStation?: number;
  selectedWeaponRawName?: string;
  selectedWeaponProfileName?: string;
  selectedWeaponType: MissileType;
  ownshipExportAvailable: boolean;
}

export interface TargetState {
  dataSource?: string | null;
  isLocked: boolean;
  sensorExportAvailable: boolean;
  statusMessage?: string;
  targetRangeNm?: number;
  targetAltitudeFt?: number;
  targetMach?: number;
  targetClosureRateKnots?: number;
  targetAspectDeg?: number;
  targetAspectCategory: TargetAspectCategory;
  targetCourseDeg?: number;
  targetIsJamming: boolean | null;
  trackingMode: TrackingMode;
}

export interface ShotQualityResult {
  canCalculate: boolean;
  estimatedPkPercent?: number;
  shotCategory: ShotCategory;
  recommendation: ShotRecommendation;
  explanation: string;
  missingDataFields: string[];
  activeMissileProfile?: MissileProfile;
}

export interface ExportPermissions {
  ownship: boolean;
  sensor: boolean;
  object: boolean;
}

export interface AdvisorSnapshot {
  connectionStatus: DcsConnectionStatus;
  permissions: ExportPermissions;
  aircraft: AircraftState;
  target: TargetState;
  shotQuality: ShotQualityResult;
  timestamp: string;
  packetSeq?: number;
  modelTime?: number;
}
