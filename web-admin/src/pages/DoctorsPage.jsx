import { useState } from 'react';
import { useDoctor } from '../components/DoctorContext';
import { api } from '../api/client';

const emptyForm = { fullName: '', specialty: '', phoneNumber: '', email: '' };

export default function DoctorsPage() {
  const { doctors, loading, loadError, refreshDoctors } = useDoctor();

  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState(null);

  const [editingId, setEditingId] = useState(null);
  const [editForm, setEditForm] = useState(emptyForm);
  const [editSaving, setEditSaving] = useState(false);

  async function handleAdd(e) {
    e.preventDefault();
    setSaving(true);
    setMessage(null);
    try {
      await api.createDoctor(form);
      setMessage({ type: 'success', text: `${form.fullName} added.` });
      setForm(emptyForm);
      refreshDoctors();
    } catch (err) {
      setMessage({ type: 'error', text: err.message });
    } finally {
      setSaving(false);
    }
  }

  function startEdit(doctor) {
    setEditingId(doctor.id);
    setEditForm({
      fullName: doctor.fullName,
      specialty: doctor.specialty,
      phoneNumber: doctor.phoneNumber || '',
      email: doctor.email || ''
    });
  }

  async function handleSaveEdit(doctorId) {
    setEditSaving(true);
    try {
      await api.updateDoctor(doctorId, { ...editForm, isActive: true });
      setEditingId(null);
      refreshDoctors();
    } catch (err) {
      setMessage({ type: 'error', text: err.message });
    } finally {
      setEditSaving(false);
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1>Doctor management</h1>
        <p>Register new doctors and keep their contact details up to date.</p>
      </div>

      <div className="card">
        <h2>Add a doctor</h2>
        <form onSubmit={handleAdd}>
          <div className="form-row">
            <div className="field">
              <label htmlFor="fullName">Full name</label>
              <input
                id="fullName"
                required
                value={form.fullName}
                onChange={(e) => setForm({ ...form, fullName: e.target.value })}
              />
            </div>
            <div className="field">
              <label htmlFor="specialty">Specialty</label>
              <input
                id="specialty"
                required
                value={form.specialty}
                onChange={(e) => setForm({ ...form, specialty: e.target.value })}
              />
            </div>
            <div className="field">
              <label htmlFor="phone">Phone</label>
              <input
                id="phone"
                value={form.phoneNumber}
                onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })}
              />
            </div>
            <div className="field">
              <label htmlFor="email">Email (optional)</label>
              <input
                id="email"
                type="email"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
              />
            </div>
            <button className="btn btn-primary" disabled={saving}>
              {saving ? 'Adding…' : 'Add doctor'}
            </button>
          </div>
        </form>
        {message && <div className={`status-msg ${message.type}`}>{message.text}</div>}
      </div>

      <div className="card">
        <h2>All doctors</h2>
        {loading && <p className="empty-state">Loading…</p>}
        {loadError && <div className="status-msg error">{loadError}</div>}
        {!loading && doctors.length === 0 && (
          <p className="empty-state">No doctors yet - add one above.</p>
        )}
        {!loading && doctors.length > 0 && (
          <div className="slot-list">
            {doctors.map((doctor) => (
              <div key={doctor.id} className="slot-row" style={{ alignItems: 'flex-start' }}>
                {editingId === doctor.id ? (
                  <div style={{ flex: 1, display: 'flex', flexWrap: 'wrap', gap: 10, alignItems: 'center' }}>
                    <input
                      value={editForm.fullName}
                      onChange={(e) => setEditForm({ ...editForm, fullName: e.target.value })}
                      style={{ padding: 6, borderRadius: 6, border: '1px solid var(--color-border)' }}
                    />
                    <input
                      value={editForm.specialty}
                      onChange={(e) => setEditForm({ ...editForm, specialty: e.target.value })}
                      style={{ padding: 6, borderRadius: 6, border: '1px solid var(--color-border)' }}
                    />
                    <input
                      value={editForm.phoneNumber}
                      onChange={(e) => setEditForm({ ...editForm, phoneNumber: e.target.value })}
                      style={{ padding: 6, borderRadius: 6, border: '1px solid var(--color-border)' }}
                    />
                    <button
                      className="btn btn-primary"
                      disabled={editSaving}
                      onClick={() => handleSaveEdit(doctor.id)}
                    >
                      {editSaving ? 'Saving…' : 'Save'}
                    </button>
                    <button className="btn btn-secondary" onClick={() => setEditingId(null)}>
                      Cancel
                    </button>
                  </div>
                ) : (
                  <>
                    <div>
                      <span className="slot-time">{doctor.fullName}</span>
                      <span style={{ color: 'var(--color-muted)', marginLeft: 10 }}>
                        {doctor.specialty}
                        {doctor.phoneNumber ? ` · ${doctor.phoneNumber}` : ''}
                      </span>
                    </div>
                    <button className="btn btn-secondary" onClick={() => startEdit(doctor)}>
                      Edit
                    </button>
                  </>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
