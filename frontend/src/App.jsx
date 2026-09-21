import { Routes, Route, Navigate } from 'react-router-dom'
import Sidebar from './components/Sidebar.jsx'
import { DoctorProvider } from './components/DoctorContext.jsx'
import RosterPage from './pages/RosterPage.jsx'
import SlotsPage from './pages/SlotsPage.jsx'
import ConsultationsPage from './pages/ConsultationsPage.jsx'

export default function App() {
  return (
    <DoctorProvider>
      <div className="app-shell">
        <Sidebar />
        <main className="main">
          <Routes>
            <Route path="/" element={<Navigate to="/roster" replace />} />
            <Route path="/roster" element={<RosterPage />} />
            <Route path="/slots" element={<SlotsPage />} />
            <Route path="/consultations" element={<ConsultationsPage />} />
          </Routes>
        </main>
      </div>
    </DoctorProvider>
  )
}
