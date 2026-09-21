import { useState, useEffect } from 'react'
import DoctorPicker from '../components/DoctorPicker.jsx'
import { useDoctor } from '../components/DoctorContext.jsx'
import { api } from '../api/client.js'

const DAYS = [
  { value: 0, label: 'Sun' },
  { value: 1, label: 'Mon' },
  { value: 2, label: 'Tue' },
  { value: 3, label: 'Wed' },
  { value: 4, label: 'Thu' },
  { value: 5, label: 'Fri' },
  { value: 6, label: 'Sat' }
]

export default function RosterPage() {
  const { selectedDoctorId } = useDoctor()

  // Populated from the API whenever the selected doctor changes, so the
  // grid reflects what's actually saved - not just what happened this session.
  const [rosterByDay, setRosterByDay] = useState({})
  const [rosterLoading, setRosterLoading] = useState(false)

  useEffect(() => {
    if (!selectedDoctorId) return
    setRosterLoading(true)
    api
      .getRoster(selectedDoctorId)
      .then((entries) => {
        const byDay = {}
        for (const entry of entries) {
          byDay[entry.dayOfWeek] = {
            startTime: entry.startTime,
            endTime: entry.endTime,
            slotDurationMinutes: entry.slotDurationMinutes
          }
        }
        setRosterByDay(byDay)
      })
      .catch(() => setRosterByDay({}))
      .finally(() => setRosterLoading(false))
  }, [selectedDoctorId])

  const [form, setForm] = useState({
    dayOfWeek: 1,
    startTime: '09:00',
    endTime: '13:00',
    slotDurationMinutes: 30
  })
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState(null)

  const [range, setRange] = useState({ startDate: '', endDate: '' })
  const [generating, setGenerating] = useState(false)
  const [generateMessage, setGenerateMessage] = useState(null)

  async function handleSaveRoster(e) {
    e.preventDefault()
    if (!selectedDoctorId) return

    setSaving(true)
    setMessage(null)
    try {
      await api.updateRoster({
        doctorId: selectedDoctorId,
        dayOfWeek: Number(form.dayOfWeek),
        startTime: `${form.startTime}:00`,
        endTime: `${form.endTime}:00`,
        slotDurationMinutes: Number(form.slotDurationMinutes)
      })
      setRosterByDay((prev) => ({
        ...prev,
        [form.dayOfWeek]: {
          startTime: form.startTime,
          endTime: form.endTime,
          slotDurationMinutes: form.slotDurationMinutes
        }
      }))
      setMessage({ type: 'success', text: 'Availability saved for this day.' })
    } catch (err) {
      setMessage({ type: 'error', text: err.message })
    } finally {
      setSaving(false)
    }
  }

  async function handleGenerateSlots(e) {
    e.preventDefault()
    if (!selectedDoctorId || !range.startDate || !range.endDate) return

    setGenerating(true)
    setGenerateMessage(null)
    try {
      const result = await api.generateSlots(selectedDoctorId, range)
      setGenerateMessage({
        type: 'success',
        text: `${result.slotsCreated} new appointment slot(s) created. View them in Appointment slots.`
      })
    } catch (err) {
      setGenerateMessage({ type: 'error', text: err.message })
    } finally {
      setGenerating(false)
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1>Weekly roster</h1>
        <p>Set the hours each doctor is normally available, then turn a date range into real bookable slots.</p>
      </div>

      <DoctorPicker />

      <div className="card">
        <h2>This week at a glance</h2>
        <p className="card-desc">Days you've set availability for this session are highlighted.</p>
        <div className="roster-grid">
          {DAYS.map((day) => {
            const entry = rosterByDay[day.value]
            return (
              <div key={day.value} className={`roster-day${entry ? ' has-hours' : ''}`}>
                <div className="roster-day-name">{day.label}</div>
                {entry ? (
                  <div className="roster-day-hours">
                    {entry.startTime}–{entry.endTime}
                    <br />
                    {entry.slotDurationMinutes} min slots
                  </div>
                ) : (
                  <div className="roster-day-empty">No hours set</div>
                )}
              </div>
            )
          })}
        </div>
      </div>

      <div className="card">
        <h2>Set availability for a day</h2>
        <p className="card-desc">Applies every week on this day until changed.</p>
        <form onSubmit={handleSaveRoster}>
          <div className="form-row">
            <div className="field">
              <label htmlFor="day">Day of week</label>
              <select
                id="day"
                value={form.dayOfWeek}
                onChange={(e) => setForm({ ...form, dayOfWeek: e.target.value })}
              >
                {DAYS.map((d) => (
                  <option key={d.value} value={d.value}>
                    {d.label}
                  </option>
                ))}
              </select>
            </div>
            <div className="field">
              <label htmlFor="start">Start time</label>
              <input
                id="start"
                type="time"
                value={form.startTime}
                onChange={(e) => setForm({ ...form, startTime: e.target.value })}
              />
            </div>
            <div className="field">
              <label htmlFor="end">End time</label>
              <input
                id="end"
                type="time"
                value={form.endTime}
                onChange={(e) => setForm({ ...form, endTime: e.target.value })}
              />
            </div>
            <div className="field">
              <label htmlFor="duration">Slot length (min)</label>
              <input
                id="duration"
                type="number"
                min="5"
                step="5"
                value={form.slotDurationMinutes}
                onChange={(e) => setForm({ ...form, slotDurationMinutes: e.target.value })}
              />
            </div>
            <button className="btn btn-primary" disabled={saving || !selectedDoctorId}>
              {saving ? 'Saving…' : 'Save availability'}
            </button>
          </div>
        </form>
        {message && <div className={`status-msg ${message.type}`}>{message.text}</div>}
      </div>

      <div className="card">
        <h2>Generate bookable slots</h2>
        <p className="card-desc">
          Turns the availability above into individual time slots patients can book, for the date range you choose.
        </p>
        <form onSubmit={handleGenerateSlots}>
          <div className="form-row">
            <div className="field">
              <label htmlFor="rangeStart">From</label>
              <input
                id="rangeStart"
                type="date"
                value={range.startDate}
                onChange={(e) => setRange({ ...range, startDate: e.target.value })}
              />
            </div>
            <div className="field">
              <label htmlFor="rangeEnd">To</label>
              <input
                id="rangeEnd"
                type="date"
                value={range.endDate}
                onChange={(e) => setRange({ ...range, endDate: e.target.value })}
              />
            </div>
            <button
              className="btn btn-primary"
              disabled={generating || !selectedDoctorId || !range.startDate || !range.endDate}
            >
              {generating ? 'Generating…' : 'Generate slots'}
            </button>
          </div>
        </form>
        {generateMessage && (
          <div className={`status-msg ${generateMessage.type}`}>{generateMessage.text}</div>
        )}
      </div>
    </div>
  )
}
