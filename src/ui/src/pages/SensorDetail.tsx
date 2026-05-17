import { useEffect, useState, useCallback, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import type { SensorReading } from '../types/sensor';
import { signalRService } from '../services/signalrService';
import { getSensorById } from '../services/api';

const MAX_CHART_POINTS = 60; // 1 minute of data at 1/sec

export default function SensorDetail() {
  const { sensorId } = useParams<{ sensorId: string }>();
  const navigate = useNavigate();
  const id = Number(sensorId);

  const [current, setCurrent] = useState<SensorReading | null>(null);
  const [history, setHistory] = useState<{ time: string; value: number }[]>([]);
  const [connected, setConnected] = useState(false);
  const unsubRef = useRef<(() => void) | null>(null);

  const handleSensorTelemetry = useCallback((reading: SensorReading) => {
    setCurrent(reading);
    setHistory(prev => {
      const next = [...prev, {
        time: new Date(reading.timestamp).toLocaleTimeString(),
        value: reading.value,
      }];
      return next.length > MAX_CHART_POINTS ? next.slice(-MAX_CHART_POINTS) : next;
    });
  }, []);

  useEffect(() => {
    if (isNaN(id) || id < 1 || id > 20) {
      navigate('/');
      return;
    }

    async function init() {
      try {
        const sensor = await getSensorById(id);
        setCurrent(sensor);
        setHistory([{
          time: new Date(sensor.timestamp).toLocaleTimeString(),
          value: sensor.value,
        }]);
      } catch {
        console.warn('Could not load initial sensor data');
      }

      try {
        await signalRService.start();
        setConnected(true);
        const unsub = await signalRService.subscribeToSensor(id, handleSensorTelemetry);
        unsubRef.current = unsub;
      } catch (err) {
        console.error('SignalR error:', err);
      }
    }

    init();

    return () => {
      if (unsubRef.current) {
        unsubRef.current();
      }
    };
  }, [id, navigate, handleSensorTelemetry]);

  const statusColors: Record<string, string> = {
    Normal: '#22c55e',
    Warning: '#eab308',
    Critical: '#ef4444',
  };

  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 24 }}>
        <button
          onClick={() => navigate('/')}
          style={{
            background: '#334155', border: 'none', color: '#94a3b8',
            padding: '6px 12px', borderRadius: 6, cursor: 'pointer', fontSize: 14,
          }}
        >
          ← Back
        </button>
        <h2 style={{ margin: 0, fontSize: 24 }}>Sensor #{id}</h2>
        <div style={{
          width: 8, height: 8, borderRadius: '50%',
          backgroundColor: connected ? '#22c55e' : '#ef4444',
        }} />
      </div>

      {current && (
        <>
          {/* Large value display */}
          <div style={{
            background: '#1e293b', borderRadius: 12, padding: 32,
            display: 'flex', gap: 40, alignItems: 'center', marginBottom: 24,
            border: `1px solid ${statusColors[current.status] || '#334155'}44`,
          }}>
            <div>
              <div style={{ fontSize: 14, color: '#94a3b8', marginBottom: 4 }}>{current.sensorType}</div>
              <div style={{ fontSize: 56, fontWeight: 700, color: '#f1f5f9' }}>
                {current.value.toFixed(1)}
                <span style={{ fontSize: 24, color: '#64748b', marginLeft: 8 }}>{current.unit}</span>
              </div>
              <div style={{
                display: 'inline-block', marginTop: 8,
                padding: '4px 12px', borderRadius: 12,
                backgroundColor: `${statusColors[current.status]}22`,
                color: statusColors[current.status],
                fontWeight: 600, fontSize: 14,
              }}>
                {current.status}
              </div>
            </div>
            <div style={{ color: '#64748b', fontSize: 14 }}>
              <div>Sensor ID: {current.sensorId}</div>
              <div>Type: {current.sensorType}</div>
              <div>Last Update: {new Date(current.timestamp).toLocaleString()}</div>
            </div>
          </div>

          {/* Real-time chart */}
          <div style={{ background: '#1e293b', borderRadius: 12, padding: 24, border: '1px solid #334155' }}>
            <h3 style={{ margin: '0 0 16px', fontSize: 16, color: '#94a3b8' }}>
              Live Chart (last {MAX_CHART_POINTS}s)
            </h3>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={history}>
                <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
                <XAxis dataKey="time" stroke="#64748b" fontSize={11} />
                <YAxis stroke="#64748b" fontSize={11} domain={['auto', 'auto']} />
                <Tooltip
                  contentStyle={{ backgroundColor: '#1e293b', border: '1px solid #334155', borderRadius: 8 }}
                  labelStyle={{ color: '#94a3b8' }}
                />
                <Line
                  type="monotone"
                  dataKey="value"
                  stroke="#38bdf8"
                  strokeWidth={2}
                  dot={false}
                  isAnimationActive={false}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </>
      )}

      {!current && (
        <div style={{ textAlign: 'center', color: '#64748b', padding: 60, fontSize: 16 }}>
          Loading sensor data...
        </div>
      )}
    </div>
  );
}
