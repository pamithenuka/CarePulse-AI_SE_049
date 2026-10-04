import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getEmergencyAlertLogs } from "../api/patientsApi";
import { EmptyState, ErrorState, LoadingState } from "../components/StatusView";
import "../components/StatusView.css";
import "./AuditLogPage.css";

const PAGE_SIZE = 20;

export default function EmergencyAlertLogPage() {
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [status, setStatus] = useState("loading");
  const [errorMessage, setErrorMessage] = useState("");

  const loadLogs = useCallback(async () => {
    setStatus("loading");
    try {
      const data = await getEmergencyAlertLogs({ page, pageSize: PAGE_SIZE });
      setResult(data);
      setStatus("success");
    } catch (err) {
      setErrorMessage(err.response?.data?.message || "Failed to load emergency alert logs.");
      setStatus("error");
    }
  }, [page]);

  useEffect(() => {
    loadLogs();
  }, [loadLogs]);

  return (
    <div>
      <h1>Emergency Alert Log</h1>
      <p className="page-subtitle">Past emergency alerts, who triggered them, and whether contacts were notified.</p>

      {status === "loading" && <LoadingState label="Loading emergency alerts..." />}
      {status === "error" && <ErrorState message={errorMessage} onRetry={loadLogs} />}
      {status === "success" && result.items.length === 0 && <EmptyState message="No emergency alerts have been triggered yet." />}

      {status === "success" && result.items.length > 0 && (
        <>
          <table className="audit-table">
            <thead>
              <tr>
                <th>Patient</th>
                <th>Triggered By</th>
                <th>Triggered At</th>
                <th>Contacts Notified</th>
                <th>Failed</th>
              </tr>
            </thead>
            <tbody>
              {result.items.map((log) => (
                <tr key={log.id}>
                  <td>
                    <Link to={`/patients/${log.patientProfileId}`}>{log.patientFullName}</Link>
                  </td>
                  <td>{log.triggeredByName}</td>
                  <td>{new Date(log.triggeredAt).toLocaleString()}</td>
                  <td>{log.contactsNotified}</td>
                  <td>{log.contactsFailed > 0 ? <span className="action-pill action-deleted">{log.contactsFailed}</span> : "0"}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="pagination">
            <button type="button" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
              Previous
            </button>
            <span>
              Page {result.page} of {Math.max(result.totalPages, 1)} ({result.totalCount} alerts)
            </span>
            <button type="button" disabled={page >= result.totalPages} onClick={() => setPage((p) => p + 1)}>
              Next
            </button>
          </div>
        </>
      )}
    </div>
  );
}
