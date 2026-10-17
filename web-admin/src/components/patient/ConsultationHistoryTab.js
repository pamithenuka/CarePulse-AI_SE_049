import { useCallback, useEffect, useState } from "react";
import { api } from "../../api/client";
import { EmptyState, ErrorState, LoadingState } from "../StatusView";
import "../StatusView.css";
import "./PatientTabs.css";

const formatWhen = (iso) =>
  iso ? new Date(iso).toLocaleString([], { dateStyle: "medium", timeStyle: "short" }) : "Date unknown";

/** Past consultations for one patient, newest first. */
export default function ConsultationHistoryTab({ patientId }) {
  const [records, setRecords] = useState([]);
  const [status, setStatus] = useState("loading");
  const [errorMessage, setErrorMessage] = useState("");

  const load = useCallback(async () => {
    setStatus("loading");
    try {
      setRecords(await api.getPatientConsultations(patientId));
      setStatus("ready");
    } catch (err) {
      setErrorMessage(err.response?.data?.message || err.message || "Failed to load consultations.");
      setStatus("error");
    }
  }, [patientId]);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <div className="detail-card">
      <h2>Consultation History</h2>
      {status === "loading" && <LoadingState label="Loading consultations..." />}
      {status === "error" && <ErrorState message={errorMessage} onRetry={load} />}
      {status === "ready" && records.length === 0 && <EmptyState message="No consultations recorded yet." />}
      {status === "ready" && records.length > 0 && (
        <ul className="consultation-list">
          {records.map((record) => (
            <li key={record.id} className="consultation-item">
              <div className="consultation-head">
                <strong>{record.doctorName}</strong>
                {record.specialty && <span className="consultation-specialty">{record.specialty}</span>}
                <span className="consultation-when">{formatWhen(record.appointmentStart)}</span>
              </div>
              <p className="consultation-notes">{record.notes}</p>
              {record.prescription && (
                <p className="consultation-prescription">
                  <span>Prescription</span> {record.prescription}
                </p>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
