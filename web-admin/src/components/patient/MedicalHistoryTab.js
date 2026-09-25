import { useEffect, useState } from "react";
import { addMedicalHistory, deleteMedicalHistory, updateMedicalHistory } from "../../api/patientsApi";
import { useAuth } from "../../context/AuthContext";
import { EmptyState } from "../StatusView";
import "./PatientTabs.css";

const emptyForm = { conditionName: "", notes: "", diagnosedOn: "", isChronic: false, currentMedications: "" };

export default function MedicalHistoryTab({ patientId, initialHistory, onChanged }) {
  const { hasRole } = useAuth();
  const canRecord = hasRole("Doctor");
  const canManage = hasRole("Doctor", "Admin");

  const [history, setHistory] = useState(initialHistory);
  const [addForm, setAddForm] = useState(emptyForm);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");
  const [editingId, setEditingId] = useState(null);
  const [editForm, setEditForm] = useState(emptyForm);

  useEffect(() => setHistory(initialHistory), [initialHistory]);

  const handleAdd = async (event) => {
    event.preventDefault();
    setError("");
    setIsSaving(true);
    try {
      const updated = await addMedicalHistory(patientId, addForm);
      setHistory(updated);
      setAddForm(emptyForm);
      onChanged();
    } catch (err) {
      setError(err.response?.data?.message || "Failed to add medical history entry.");
    } finally {
      setIsSaving(false);
    }
  };

  const startEdit = (entry) => {
    setEditingId(entry.id);
    setEditForm({
      conditionName: entry.conditionName,
      notes: entry.notes || "",
      diagnosedOn: entry.diagnosedOn,
      isChronic: entry.isChronic,
      currentMedications: entry.currentMedications || "",
      isResolved: entry.isResolved,
      resolvedOn: entry.resolvedOn || "",
    });
  };

  const handleUpdate = async (event) => {
    event.preventDefault();
    setError("");
    try {
      const updatedEntry = await updateMedicalHistory(patientId, editingId, editForm);
      setHistory((prev) => prev.map((h) => (h.id === editingId ? updatedEntry : h)));
      setEditingId(null);
      onChanged();
    } catch (err) {
      setError(err.response?.data?.message || "Failed to update entry.");
    }
  };

  const handleResolveToggle = async (entry) => {
    setError("");
    try {
      const updatedEntry = await updateMedicalHistory(patientId, entry.id, {
        conditionName: entry.conditionName,
        notes: entry.notes,
        diagnosedOn: entry.diagnosedOn,
        isChronic: entry.isChronic,
        currentMedications: entry.currentMedications,
        isResolved: !entry.isResolved,
        resolvedOn: !entry.isResolved ? new Date().toISOString().slice(0, 10) : null,
      });
      setHistory((prev) => prev.map((h) => (h.id === entry.id ? updatedEntry : h)));
      onChanged();
    } catch (err) {
      setError(err.response?.data?.message || "Failed to update entry.");
    }
  };

  const handleDelete = async (historyId) => {
    if (!window.confirm("Mark this entry as entered in error? It will be hidden but kept for the legal record.")) return;
    setError("");
    try {
      await deleteMedicalHistory(patientId, historyId);
      setHistory((prev) => prev.filter((h) => h.id !== historyId));
      onChanged();
    } catch (err) {
      setError(err.response?.data?.message || "Failed to remove entry.");
    }
  };

  return (
    <div className="detail-card">
      <h2>Clinical History Timeline</h2>
      {error && (
        <div className="alert alert-error" role="alert">
          {error}
        </div>
      )}

      {history.length === 0 ? (
        <EmptyState message="No medical history recorded yet." />
      ) : (
        <ul className="timeline">
          {history.map((entry) =>
            editingId === entry.id ? (
              <li key={entry.id}>
                <form className="inline-edit-form" onSubmit={handleUpdate}>
                  <input
                    value={editForm.conditionName}
                    onChange={(e) => setEditForm((f) => ({ ...f, conditionName: e.target.value }))}
                  />
                  <input
                    type="date"
                    value={editForm.diagnosedOn}
                    onChange={(e) => setEditForm((f) => ({ ...f, diagnosedOn: e.target.value }))}
                  />
                  <input
                    placeholder="Current medications"
                    value={editForm.currentMedications}
                    onChange={(e) => setEditForm((f) => ({ ...f, currentMedications: e.target.value }))}
                  />
                  <textarea
                    placeholder="Notes"
                    value={editForm.notes}
                    onChange={(e) => setEditForm((f) => ({ ...f, notes: e.target.value }))}
                  />
                  <label className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={editForm.isChronic}
                      onChange={(e) => setEditForm((f) => ({ ...f, isChronic: e.target.checked }))}
                    />
                    Chronic
                  </label>
                  <div className="form-actions">
                    <button type="submit">Save</button>
                    <button type="button" className="btn-secondary" onClick={() => setEditingId(null)}>
                      Cancel
                    </button>
                  </div>
                </form>
              </li>
            ) : (
              <li key={entry.id}>
                <div className="timeline-header">
                  <strong>{entry.conditionName}</strong>
                  <span>{entry.diagnosedOn}</span>
                </div>
                {entry.notes && <p>{entry.notes}</p>}
                {entry.currentMedications && <p className="medication-line">Medications: {entry.currentMedications}</p>}
                <div className="badge-row">
                  {entry.isChronic && <span className="badge badge-warning">Chronic</span>}
                  {entry.isResolved ? (
                    <span className="badge badge-success">Resolved {entry.resolvedOn ? `on ${entry.resolvedOn}` : ""}</span>
                  ) : (
                    <span className="badge">Active</span>
                  )}
                </div>
                {canManage && (
                  <div className="row-actions">
                    <button type="button" onClick={() => startEdit(entry)}>
                      Edit
                    </button>
                    <button type="button" onClick={() => handleResolveToggle(entry)}>
                      {entry.isResolved ? "Reopen" : "Mark resolved"}
                    </button>
                    <button type="button" className="btn-danger-link" onClick={() => handleDelete(entry.id)}>
                      Entered in error
                    </button>
                  </div>
                )}
              </li>
            )
          )}
        </ul>
      )}

      {canRecord && (
        <form className="inline-add-form" onSubmit={handleAdd}>
          <h3>Add Medical History Entry</h3>
          <div className="history-form-row">
            <input
              type="text"
              placeholder="Condition name"
              required
              value={addForm.conditionName}
              onChange={(e) => setAddForm((f) => ({ ...f, conditionName: e.target.value }))}
            />
            <input
              type="date"
              required
              max={new Date().toISOString().slice(0, 10)}
              value={addForm.diagnosedOn}
              onChange={(e) => setAddForm((f) => ({ ...f, diagnosedOn: e.target.value }))}
            />
          </div>
          <input
            type="text"
            placeholder="Current medications (optional)"
            value={addForm.currentMedications}
            onChange={(e) => setAddForm((f) => ({ ...f, currentMedications: e.target.value }))}
          />
          <textarea
            placeholder="Clinical notes"
            value={addForm.notes}
            onChange={(e) => setAddForm((f) => ({ ...f, notes: e.target.value }))}
          />
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={addForm.isChronic}
              onChange={(e) => setAddForm((f) => ({ ...f, isChronic: e.target.checked }))}
            />
            Chronic condition
          </label>
          <button type="submit" disabled={isSaving}>
            {isSaving ? "Saving..." : "Add entry"}
          </button>
        </form>
      )}
    </div>
  );
}
