import { useState } from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import type { SensorReading } from '../types/sensor';
import { getSensorHistory } from '../services/api';

export default function HistoricalData() {
  const [sensorId, setSensorId] = useState(1);
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [readings, setReadings] = useState<SensorReading[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const pageSize = 200;

  async function handleSearch(newPage: number = 1) {
    setLoading(true);
    setError(null);
    try {
      const result = await getSensorHistory(
        sensorId,
        from || undefined,
        to || undefined,
        newPage,
        pageSize
      );
      setReadings(result.readings);
      setTotalCount(result.totalCount);
      setPage(newPage);
    } catch (err) {
      setError('Failed to fetch historical data. Make sure the system is running.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  }

  const chartData = [...readings]
    .reverse()
    .map(r => ({
      time: new Date(r.timestamp).toLocaleTimeString(),
      value: r.value,
    }));

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div>
      <h2 style={{ margin: '0 0 24px', fontSize: 24, fontWeight: 700 }}>Historical Data</h2>

      {/* Query Form */}
      <div style={{
        background: '#1e293b', borderRadius: 12, padding: 20,
        border: '1px solid #334155', marginBottom: 24,
        display: 'flex', gap: 16, alignItems: 'flex-end', flexWrap: 'wrap',
      }}>
        <div>
          <label style={{ display: 'block', fontSize: 12, color: '#94a3b8', marginBottom: 4 }}>Sensor</label>
          <select
            value={sensorId}
            onChange={e => setSensorId(Number(e.target.value))}
            style={{
              background: '#0f172a', color: '#e2e8f0', border: '1px solid #475569',
              borderRadius: 6, padding: '8px 12px', fontSize: 14,
            }}
          >
            {Array.from({ length: 20 }, (_, i) => i + 1).map(id => (
              <option key={id} value={id}>Sensor #{id}</option>
            ))}
          </select>
        </div>
        <div>
          <label style={{ display: 'block', fontSize: 12, color: '#94a3b8', marginBottom: 4 }}>From</label>
          <input
            type="datetime-local"
            value={from}
            onChange={e => setFrom(e.target.value)}
            style={{
              background: '#0f172a', color: '#e2e8f0', border: '1px solid #475569',
              borderRadius: 6, padding: '8px 12px', fontSize: 14,
            }}
          />
        </div>
        <div>
          <label style={{ display: 'block', fontSize: 12, color: '#94a3b8', marginBottom: 4 }}>To</label>
          <input
            type="datetime-local"
            value={to}
            onChange={e => setTo(e.target.value)}
            style={{
              background: '#0f172a', color: '#e2e8f0', border: '1px solid #475569',
              borderRadius: 6, padding: '8px 12px', fontSize: 14,
            }}
          />
        </div>
        <button
          onClick={() => handleSearch(1)}
          disabled={loading}
          style={{
            background: '#2563eb', color: 'white', border: 'none',
            borderRadius: 6, padding: '8px 24px', fontSize: 14,
            cursor: loading ? 'not-allowed' : 'pointer', fontWeight: 600,
          }}
        >
          {loading ? 'Loading...' : 'Search'}
        </button>
      </div>

      {error && (
        <div style={{ background: '#7f1d1d', padding: 12, borderRadius: 8, marginBottom: 16, fontSize: 14 }}>
          {error}
        </div>
      )}

      {/* Chart */}
      {chartData.length > 0 && (
        <div style={{ background: '#1e293b', borderRadius: 12, padding: 24, border: '1px solid #334155', marginBottom: 24 }}>
          <h3 style={{ margin: '0 0 16px', fontSize: 16, color: '#94a3b8' }}>
            Sensor #{sensorId} — {totalCount} readings
          </h3>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
              <XAxis dataKey="time" stroke="#64748b" fontSize={11} />
              <YAxis stroke="#64748b" fontSize={11} domain={['auto', 'auto']} />
              <Tooltip
                contentStyle={{ backgroundColor: '#1e293b', border: '1px solid #334155', borderRadius: 8 }}
                labelStyle={{ color: '#94a3b8' }}
              />
              <Line type="monotone" dataKey="value" stroke="#38bdf8" strokeWidth={2} dot={false} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* Data Table */}
      {readings.length > 0 && (
        <div style={{ background: '#1e293b', borderRadius: 12, border: '1px solid #334155', overflow: 'hidden' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
            <thead>
              <tr style={{ borderBottom: '1px solid #334155' }}>
                {['Sensor', 'Type', 'Value', 'Unit', 'Status', 'Timestamp'].map(h => (
                  <th key={h} style={{ padding: '12px 16px', textAlign: 'left', color: '#94a3b8', fontWeight: 600 }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {readings.map((r, i) => (
                <tr key={i} style={{ borderBottom: '1px solid #1e293b' }}>
                  <td style={{ padding: '10px 16px' }}>#{r.sensorId}</td>
                  <td style={{ padding: '10px 16px' }}>{r.sensorType}</td>
                  <td style={{ padding: '10px 16px', fontWeight: 600 }}>{r.value.toFixed(2)}</td>
                  <td style={{ padding: '10px 16px', color: '#64748b' }}>{r.unit}</td>
                  <td style={{ padding: '10px 16px' }}>
                    <span style={{
                      padding: '2px 8px', borderRadius: 10, fontSize: 11, fontWeight: 600,
                      color: r.status === 'Critical' ? '#ef4444' : r.status === 'Warning' ? '#eab308' : '#22c55e',
                      backgroundColor: r.status === 'Critical' ? '#ef444422' : r.status === 'Warning' ? '#eab30822' : '#22c55e22',
                    }}>
                      {r.status}
                    </span>
                  </td>
                  <td style={{ padding: '10px 16px', color: '#64748b' }}>{new Date(r.timestamp).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* Pagination */}
          {totalPages > 1 && (
            <div style={{ padding: 16, display: 'flex', justifyContent: 'center', gap: 8 }}>
              <button
                onClick={() => handleSearch(page - 1)}
                disabled={page <= 1 || loading}
                style={{ background: '#334155', color: '#e2e8f0', border: 'none', borderRadius: 6, padding: '6px 12px', cursor: 'pointer' }}
              >
                Previous
              </button>
              <span style={{ padding: '6px 12px', color: '#94a3b8', fontSize: 13 }}>
                Page {page} of {totalPages}
              </span>
              <button
                onClick={() => handleSearch(page + 1)}
                disabled={page >= totalPages || loading}
                style={{ background: '#334155', color: '#e2e8f0', border: 'none', borderRadius: 6, padding: '6px 12px', cursor: 'pointer' }}
              >
                Next
              </button>
            </div>
          )}
        </div>
      )}

      {readings.length === 0 && !loading && !error && (
        <div style={{ textAlign: 'center', color: '#64748b', padding: 60, fontSize: 16 }}>
          Select a sensor and date range, then click Search to view historical data.
        </div>
      )}
    </div>
  );
}
