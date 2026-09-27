import { useState } from 'react';
import DoctorPicker from '../components/DoctorPicker';
import { useDoctor } from '../components/DoctorContext';
import { api } from '../api/client';

export default function ConsultationsPage() {
  const { selectedDoctorId } = useDoctor();
  const [form, setForm] = useState({
    slotId: '',
    patientId: '11111111-1111-1111-1111-111111111111',
    notes: '',
    prescription: ''
  });
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState(null);
  const [lastRecord, setLastRecord] = useState(null);

  async function handleSubmit(e) {
    e.preventDefault();
    if (!selectedDoctorId) return;

    setSaving(true);
    setMessage(null);
    try {
      const record = await api.completeConsultation({
        slotId: form.slotId,
        doctorId: selectedDoctorId,
        patientId: form.patientId,
        notes: form.notes,
        prescription: form.prescription || null
      });
      setLastRecord(record);
      setMessage({ type: 'success', text: 'Consultation summary saved.' });
      setForm({ ...form, slotId: '', notes: '', prescription: '' });
    } catch (err) {
      setMessage({ type: 'error', text: err.message });
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1>Consultations</h1>
        <p>Log the outcome of a booked appointment - notes and, where relevant, a prescription.</p>
      </div>

      <DoctorPicker />

      <div className="card">
        <h2>New consultation summary</h2>
        <p className="card-desc">
          Find the Slot ID on the Appointment slots page once Student 1's patient module is wired in.
        </p>
        <form onSubmit={handleSubmit}>
          <div className="form-row">
            <div className="field">
              <label htmlFor="slotId">Slot ID</label>
              <input
                id="slotId"
                type="text"
                placeholder="e.g. 4626e3e1-450f-..."
                value={form.slotId}
                onChange={(e) => setForm({ ...form, slotId: e.target.value })}
                style={{ minWidth: 280 }}
                required
              />
            </div>
            <div className="field">
              <label htmlFor="patientId">Patient ID</label>
              <input
                id="patientId"
                type="text"
                value={form.patientId}
                onChange={(e) => setForm({ ...form, patientId: e.target.value })}
                style={{ minWidth: 280 }}
                required
              />
            </div>
          </div>
          <div className="form-row">
            <div className="field">
              <label htmlFor="notes">Consultation notes</label>
              <textarea
                id="notes"
                rows={4}
                value={form.notes}
                onChange={(e) => setForm({ ...form, notes: e.target.value })}
                required
              />
            </div>
          </div>
          <div className="form-row">
            <div className="field">
              <label htmlFor="prescription">Prescription (optional)</label>
              <textarea
                id="prescription"
                rows={2}
                value={form.prescription}
                onChange={(e) => setForm({ ...form, prescription: e.target.value })}
              />
            </div>
          </div>
          <button className="btn btn-primary" disabled={saving || !selectedDoctorId}>
            {saving ? 'Saving…' : 'Save consultation summary'}
          </button>
        </form>
        {message && <div className={`status-msg ${message.type}`}>{message.text}</div>}
        {lastRecord && (
          <p className="card-desc" style={{ marginTop: 12 }}>
            Saved record ID: <code>{lastRecord.id}</code> — fetchable via GET /api/v1/consultations/{'{id}'}
          </p>
        )}
      </div>
    </div>
  );
}
