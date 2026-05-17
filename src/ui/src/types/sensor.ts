export interface SensorReading {
  sensorId: number;
  sensorType: string;
  value: number;
  unit: string;
  timestamp: string;
  status: 'Normal' | 'Warning' | 'Critical';
}

export interface SensorHistoryResponse {
  readings: SensorReading[];
  totalCount: number;
  page: number;
  pageSize: number;
}
