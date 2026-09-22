import { useDoctor } from './DoctorContext';

export default function DoctorPicker() {
  const { doctors, selectedDoctorId, setSelectedDoctorId, loading, loadError } = useDoctor();

  if (loading) return <div className="doctor-picker">Loading doctors…</div>;

  if (loadError) {
    return (
      <div className="doctor-picker" style={{ borderColor: '#e3b6ac' }}>
        Couldn't reach the API ({loadError}). Is <code>dotnet run</code> still running in backend-api?
      </div>
    );
  }

  if (doctors.length === 0) {
    return <div className="doctor-picker">No doctors yet - add one on the Doctors page.</div>;
  }

  return (
    <div className="doctor-picker">
      <label htmlFor="doctor-select">Viewing as</label>
      <select
        id="doctor-select"
        value={selectedDoctorId}
        onChange={(e) => setSelectedDoctorId(e.target.value)}
      >
        {doctors.map((d) => (
          <option key={d.id} value={d.id}>
            {d.fullName} — {d.specialty}
          </option>
        ))}
      </select>
    </div>
  );
}
