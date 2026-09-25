import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { deleteNurse, getNurse, restoreNurse } from "../api/staffApi";
import { useAuth } from "../context/AuthContext";
import { ErrorState, LoadingState } from "../components/StatusView";
import "../components/StatusView.css";
import "../components/patient/PatientTabs.css";
import "./PatientsListPage.css";
import "./PatientDetailPage.css";

export default function NurseDetailPage() {
  const { id } = useParams();
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");

  const [nurse, setNurse] = useState(null);
  const [status, setStatus] = useState("loading");
  const [errorMessage, setErrorMessage] = useState("");
  const [actionError, setActionError] = useState("");

  const loadNurse = useCallback(async () => {
    setStatus("loading");
    try {
      const data = await getNurse(id);
      setNurse(data);
      setStatus("success");
    } catch (err) {
      setErrorMessage(err.response?.data?.message || "Failed to load nurse.");
      setStatus("error");
    }
  }, [id]);

  useEffect(() => {
    loadNurse();
  }, [loadNurse]);

  const handleDelete = async () => {
    if (!window.confirm(`Delete ${nurse.fullName}? They will not be able to log in, but an Admin can restore them later.`)) {
      return;
    }
    setActionError("");
    try {
      await deleteNurse(id);
      await loadNurse();
    } catch (err) {
      setActionError(err.response?.data?.message || "Failed to delete nurse.");
    }
  };

  const handleRestore = async () => {
    setActionError("");
    try {
      await restoreNurse(id);
      await loadNurse();
    } catch (err) {
      setActionError(err.response?.data?.message || "Failed to restore nurse.");
    }
  };

  return (
    <div>
      <div className="page-header-links">
        <Link to="/nurses">&larr; Back to nurses</Link>
      </div>

      {status === "loading" && <LoadingState label="Loading nurse..." />}
      {status === "error" && <ErrorState message={errorMessage} />}

      {status === "success" && nurse && (
        <>
          <div className="page-header-row">
            <div>
              <h1>{nurse.fullName}</h1>
              <p className="page-subtitle">{nurse.specialization}</p>
            </div>
            <div className="detail-header-actions">
              <span className={`status-pill status-${nurse.isAvailable ? "active" : "inactive"}`}>
                {nurse.isAvailable ? "Available" : "Unavailable"}
              </span>
              <span className={`status-pill status-${nurse.status.toLowerCase()}`}>{nurse.status}</span>
              {isAdmin && nurse.status === "Active" && (
                <button type="button" onClick={handleDelete} className="btn-danger">
                  Delete
                </button>
              )}
              {isAdmin && nurse.status === "Inactive" && (
                <button type="button" onClick={handleRestore} className="btn-secondary">
                  Restore
                </button>
              )}
            </div>
          </div>

          {actionError && (
            <div className="alert alert-error" role="alert">
              {actionError}
            </div>
          )}

          <div className="detail-card">
            <h2>License</h2>
            <dl>
              <dt>License number</dt>
              <dd>{nurse.licenseNumber}</dd>
            </dl>
          </div>
        </>
      )}
    </div>
  );
}
