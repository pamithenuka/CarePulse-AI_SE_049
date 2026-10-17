import { useEffect, useState } from 'react';
import DoctorPicker from '../components/DoctorPicker';
import PatientPicker from '../components/PatientPicker';
import { useDoctor } from '../components/DoctorContext';
import { api } from '../api/client';

function formatTime(iso) {
  return new Date(iso).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function formatDate(iso) {
  return new Date(iso).toLocaleDateString([], { weekday: 'short', month: 'short', day: 'numeric' });
}

export default function SlotsPage() {
  const { selectedDoctor, selectedDoctorId } = useDoctor();
  const [date, setDate] = useState('');
  const [patient, setPatient] = useState(null);
  const [slots, setSlots] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [bookingId, setBookingId] = useState(null);
  const [bookMessage, setBookMessage] = useState(null);

  async function loadSlots() {
    setLoading(true);
    setError(null);
    try {
      const results = await api.getSlots({
        date: date || undefined,
        doctorId: selectedDoctorId || undefined
      });
      setSlots(results);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (selectedDoctorId) loadSlots();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedDoctorId, date]);

  async function handleBook(slotId) {
    setBookingId(slotId);
    setBookMessage(null);
    try {
      await api.bookAppointment({ slotId, patientId: patient.id });
      setBookMessage({ type: 'success', text: `Appointment booked for ${patient.fullName}.` });
      loadSlots();
    } catch (err) {
      setBookMessage({ type: 'error', text: err.message });
    } finally {
      setBookingId(null);
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1>Appointment slots</h1>
        <p>Slots generated from the weekly roster. Choose a patient, then book one of the open slots for them.</p>
      </div>

      <DoctorPicker />
      <PatientPicker value={patient} onChange={setPatient} />

      <div className="card">
        <h2>Filter</h2>
        <div className="form-row">
          <div className="field">
            <label htmlFor="filterDate">Date</label>
            <input id="filterDate" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
          </div>
          <button className="btn btn-secondary" onClick={() => setDate('')}>
            Clear date
          </button>
        </div>

        {!patient && <p className="hint-text">Choose a patient above to enable booking.</p>}
        {bookMessage && <div className={`status-msg ${bookMessage.type}`}>{bookMessage.text}</div>}

        {loading && <p className="empty-state">Loading slots…</p>}
        {error && <div className="status-msg error">{error}</div>}

        {!loading && !error && slots.length === 0 && (
          <p className="empty-state">
            No slots found for {selectedDoctor?.fullName ?? 'this doctor'}
            {date ? ` on ${date}` : ''}. Generate some from the Weekly roster page.
          </p>
        )}

        {!loading && slots.length > 0 && (
          <div className="slot-list">
            {slots.map((slot) => (
              <div key={slot.slotId} className="slot-row">
                <div>
                  <span className="slot-time">
                    {formatDate(slot.slotStart)}, {formatTime(slot.slotStart)}–{formatTime(slot.slotEnd)}
                  </span>
                  <span style={{ color: 'var(--color-muted)', marginLeft: 10 }}>
                    {slot.doctorName}
                  </span>
                </div>
                <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
                  <span className="slot-badge open">Open</span>
                  <button
                    className="btn btn-secondary"
                    disabled={bookingId === slot.slotId || !patient}
                    onClick={() => handleBook(slot.slotId)}
                  >
                    {bookingId === slot.slotId ? 'Booking…' : 'Book appointment'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
