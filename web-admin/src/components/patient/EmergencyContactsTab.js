import { useEffect, useState } from "react";
import { addEmergencyContact, deleteEmergencyContact, updateEmergencyContact } from "../../api/patientsApi";
import { EmptyState } from "../StatusView";
import "./PatientTabs.css";

const emptyForm = { fullName: "", relationshipToPatient: "", phoneNumber: "", isPrimary: false };

export default function EmergencyContactsTab({ patientId, initialContacts, isAdmin, onChanged }) {
  const [contacts, setContacts] = useState(initialContacts);
  const [addForm, setAddForm] = useState(emptyForm);
  const [error, setError] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [editForm, setEditForm] = useState(emptyForm);

  useEffect(() => setContacts(initialContacts), [initialContacts]);

  const handleAdd = async (event) => {
    event.preventDefault();
    setError("");
    setIsSaving(true);
    try {
      const updated = await addEmergencyContact(patientId, addForm);
      setContacts(updated);
      setAddForm(emptyForm);
      onChanged();
    } catch (err) {
      setError(err.response?.data?.message || "Failed to add emergency contact.");
    } finally {
      setIsSaving(false);
    }
  };

  const startEdit = (contact) => {
    setEditingId(contact.id);
    setEditForm({ ...contact });
  };

  const handleUpdate = async (event) => {
    event.preventDefault();
    setError("");
    try {
      const updatedContact = await updateEmergencyContact(patientId, editingId, editForm);
      setContacts((prev) =>
        prev.map((c) => (c.id === editingId ? updatedContact : updatedContact.isPrimary ? { ...c, isPrimary: false } : c))
      );
      setEditingId(null);
      onChanged();
    } catch (err) {
      setError(err.response?.data?.message || "Failed to update contact.");
    }
  };

  const handleDelete = async (contactId) => {
    if (!window.confirm("Remove this emergency contact?")) return;
    setError("");
    try {
      await deleteEmergencyContact(patientId, contactId);
      setContacts((prev) => prev.filter((c) => c.id !== contactId));
      onChanged();
    } catch (err) {
      setError(err.response?.data?.message || "A patient must keep at least one emergency contact.");
    }
  };

  return (
    <div className="detail-card">
      <h2>Emergency Contacts</h2>
      {error && (
        <div className="alert alert-error" role="alert">
          {error}
        </div>
      )}

      {contacts.length === 0 ? (
        <EmptyState message="No emergency contacts registered." />
      ) : (
        <ul className="contact-list">
          {contacts.map((contact) =>
            editingId === contact.id ? (
              <li key={contact.id}>
                <form className="inline-edit-form" onSubmit={handleUpdate}>
                  <input
                    value={editForm.fullName}
                    onChange={(e) => setEditForm((f) => ({ ...f, fullName: e.target.value }))}
                  />
                  <input
                    value={editForm.relationshipToPatient}
                    onChange={(e) => setEditForm((f) => ({ ...f, relationshipToPatient: e.target.value }))}
                  />
                  <input
                    value={editForm.phoneNumber}
                    onChange={(e) => setEditForm((f) => ({ ...f, phoneNumber: e.target.value }))}
                  />
                  <label className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={editForm.isPrimary}
                      onChange={(e) => setEditForm((f) => ({ ...f, isPrimary: e.target.checked }))}
                    />
                    Primary contact
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
              <li key={contact.id}>
                <div>
                  <strong>{contact.fullName}</strong> ({contact.relationshipToPatient}) &middot; {contact.phoneNumber}
                  {contact.isPrimary && <span className="badge">Primary</span>}
                </div>
                {isAdmin && (
                  <div className="row-actions">
                    <button type="button" onClick={() => startEdit(contact)}>
                      Edit
                    </button>
                    <button type="button" className="btn-danger-link" onClick={() => handleDelete(contact.id)}>
                      Remove
                    </button>
                  </div>
                )}
              </li>
            )
          )}
        </ul>
      )}

      {isAdmin && (
        <form className="inline-add-form" onSubmit={handleAdd}>
          <h3>Add Emergency Contact</h3>
          <div className="history-form-row">
            <input
              placeholder="Full name"
              required
              value={addForm.fullName}
              onChange={(e) => setAddForm((f) => ({ ...f, fullName: e.target.value }))}
            />
            <input
              placeholder="Relationship"
              required
              value={addForm.relationshipToPatient}
              onChange={(e) => setAddForm((f) => ({ ...f, relationshipToPatient: e.target.value }))}
            />
          </div>
          <input
            placeholder="Phone number"
            required
            value={addForm.phoneNumber}
            onChange={(e) => setAddForm((f) => ({ ...f, phoneNumber: e.target.value }))}
          />
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={addForm.isPrimary}
              onChange={(e) => setAddForm((f) => ({ ...f, isPrimary: e.target.checked }))}
            />
            Set as primary contact
          </label>
          <button type="submit" disabled={isSaving}>
            {isSaving ? "Saving..." : "Add contact"}
          </button>
        </form>
      )}
    </div>
  );
}
