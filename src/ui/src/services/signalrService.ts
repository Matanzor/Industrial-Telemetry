import * as signalR from '@microsoft/signalr';
import type { SensorReading } from '../types/sensor';

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

class SignalRService {
  private connection: signalR.HubConnection;
  private telemetryCallbacks: ((reading: SensorReading) => void)[] = [];
  private sensorCallbacks: Map<number, ((reading: SensorReading) => void)[]> = new Map();

  constructor() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/telemetry`)
      .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.connection.on('ReceiveTelemetry', (reading: SensorReading) => {
      this.telemetryCallbacks.forEach(cb => cb(reading));
    });

    this.connection.on('ReceiveSensorTelemetry', (reading: SensorReading) => {
      const callbacks = this.sensorCallbacks.get(reading.sensorId) || [];
      callbacks.forEach(cb => cb(reading));
    });

    this.connection.onreconnecting(() => {
      console.log('SignalR reconnecting...');
    });

    this.connection.onreconnected(() => {
      console.log('SignalR reconnected');
    });
  }

  async start(): Promise<void> {
    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      await this.connection.start();
      console.log('SignalR connected');
    }
  }

  async stop(): Promise<void> {
    await this.connection.stop();
  }

  onTelemetry(callback: (reading: SensorReading) => void): () => void {
    this.telemetryCallbacks.push(callback);
    return () => {
      this.telemetryCallbacks = this.telemetryCallbacks.filter(cb => cb !== callback);
    };
  }

  async subscribeToSensor(sensorId: number, callback: (reading: SensorReading) => void): Promise<() => void> {
    await this.connection.invoke('SubscribeToSensor', sensorId);
    const callbacks = this.sensorCallbacks.get(sensorId) || [];
    callbacks.push(callback);
    this.sensorCallbacks.set(sensorId, callbacks);

    return async () => {
      const cbs = this.sensorCallbacks.get(sensorId) || [];
      this.sensorCallbacks.set(sensorId, cbs.filter(cb => cb !== callback));
      if (this.sensorCallbacks.get(sensorId)?.length === 0) {
        await this.connection.invoke('UnsubscribeFromSensor', sensorId);
      }
    };
  }

  getState(): signalR.HubConnectionState {
    return this.connection.state;
  }
}

export const signalRService = new SignalRService();
