import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getNurses } from "../api/staffApi";
import { useAuth } from "../context/AuthContext";
import { EmptyState, ErrorState, LoadingState } from "../components/StatusView";
import "../components/StatusView.css";
import "./PatientsListPage.css";

export default function NursesListPage() {
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");

  const [filterStatus, setFilterStatus] = useState("active");
  const [nurses, setNurses] = useState([]);
  const [loadStatus, setLoadStatus] = useState("loading");
  const [errorMessage, setErrorMessage] = useState("");

  const loadNurses = useCallback(async () => {
    setLoadStatus("loading");
    try {
      const data = await getNurses(filterStatus);
      setNurses(data);
      setLoadStatus("success");
    } catch (err) {
      setErrorMessage(err.response?.data?.message || "Failed to load nurses.");
      setLoadStatus("error");
    }
  }, [filterStatus]);

  useEffect(() => {
    loadNurses();
  }, [loadNurses]);

  return (
    <div>
      <div className="page-header-row">
        <div>
          <h1>Nurses</h1>
          <p className="page-subtitle">Registered field nurse accounts.</p>
        </div>
        {isAdmin && (
          <Link to="/nurses/new" className="btn-primary-link">
            + Register Nurse
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

      {loadStatus === "loading" && <LoadingState label="Loading nurses..." />}
      {loadStatus === "error" && <ErrorState message={errorMessage} onRetry={loadNurses} />}
      {loadStatus === "success" && nurses.length === 0 && <EmptyState message="No nurses match the current filter." />}

      {loadStatus === "success" && nurses.length > 0 && (
        <table className="patients-table">
          <thead>
            <tr>
              <th>Full Name</th>
              <th>License Number</th>
              <th>Specialization</th>
              <th>Availability</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {nurses.map((nurse) => (
              <tr key={nurse.id}>
                <td>
                  <Link to={`/nurses/${nurse.id}`}>{nurse.fullName}</Link>
                </td>
                <td>{nurse.licenseNumber}</td>
                <td>{nurse.specialization}</td>
                <td>
                  <span className={`status-pill status-${nurse.isAvailable ? "active" : "inactive"}`}>
                    {nurse.isAvailable ? "Available" : "Unavailable"}
                  </span>
                </td>
                <td>
                  <span className={`status-pill status-${nurse.status.toLowerCase()}`}>{nurse.status}</span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
