import { useEffect, useState } from 'react';
import DoctorPicker from '../components/DoctorPicker';
import { useDoctor } from '../components/DoctorContext';
import { api } from '../api/client';

export default function ConsultationsPage() {
  const { selectedDoctorId } = useDoctor();
  const [bookings, setBookings] = useState([]);
  const [slotId, setSlot] = useState('');
  const [notes, setNotes] = useState('');
  const [prescription, setPrescription] = useState('');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);
  useEffect(() => {
    setSlot(''); setBookings([]);
    if (selectedDoctorId) api.getBooked(selectedDoctorId).then(setBookings).catch(e => setMessage(e.message));
  }, [selectedDoctorId]);
  async function submit(e) {
    e.preventDefault(); const booking = bookings.find(b => b.slotId === slotId); if (!booking) return;
    setBusy(true); setMessage('');
    try {
      await api.completeConsultation({ slotId, patientId: booking.patientId, doctorId: booking.doctorId, notes, prescription: prescription || null });
      setBookings(bookings.filter(b => b.slotId !== slotId)); setSlot(''); setNotes(''); setPrescription(''); setMessage('Consultation saved.');
    } catch (e) { setMessage(e.message); } finally { setBusy(false); }
  }
  return <div><h1>Consultations</h1><DoctorPicker />
    <form onSubmit={submit} className="card">
      <label>Booked appointment <select required value={slotId} onChange={e => setSlot(e.target.value)}><option value="">Select appointment</option>{bookings.map(b => <option key={b.slotId} value={b.slotId}>{b.patientName} — {new Date(b.slotStart).toLocaleString()}</option>)}</select></label>
      {!bookings.length && <p>No pending consultations for this doctor.</p>}
      <label>Notes <textarea required maxLength={4000} value={notes} onChange={e => setNotes(e.target.value)} /></label>
      <label>Prescription (optional) <textarea maxLength={2000} value={prescription} onChange={e => setPrescription(e.target.value)} /></label>
      <button disabled={busy || !slotId}>{busy ? 'Saving…' : 'Complete consultation'}</button>
    </form>{message && <p role="status">{message}</p>}
  </div>;
}
