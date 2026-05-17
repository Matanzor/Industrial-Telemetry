import axios from 'axios';
import type { SensorReading, SensorHistoryResponse } from '../types/sensor';

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

const api = axios.create({
  baseURL: `${API_URL}/api`,
});

export async function getAllSensors(): Promise<SensorReading[]> {
  const { data } = await api.get<SensorReading[]>('/sensors');
  return data;
}

export async function getSensorById(sensorId: number): Promise<SensorReading> {
  const { data } = await api.get<SensorReading>(`/sensors/${sensorId}`);
  return data;
}

export async function getSensorHistory(
  sensorId: number,
  from?: string,
  to?: string,
  page: number = 1,
  pageSize: number = 100
): Promise<SensorHistoryResponse> {
  const params = new URLSearchParams();
  if (from) params.append('from', new Date(from).toISOString());
  if (to) params.append('to', new Date(to).toISOString());
  params.append('page', page.toString());
  params.append('pageSize', pageSize.toString());

  const { data } = await api.get<SensorHistoryResponse>(
    `/sensors/${sensorId}/history?${params.toString()}`
  );
  return data;
}
