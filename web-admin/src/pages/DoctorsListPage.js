import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getDoctors } from "../api/staffApi";
import { useAuth } from "../context/AuthContext";
import { EmptyState, ErrorState, LoadingState } from "../components/StatusView";
import "../components/StatusView.css";
import "./PatientsListPage.css";

export default function DoctorsListPage() {
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");

  const [filterStatus, setFilterStatus] = useState("active");
  const [doctors, setDoctors] = useState([]);
  const [loadStatus, setLoadStatus] = useState("loading");
  const [errorMessage, setErrorMessage] = useState("");

  const loadDoctors = useCallback(async () => {
    setLoadStatus("loading");
    try {
      const data = await getDoctors(filterStatus);
      setDoctors(data);
      setLoadStatus("success");
    } catch (err) {
      setErrorMessage(err.response?.data?.message || "Failed to load doctors.");
      setLoadStatus("error");
    }
  }, [filterStatus]);

  useEffect(() => {
    loadDoctors();
  }, [loadDoctors]);

  return (
    <div>
      <div className="page-header-row">
        <div>
          <h1>Doctors</h1>
          <p className="page-subtitle">Registered doctor accounts.</p>
        </div>
        {isAdmin && (
          <Link to="/doctors/new" className="btn-primary-link">
            + Register Doctor
          </Link>
        )}
      </div>

      {isAdmin && (
        <div className="filters">
          <select value={filterStatus} onChange={(e) => setFilterStatus(e.target.value)}>
            <option value="active">Active</option>
            <option value="inactive">Deleted</option>
            <option value="all">All</option>
          </select>
        </div>
      )}

      {loadStatus === "loading" && <LoadingState label="Loading doctors..." />}
      {loadStatus === "error" && <ErrorState message={errorMessage} onRetry={loadDoctors} />}
      {loadStatus === "success" && doctors.length === 0 && <EmptyState message="No doctors match the current filter." />}

      {loadStatus === "success" && doctors.length > 0 && (
        <table className="patients-table">
          <thead>
            <tr>
              <th>Full Name</th>
              <th>Specialty</th>
              <th>Phone</th>
              <th>Email</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {doctors.map((doctor) => (
              <tr key={doctor.id}>
                <td>
                  <Link to={`/doctors/${doctor.id}`}>{doctor.fullName}</Link>
                </td>
                <td>{doctor.specialty}</td>
                <td>{doctor.phoneNumber}</td>
                <td>{doctor.email || "-"}</td>
                <td>
                  <span className={`status-pill status-${doctor.status.toLowerCase()}`}>{doctor.status}</span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
