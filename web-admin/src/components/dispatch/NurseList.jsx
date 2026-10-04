import React, { useState } from 'react';
import { useDispatchStore } from '../../store/useDispatchStore';
import api from '../../api/apiClient';
import './NurseList.css';

function WaitingCase({ ticket, nurses, refresh }) {
  const [nurseId, setNurse] = useState('');
  const [lat, setLat] = useState(ticket.latitude ?? '');
  const [lng, setLng] = useState(ticket.longitude ?? '');
  const [error, setError] = useState('');
  const [flags, setFlags] = useState([]);
  const [acknowledged, setAcknowledged] = useState(false);
  const [busy, setBusy] = useState(false);
  async function assign(e) {
    e.preventDefault(); setBusy(true); setError('');
    try {
      if (lat === '' || lng === '') throw new Error('Confirm the patient destination before assignment.');
      await api.put(`/triage/${ticket.id}/destination`, { latitude: Number(lat), longitude: Number(lng) });
      await api.post('/dispatch/assign', { triageTicketId: ticket.id, nurseId, acknowledgeSafetyFlags: acknowledged });
      await refresh();
    } catch (e) { setError(e.message); setFlags(e.response?.data?.flaggedRules || []); }
    finally { setBusy(false); }
  }
  return <form onSubmit={assign} className="dispatch-card">
    <strong>{ticket.patientName}</strong><p>Risk {ticket.riskScore}/10 • Waiting for assignment</p>
    <label>Nurse <select required value={nurseId} onChange={e => {setNurse(e.target.value); setAcknowledged(false);}}>
      <option value="">Select available nurse</option>{nurses.map(n => <option key={n.id} value={n.id}>{n.name}</option>)}
    </select></label>
    <label>Confirmed latitude <input required type="number" min="-90" max="90" step="any" value={lat} onChange={e => setLat(e.target.value)} /></label>
    <label>Confirmed longitude <input required type="number" min="-180" max="180" step="any" value={lng} onChange={e => setLng(e.target.value)} /></label>
    {flags.length > 0 && <><ul>{flags.map(f => <li key={f}>{f}</li>)}</ul><label><input type="checkbox" checked={acknowledged} onChange={e => setAcknowledged(e.target.checked)} /> I reviewed these safety warnings</label></>}
    {error && <p role="alert">{error}</p>}
    <button disabled={busy || !nurseId}>{busy ? 'Assigning…' : 'Assign nurse'}</button>
    {!nurses.length && <p>No nurse is currently available. This case remains in the queue.</p>}
  </form>;
}
export const NurseList = () => {
  const { activeDispatches, availableNurses, waitingCases, isLoading, error, fetchDispatchData } = useDispatchStore();
  return <div className="nurse-list-container">
    {error && <p role="alert">Refresh failed: {error}. Previously loaded data may be stale.</p>}
    {isLoading && <p>Updating…</p>}
    <h2>Waiting for assignment</h2>
    {!waitingCases.length && <p>No approved cases waiting.</p>}
    {waitingCases.map(ticket => <WaitingCase key={ticket.id} ticket={ticket} nurses={availableNurses} refresh={fetchDispatchData} />)}
    <h2>Active dispatches</h2>
    {!activeDispatches.length && <p>No active dispatches.</p>}
    {activeDispatches.map(d => <div key={d.id} className="dispatch-card">
      <strong>{d.nurseName}</strong><p>{d.status}</p>
      {d.isEscalated && <p role="alert">Escalation: {d.escalationNotes}</p>}
      <p>{d.location ? `${d.location.lat.toFixed(4)}, ${d.location.lng.toFixed(4)}` : 'No telemetry received'}</p>
      {d.locationRecordedAt && <p>Last update: {new Date(d.locationRecordedAt).toLocaleString()}</p>}
    </div>)}
  </div>;
};
