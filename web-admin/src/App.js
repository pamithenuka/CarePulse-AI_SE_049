import { EmergencyControlCenter } from './components/dispatch/EmergencyControlCenter';
import { Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import ProtectedRoute from "./components/ProtectedRoute";
import Layout from "./components/Layout";
import LoginPage from "./pages/LoginPage";
import PatientsListPage from "./pages/PatientsListPage";
import PatientDetailPage from "./pages/PatientDetailPage";
import RegisterPatientPage from "./pages/RegisterPatientPage";
import RegisterDoctorPage from "./pages/RegisterDoctorPage";
import RegisterNursePage from "./pages/RegisterNursePage";
import DoctorsListPage from "./pages/DoctorsListPage";
import DoctorDetailPage from "./pages/DoctorDetailPage";
import NursesListPage from "./pages/NursesListPage";
import NurseDetailPage from "./pages/NurseDetailPage";
import AuditLogPage from "./pages/AuditLogPage";
import EmergencyAlertLogPage from "./pages/EmergencyAlertLogPage";
import UnauthorizedPage from "./pages/UnauthorizedPage";
import NotFoundPage from "./pages/NotFoundPage";
import "./App.css";

function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/unauthorized" element={<UnauthorizedPage />} />

        <Route element={<ProtectedRoute allowedRoles={["Doctor", "Admin"]} />}>
          <Route element={<Layout />}>
            <Route path="/patients" element={<PatientsListPage />} />
            <Route path="/patients/:id" element={<PatientDetailPage />} />
            <Route path="/emergency-alerts" element={<EmergencyAlertLogPage />} />
            <Route path="/dispatch" element={<EmergencyControlCenter />} />
            <Route element={<ProtectedRoute allowedRoles={["Admin"]} />}>
              <Route path="/patients/new" element={<RegisterPatientPage />} />
              <Route path="/doctors/new" element={<RegisterDoctorPage />} />
              <Route path="/nurses/new" element={<RegisterNursePage />} />
              <Route path="/doctors" element={<DoctorsListPage />} />
              <Route path="/doctors/:id" element={<DoctorDetailPage />} />
              <Route path="/nurses" element={<NursesListPage />} />
              <Route path="/nurses/:id" element={<NurseDetailPage />} />
              <Route path="/audit-log" element={<AuditLogPage />} />
            </Route>
          </Route>
        </Route>

        <Route path="/" element={<Navigate to="/patients" replace />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </AuthProvider>
  );
}

export default App;
