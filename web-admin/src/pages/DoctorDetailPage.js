import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { deleteDoctor, getDoctor, restoreDoctor } from "../api/staffApi";
import { useAuth } from "../context/AuthContext";
import { ErrorState, LoadingState } from "../components/StatusView";
import "../components/StatusView.css";
import "../components/patient/PatientTabs.css";
import "./PatientsListPage.css";
import "./PatientDetailPage.css";

export default function DoctorDetailPage() {
  const { id } = useParams();
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");

  const [doctor, setDoctor] = useState(null);
  const [status, setStatus] = useState("loading");
  const [errorMessage, setErrorMessage] = useState("");
  const [actionError, setActionError] = useState("");

  const loadDoctor = useCallback(async () => {
    setStatus("loading");
    try {
      const data = await getDoctor(id);
      setDoctor(data);
      setStatus("success");
    } catch (err) {
      setErrorMessage(err.response?.data?.message || "Failed to load doctor.");
      setStatus("error");
    }
  }, [id]);

  useEffect(() => {
    loadDoctor();
  }, [loadDoctor]);

  const handleDelete = async () => {
    if (!window.confirm(`Delete Dr. ${doctor.fullName}? They will not be able to log in, but an Admin can restore them later.`)) {
      return;
    }
    setActionError("");
    try {
      await deleteDoctor(id);
      await loadDoctor();
    } catch (err) {
      setActionError(err.response?.data?.message || "Failed to delete doctor.");
    }
  };

  const handleRestore = async () => {
    setActionError("");
    try {
      await restoreDoctor(id);
      await loadDoctor();
    } catch (err) {
      setActionError(err.response?.data?.message || "Failed to restore doctor.");
    }
  };

  return (
    <div>
      <div className="page-header-links">
        <Link to="/doctors">&larr; Back to doctors</Link>
      </div>

      {status === "loading" && <LoadingState label="Loading doctor..." />}
      {status === "error" && <ErrorState message={errorMessage} />}

      {status === "success" && doctor && (
        <>
          <div className="page-header-row">
            <div>
              <h1>{doctor.fullName}</h1>
              <p className="page-subtitle">{doctor.specialty}</p>
            </div>
            <div className="detail-header-actions">
              <span className={`status-pill status-${doctor.status.toLowerCase()}`}>{doctor.status}</span>
              {isAdmin && doctor.status === "Active" && (
                <button type="button" onClick={handleDelete} className="btn-danger">
                  Delete
                </button>
              )}
              {isAdmin && doctor.status === "Inactive" && (
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
            <h2>Contact Details</h2>
            <dl>
              <dt>Phone number</dt>
              <dd>{doctor.phoneNumber}</dd>
              <dt>Email</dt>
              <dd>{doctor.email || "-"}</dd>
            </dl>
          </div>
        </>
      )}
    </div>
  );
}
