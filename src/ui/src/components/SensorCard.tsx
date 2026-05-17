import type { SensorReading } from '../types/sensor';
import { useNavigate } from 'react-router-dom';

const statusColors: Record<string, string> = {
  Normal: '#22c55e',
  Warning: '#eab308',
  Critical: '#ef4444',
};

const sensorTypeIcons: Record<string, string> = {
  Temperature: '🌡️',
  Pressure: '🔵',
  Humidity: '💧',
  Vibration: '📳',
  Speed: '⚡',
};

interface SensorCardProps {
  reading: SensorReading;
}

export default function SensorCard({ reading }: SensorCardProps) {
  const navigate = useNavigate();
  const statusColor = statusColors[reading.status] || '#64748b';

  return (
    <div
      onClick={() => navigate(`/sensor/${reading.sensorId}`)}
      style={{
        background: '#1e293b',
        border: `1px solid ${reading.status === 'Critical' ? '#ef4444' : '#334155'}`,
        borderRadius: 12,
        padding: 16,
        cursor: 'pointer',
        transition: 'all 0.2s',
        boxShadow: reading.status === 'Critical'
          ? '0 0 12px rgba(239,68,68,0.3)'
          : '0 1px 3px rgba(0,0,0,0.3)',
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = 'translateY(-2px)';
        e.currentTarget.style.borderColor = '#38bdf8';
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = 'translateY(0)';
        e.currentTarget.style.borderColor = reading.status === 'Critical' ? '#ef4444' : '#334155';
      }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
        <span style={{ fontSize: 12, color: '#94a3b8' }}>
          {sensorTypeIcons[reading.sensorType] || '📊'} Sensor #{reading.sensorId}
        </span>
        <span style={{
          fontSize: 11,
          fontWeight: 600,
          padding: '2px 8px',
          borderRadius: 10,
          backgroundColor: `${statusColor}22`,
          color: statusColor,
        }}>
          {reading.status}
        </span>
      </div>
      <div style={{ fontSize: 28, fontWeight: 700, color: '#f1f5f9', marginBottom: 4 }}>
        {reading.value.toFixed(1)}
        <span style={{ fontSize: 14, color: '#64748b', marginLeft: 4 }}>{reading.unit}</span>
      </div>
      <div style={{ fontSize: 12, color: '#64748b' }}>
        {reading.sensorType}
      </div>
      <div style={{ fontSize: 10, color: '#475569', marginTop: 4 }}>
        {new Date(reading.timestamp).toLocaleTimeString()}
      </div>
    </div>
  );
}
