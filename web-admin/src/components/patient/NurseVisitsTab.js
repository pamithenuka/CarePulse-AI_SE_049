import { useEffect, useState } from 'react';
import api from '../../api/apiClient';
import './PatientTabs.css';
import './NurseVisitsTab.css';

export default function NurseVisitsTab({ patientId }) {
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  useEffect(() => { setFromDate(''); setToDate(''); }, [patientId]);
  const [visits, setVisits] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [revision, setRevision] = useState(0);
  useEffect(() => {
    let current = true;
    setLoading(true);
    setError('');
    api.get(`/dispatch/patients/${patientId}/visits`).then(({ data }) => {
      if (current) setVisits(data);
    }).catch(() => {
      if (current) setError('Could not load nurse visits. Please try again.');
    }).finally(() => { if (current) setLoading(false); });
    return () => { current = false; };
  }, [patientId, revision]);
  const invalidRange = Boolean(fromDate && toDate && fromDate > toDate);
  const filteredVisits = visits.filter(visit => {
    // Use the same local calendar date as the displayed assignment timestamp.
    const date = new Date(visit.assignedAt);
    const day = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
    return (!fromDate || day >= fromDate) && (!toDate || day <= toDate);
  }).sort((a, b) => new Date(b.assignedAt) - new Date(a.assignedAt));
  return <section className="detail-card nurse-visits">
    <header className="nurse-visits-toolbar">
      <h2>Nurse visits &amp; vitals</h2>
      <button className="nurse-visits-refresh" type="button" disabled={loading} onClick={() => setRevision(r => r + 1)}>
        <span aria-hidden="true">↻</span> Refresh visits
      </button>
    </header>
    <fieldset className="nurse-visits-filters">
      <legend>Visit date</legend>
      <div className="nurse-visits-filter-controls">
        <label>From date<input type="date" value={fromDate} onChange={e => setFromDate(e.target.value)} /></label>
        <label>To date<input type="date" value={toDate} aria-invalid={invalidRange} aria-describedby={invalidRange ? 'visit-date-error' : undefined} onChange={e => setToDate(e.target.value)} /></label>
        <button className="nurse-visits-refresh" type="button" disabled={!fromDate && !toDate} onClick={() => { setFromDate(''); setToDate(''); }}>Clear filters</button>
      </div>
      {invalidRange && <p id="visit-date-error" role="alert">To date must be on or after From date.</p>}
    </fieldset>
    {!loading && !error && !invalidRange && <p className="nurse-visit-saved" role="status">Showing {filteredVisits.length} of {visits.length} visits</p>}
    {loading ? <p className="nurse-visits-message" role="status">Loading nurse visits…</p> : error ? <p className="nurse-visits-message nurse-visits-error" role="alert">{error}</p> : invalidRange ? null : !visits.length ? <p className="nurse-visits-message">No nurse visits recorded.</p> : !filteredVisits.length ? <p className="nurse-visits-message">No visits match these dates. Change or clear the filters.</p> : <div className="nurse-visits-list">{filteredVisits.map(visit =>
      <article key={visit.id} className="nurse-visit" aria-label={`Visit by ${visit.nurseName}`}>
        <header className="nurse-visit-header">
          <div className="nurse-visit-person">
            <span className="nurse-visit-avatar" aria-hidden="true">{visit.nurseName?.trim().charAt(0) || 'N'}</span>
            <h3>{visit.nurseName}</h3>
          </div>
          <span className={`nurse-visit-status nurse-visit-status--${visit.status}`}>
            {({ Assigned: 'Assigned', EnRoute: 'On the way', ArrivedOnSite: 'On site', Completed: 'Completed' })[visit.status] || visit.status}
          </span>
        </header>
        <div className="nurse-visit-dates">
          <p><span>Assigned</span><time dateTime={visit.assignedAt}>{new Date(visit.assignedAt).toLocaleString()}</time></p>
          {visit.completedAt && <p><span>Completed</span><time dateTime={visit.completedAt}>{new Date(visit.completedAt).toLocaleString()}</time></p>}
        </div>
        {!visit.vitals.length ? <p className="nurse-visits-message">No measurements saved yet.</p> : visit.vitals.map(v => <div key={v.id} className="nurse-visit-record">
          <p className="nurse-visit-saved">Measurements last saved · <time dateTime={v.recordedAt}>{new Date(v.recordedAt).toLocaleString()}</time></p>
          <dl className="nurse-vitals-grid">
            <div><dt>Heart rate</dt><dd>{v.heartRate} <small>bpm</small></dd></div>
            <div><dt>Blood pressure</dt><dd>{v.bloodPressure} <small>mmHg</small></dd></div>
            <div><dt>Temperature</dt><dd>{v.bodyTempC} <small>°C</small></dd></div>
            <div><dt>Oxygen saturation</dt><dd>{v.oxygenSaturation}<small>%</small></dd></div>
          </dl>
          <section className="nurse-visit-notes">
            <h4>Clinical notes</h4>
            <p>{v.clinicalNotes}</p>
          </section>
        </div>)}
      </article>)}</div>}
  </section>;
}
