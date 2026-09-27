import { Fragment, useCallback, useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { getAuditLog } from "../api/patientsApi";
import { EmptyState, ErrorState, LoadingState } from "../components/StatusView";
import "../components/StatusView.css";
import "./AuditLogPage.css";

const PAGE_SIZE = 20;

function ChangeDiff({ changesJson }) {
  let changes = [];
  try {
    changes = JSON.parse(changesJson);
  } catch {
    return null;
  }
  if (changes.length === 0) return null;

  return (
    <ul className="change-diff">
      {changes.map((change) => (
        <li key={change.field}>
          <strong>{change.field}</strong>: <span className="old-value">{String(change.oldValue ?? "—")}</span>{" "}
          &rarr; <span className="new-value">{String(change.newValue ?? "—")}</span>
        </li>
      ))}
    </ul>
  );
}

export default function AuditLogPage() {
  const [searchParams] = useSearchParams();
  const patientId = searchParams.get("patientId") || "";

  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [status, setStatus] = useState("loading");
  const [errorMessage, setErrorMessage] = useState("");
  const [expandedId, setExpandedId] = useState(null);

  const loadLogs = useCallback(async () => {
    setStatus("loading");
    try {
      const data = await getAuditLog({
        patientId: patientId || undefined,
        from: from || undefined,
        to: to || undefined,
        page,
        pageSize: PAGE_SIZE,
      });
      setResult(data);
      setStatus("success");
    } catch (err) {
      setErrorMessage(err.response?.data?.message || "Failed to load the audit log.");
      setStatus("error");
    }
  }, [patientId, from, to, page]);

  useEffect(() => {
    loadLogs();
  }, [loadLogs]);

  return (
    <div>
      <h1>Clinical History Auditor</h1>
      <p className="page-subtitle">
        Every change to patient records — who changed it, what changed, and when.
        {patientId && (
          <>
            {" "}
            Filtered to one patient. <Link to="/audit-log">Clear filter</Link>
          </>
        )}
      </p>

      <div className="filters">
        <label>
          From
          <input
            type="date"
            value={from}
            onChange={(e) => {
              setPage(1);
              setFrom(e.target.value);
            }}
          />
        </label>
        <label>
          To
          <input
            type="date"
            value={to}
            onChange={(e) => {
              setPage(1);
              setTo(e.target.value);
            }}
          />
        </label>
      </div>

      {status === "loading" && <LoadingState label="Loading audit log..." />}
      {status === "error" && <ErrorState message={errorMessage} onRetry={loadLogs} />}
      {status === "success" && result.items.length === 0 && <EmptyState message="No audit entries match these filters." />}

      {status === "success" && result.items.length > 0 && (
        <>
          <table className="audit-table">
            <thead>
              <tr>
                <th>Patient</th>
                <th>Entity</th>
                <th>Action</th>
                <th>Changed By</th>
                <th>Changed At</th>
              </tr>
            </thead>
            <tbody>
              {result.items.map((log) => (
                <Fragment key={log.id}>
                  <tr
                    className="audit-row"
                    onClick={() => setExpandedId(expandedId === log.id ? null : log.id)}
                  >
                    <td>
                      <Link to={`/patients/${log.patientProfileId}`} onClick={(e) => e.stopPropagation()}>
                        {log.patientFullName}
                      </Link>
                    </td>
                    <td>{log.entityName}</td>
                    <td>
                      <span className={`action-pill action-${log.action.toLowerCase()}`}>{log.action}</span>
                    </td>
                    <td>{log.changedByName}</td>
                    <td>{new Date(log.changedAt).toLocaleString()}</td>
                  </tr>
                  {expandedId === log.id && (
                    <tr className="audit-detail-row">
                      <td colSpan={5}>
                        <ChangeDiff changesJson={log.changesJson} />
                        {log.changesJson === "[]" && <span className="hint-text">No field-level changes recorded.</span>}
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))}
            </tbody>
          </table>

          <div className="pagination">
            <button type="button" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
              Previous
            </button>
            <span>
              Page {result.page} of {Math.max(result.totalPages, 1)} ({result.totalCount} entries)
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
