import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { deactivatePatient, getPatientProfile, reactivatePatient, triggerEmergencyAlert } from "../api/patientsApi";
import { useAuth } from "../context/AuthContext";
import { EmptyState, ErrorState, LoadingState } from "../components/StatusView";
import OverviewTab from "../components/patient/OverviewTab";
import MedicalHistoryTab from "../components/patient/MedicalHistoryTab";
import EmergencyContactsTab from "../components/patient/EmergencyContactsTab";
import DocumentsTab from "../components/patient/DocumentsTab";
import ConsultationHistoryTab from "../components/patient/ConsultationHistoryTab";
import AiPlanTab from "../components/patient/AiPlanTab";
import "../components/StatusView.css";
import "./PatientDetailPage.css";

const TABS = [
  { key: "overview", label: "Overview" },
  { key: "history", label: "Medical History & Medications" },
  { key: "contacts", label: "Emergency Contacts" },
  { key: "documents", label: "Documents" },
  { key: "consultations", label: "Consultation History" },
  { key: "ai-plan", label: "AI Care Plan" },
];

export default function PatientDetailPage() {
  const { id } = useParams();
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");

  const [profile, setProfile] = useState(null);
  const [status, setStatus] = useState("loading");
  const [errorMessage, setErrorMessage] = useState("");
  const [activeTab, setActiveTab] = useState("overview");
  const [actionError, setActionError] = useState("");
  const [isTriggeringAlert, setIsTriggeringAlert] = useState(false);
  const [alertResult, setAlertResult] = useState(null);

  const loadProfile = useCallback(async () => {
    setStatus("loading");
    try {
      const data = await getPatientProfile(id);
      setProfile(data);
      setStatus("success");
    } catch (err) {
      setErrorMessage(err.response?.data?.message || "Failed to load patient profile.");
      setStatus("error");
    }
  }, [id]);

  useEffect(() => {
    loadProfile();
  }, [loadProfile]);

  const handleDeactivate = async () => {
    if (!window.confirm(`Deactivate ${profile.fullName}? They will be hidden from the active registry.`)) {
      return;
    }
    setActionError("");
    try {
      await deactivatePatient(id);
      await loadProfile();
    } catch (err) {
      setActionError(err.response?.data?.message || "Failed to deactivate patient.");
    }
  };

  const handleReactivate = async () => {
    setActionError("");
    try {
      await reactivatePatient(id);
      await loadProfile();
    } catch (err) {
      setActionError(err.response?.data?.message || "Failed to reactivate patient.");
    }
  };

  const handleTriggerAlert = async () => {
    if (!window.confirm(`Trigger an emergency alert for ${profile.fullName}? Their emergency contacts will be notified.`)) {
      return;
    }
    setIsTriggeringAlert(true);
    setActionError("");
    try {
      const result = await triggerEmergencyAlert(id);
      setAlertResult(result);
      await loadProfile();
    } catch (err) {
      setActionError(err.response?.data?.message || "Failed to trigger emergency alert.");
    } finally {
      setIsTriggeringAlert(false);
    }
  };

  if (status === "loading") return <LoadingState label="Loading patient profile..." />;
  if (status === "error") return <ErrorState message={errorMessage} onRetry={loadProfile} />;
  if (!profile) return <EmptyState message="Patient profile not found." />;

  return (
    <div className="patient-detail">
      <Link to="/patients" className="back-link">
        &larr; Back to registry
      </Link>

      <div className="detail-header">
        <div className="detail-header-titles">
          <h1>
            {profile.fullName}
            {profile.allergies && <span className="allergy-badge" title={profile.allergies}>⚠ Allergy</span>}
            <span className={`status-pill status-${profile.status.toLowerCase()}`}>{profile.status}</span>
          </h1>
          <p className="page-subtitle">
            {profile.age}y &middot; {profile.gender} &middot; Blood type {profile.bloodType || "unknown"}
          </p>
        </div>
        <div className="detail-header-actions">
          <button type="button" onClick={handleTriggerAlert} disabled={isTriggeringAlert} className="btn-alert">
            {isTriggeringAlert ? "Triggering..." : "Trigger Emergency Alert"}
          </button>
          {isAdmin && profile.status === "Active" && (
            <button type="button" onClick={handleDeactivate} className="btn-danger">
              Deactivate
            </button>
          )}
          {isAdmin && profile.status === "Inactive" && (
            <button type="button" onClick={handleReactivate} className="btn-secondary">
              Reactivate
            </button>
          )}
        </div>
      </div>

      {actionError && (
        <div className="alert alert-error" role="alert">
          {actionError}
        </div>
      )}

      {alertResult && (
        <div className="alert alert-success" role="status">
          Emergency alert sent to {alertResult.notifiedContacts.length} contact(s) at{" "}
          {new Date(alertResult.triggeredAt).toLocaleString()}.
          <button type="button" className="alert-dismiss" onClick={() => setAlertResult(null)}>
            Dismiss
          </button>
        </div>
      )}

      <div className="tab-bar" role="tablist">
        {TABS.map((tab) => (
          <button
            key={tab.key}
            role="tab"
            aria-selected={activeTab === tab.key}
            className={`tab-button ${activeTab === tab.key ? "active" : ""}`}
            onClick={() => setActiveTab(tab.key)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <div className="tab-content">
        {activeTab === "overview" && (
          <OverviewTab profile={profile} isAdmin={isAdmin} onUpdated={loadProfile} />
        )}
        {activeTab === "history" && <MedicalHistoryTab patientId={id} initialHistory={profile.medicalHistories} onChanged={loadProfile} />}
        {activeTab === "contacts" && (
          <EmergencyContactsTab patientId={id} initialContacts={profile.emergencyContacts} isAdmin={isAdmin} onChanged={loadProfile} />
        )}
        {activeTab === "documents" && (
          <DocumentsTab patientId={id} initialDocuments={profile.medicalDocuments} isAdmin={isAdmin} onChanged={loadProfile} />
        )}
        {activeTab === "consultations" && <ConsultationHistoryTab patientId={id} />}
        {activeTab === "ai-plan" && <AiPlanTab patientId={id} />}
      </div>
    </div>
  );
}
