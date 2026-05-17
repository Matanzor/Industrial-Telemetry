import { useEffect, useState, useCallback } from 'react';
import type { SensorReading } from '../types/sensor';
import { signalRService } from '../services/signalrService';
import { getAllSensors } from '../services/api';
import SensorCard from '../components/SensorCard';

export default function Dashboard() {
  const [sensors, setSensors] = useState<Map<number, SensorReading>>(new Map());
  const [connected, setConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleTelemetry = useCallback((reading: SensorReading) => {
    setSensors(prev => {
      const next = new Map(prev);
      next.set(reading.sensorId, reading);
      return next;
    });
  }, []);

  useEffect(() => {
    let unsubscribe: (() => void) | null = null;

    async function init() {
      try {
        // Load initial state from REST API
        const initial = await getAllSensors();
        const map = new Map<number, SensorReading>();
        initial.forEach(s => map.set(s.sensorId, s));
        setSensors(map);
      } catch {
        // If REST fails, we still have SignalR for real-time data
        console.warn('Could not load initial sensor data from REST API');
      }

      try {
        // Start SignalR for real-time updates
        await signalRService.start();
        setConnected(true);
        unsubscribe = signalRService.onTelemetry(handleTelemetry);
      } catch (err) {
        setError('Failed to connect to real-time feed');
        console.error(err);
      }
    }

    init();

    return () => {
      if (unsubscribe) unsubscribe();
    };
  }, [handleTelemetry]);

  const sensorArray = Array.from(sensors.values()).sort((a, b) => a.sensorId - b.sensorId);

  const statusCounts = {
    Normal: sensorArray.filter(s => s.status === 'Normal').length,
    Warning: sensorArray.filter(s => s.status === 'Warning').length,
    Critical: sensorArray.filter(s => s.status === 'Critical').length,
  };

  return (
    <div>
      {/* Status bar */}
      <div style={{ display: 'flex', gap: 16, marginBottom: 24, alignItems: 'center' }}>
        <h2 style={{ margin: 0, fontSize: 24, fontWeight: 700 }}>Live Dashboard</h2>
        <div style={{
          width: 8, height: 8, borderRadius: '50%',
          backgroundColor: connected ? '#22c55e' : '#ef4444',
          boxShadow: connected ? '0 0 8px #22c55e' : '0 0 8px #ef4444',
        }} />
        <span style={{ fontSize: 12, color: '#64748b' }}>
          {connected ? 'Connected' : 'Disconnected'} · {sensorArray.length}/20 sensors
        </span>
        <div style={{ marginLeft: 'auto', display: 'flex', gap: 12, fontSize: 13 }}>
          <span style={{ color: '#22c55e' }}>● {statusCounts.Normal} Normal</span>
          <span style={{ color: '#eab308' }}>● {statusCounts.Warning} Warning</span>
          <span style={{ color: '#ef4444' }}>● {statusCounts.Critical} Critical</span>
        </div>
      </div>

      {error && (
        <div style={{ background: '#7f1d1d', padding: 12, borderRadius: 8, marginBottom: 16, fontSize: 14 }}>
          {error}
        </div>
      )}

      {/* Sensor grid: 4 columns × 5 rows = 20 sensors */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(4, 1fr)',
        gap: 16,
      }}>
        {sensorArray.map(reading => (
          <SensorCard key={reading.sensorId} reading={reading} />
        ))}
      </div>

      {sensorArray.length === 0 && !error && (
        <div style={{ textAlign: 'center', color: '#64748b', padding: 60, fontSize: 16 }}>
          Waiting for sensor data...
        </div>
      )}
    </div>
  );
}
