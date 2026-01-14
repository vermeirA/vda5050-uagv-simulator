export interface Vehicle {
  serialNumber: string;
  operatingMode: string;
  status: string;
  position: Position;
}

export interface Position {
  x: number;
  y: number;
}

export interface PositionUpdate extends Position {
  serialNumber: string;
}

export interface VehicleConfig {
  serialNumber: string;
  manufacturer: string;
  mapId: string;
  startX: number;
  startY: number;
}

export interface VdaError {
  errorType: string;
  errorDescription: string;
  errorLevel: "WARNING" | "FATAL";
}
