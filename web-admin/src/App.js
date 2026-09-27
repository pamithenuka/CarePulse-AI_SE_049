import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Sidebar from './components/Sidebar';
import { DoctorProvider } from './components/DoctorContext';
import RosterPage from './pages/RosterPage';
import SlotsPage from './pages/SlotsPage';
import ConsultationsPage from './pages/ConsultationsPage';
import './styles.css';

export default function App() {
  return (
    <BrowserRouter>
      <DoctorProvider>
        <div className="app-shell">
          <Sidebar />
          <main className="main">
            <Routes>
              <Route path="/roster" element={<RosterPage />} />
              <Route path="/slots" element={<SlotsPage />} />
              <Route path="/consultations" element={<ConsultationsPage />} />
            </Routes>
          </main>
        </div>
      </DoctorProvider>
    </BrowserRouter>
  );
}
