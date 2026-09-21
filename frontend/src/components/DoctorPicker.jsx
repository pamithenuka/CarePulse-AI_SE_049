import { useDoctor } from './DoctorContext.jsx'

export default function DoctorPicker() {
  const { doctors, selectedDoctorId, setSelectedDoctorId, loading, loadError } = useDoctor()

  if (loading) return <div className="doctor-picker">Loading doctors…</div>

  if (loadError) {
    return (
      <div className="doctor-picker" style={{ borderColor: '#e3b6ac' }}>
        Couldn't reach the API ({loadError}). Is <code>dotnet run</code> still running?
      </div>
    )
  }

  if (doctors.length === 0) {
    return (
      <div className="doctor-picker">
        No doctors in the database yet. Add one via psql or a future admin form.
      </div>
    )
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
  )
}
