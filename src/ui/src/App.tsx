import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';
import Dashboard from './pages/Dashboard';
import SensorDetail from './pages/SensorDetail';
import HistoricalData from './pages/HistoricalData';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<Dashboard />} />
          <Route path="/sensor/:sensorId" element={<SensorDetail />} />
          <Route path="/historical" element={<HistoricalData />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
