import "./PatientTabs.css";

// Read-only per the component ownership split: consultation records belong to
// Student 3 (Doctor Rostering & Consultations). This tab renders once that
// module exposes GET /api/v1/consultations?patientId={id} (or similar) —
// wiring it in is a one-line fetch swap, not a redesign.
export default function ConsultationHistoryTab() {
  return (
    <div className="detail-card">
      <h2>Consultation History</h2>
      <div className="status-view">Consultation records will appear here once the scheduling module is connected.</div>
    </div>
  );
}
