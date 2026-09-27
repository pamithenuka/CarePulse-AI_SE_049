import { useState } from "react";
import { updatePatientProfile } from "../../api/patientsApi";
import "./PatientTabs.css";

const NIC_PATTERN = /^(\d{9}[VvXx]|\d{12})$/;
const PHONE_PATTERN = /^0\d{9}$/;

function toEditForm(profile) {
  return {
    fullName: profile.fullName,
    dateOfBirth: profile.dateOfBirth,
    gender: profile.gender,
    bloodType: profile.bloodType || "",
    phoneNumber: profile.phoneNumber,
    address: profile.address || "",
    nationalId: profile.nationalId,
    allergies: profile.allergies || "",
  };
}

export default function OverviewTab({ profile, isAdmin, onUpdated }) {
  const [isEditing, setIsEditing] = useState(false);
  const [form, setForm] = useState(() => toEditForm(profile));
  const [errors, setErrors] = useState({});
  const [saveError, setSaveError] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  const startEditing = () => {
    setForm(toEditForm(profile));
    setErrors({});
    setSaveError("");
    setIsEditing(true);
  };

  const validate = () => {
    const nextErrors = {};
    if (!form.fullName.trim()) nextErrors.fullName = "Full name is required.";
    if (!form.dateOfBirth) {
      nextErrors.dateOfBirth = "Date of birth is required.";
    } else if (form.dateOfBirth > new Date().toISOString().slice(0, 10)) {
      nextErrors.dateOfBirth = "Date of birth cannot be in the future.";
    }
    if (!NIC_PATTERN.test(form.nationalId)) {
      nextErrors.nationalId = "Enter 9 digits + V/X, or 12 digits.";
    }
    if (!PHONE_PATTERN.test(form.phoneNumber)) {
      nextErrors.phoneNumber = "Enter a 10-digit number starting with 0.";
    }
    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleSave = async (event) => {
    event.preventDefault();
    if (!validate()) return;

    setIsSaving(true);
    setSaveError("");
    try {
      await updatePatientProfile(profile.id, form);
      setIsEditing(false);
      await onUpdated();
    } catch (err) {
      setSaveError(err.response?.data?.message || "Failed to save changes.");
    } finally {
      setIsSaving(false);
    }
  };

  if (isEditing) {
    return (
      <form className="detail-card edit-form" onSubmit={handleSave}>
        <h2>Edit Patient</h2>
        {saveError && (
          <div className="alert alert-error" role="alert">
            {saveError}
          </div>
        )}

        <div className="form-grid">
          <label>
            Full name
            <input value={form.fullName} onChange={(e) => setForm((f) => ({ ...f, fullName: e.target.value }))} />
            {errors.fullName && <span className="field-error">{errors.fullName}</span>}
          </label>

          <label>
            Date of birth
            <input
              type="date"
              max={new Date().toISOString().slice(0, 10)}
              value={form.dateOfBirth}
              onChange={(e) => setForm((f) => ({ ...f, dateOfBirth: e.target.value }))}
            />
            {errors.dateOfBirth && <span className="field-error">{errors.dateOfBirth}</span>}
          </label>

          <label>
            Gender
            <select value={form.gender} onChange={(e) => setForm((f) => ({ ...f, gender: e.target.value }))}>
              <option value="Male">Male</option>
              <option value="Female">Female</option>
              <option value="Other">Other</option>
            </select>
          </label>

          <label>
            Blood type
            <select value={form.bloodType} onChange={(e) => setForm((f) => ({ ...f, bloodType: e.target.value }))}>
              <option value="">Unknown</option>
              {["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"].map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </label>

          <label>
            Phone number
            <input value={form.phoneNumber} onChange={(e) => setForm((f) => ({ ...f, phoneNumber: e.target.value }))} />
            {errors.phoneNumber && <span className="field-error">{errors.phoneNumber}</span>}
          </label>

          <label>
            National ID
            <input value={form.nationalId} onChange={(e) => setForm((f) => ({ ...f, nationalId: e.target.value }))} />
            {errors.nationalId && <span className="field-error">{errors.nationalId}</span>}
          </label>

          <label className="span-2">
            Address
            <input value={form.address} onChange={(e) => setForm((f) => ({ ...f, address: e.target.value }))} />
          </label>

          <label className="span-2">
            Allergies (comma-separated)
            <input value={form.allergies} onChange={(e) => setForm((f) => ({ ...f, allergies: e.target.value }))} />
          </label>
        </div>

        <div className="form-actions">
          <button type="submit" disabled={isSaving}>
            {isSaving ? "Saving..." : "Save changes"}
          </button>
          <button type="button" className="btn-secondary" onClick={() => setIsEditing(false)} disabled={isSaving}>
            Cancel
          </button>
        </div>
      </form>
    );
  }

  return (
    <div className="detail-card">
      <div className="card-header-row">
        <h2>Overview</h2>
        {isAdmin && (
          <button type="button" className="btn-secondary" onClick={startEditing}>
            Edit
          </button>
        )}
      </div>
      <dl className="detail-grid">
        <div>
          <dt>Date of Birth</dt>
          <dd>{profile.dateOfBirth}</dd>
        </div>
        <div>
          <dt>Gender</dt>
          <dd>{profile.gender}</dd>
        </div>
        <div>
          <dt>Blood Type</dt>
          <dd>{profile.bloodType || "-"}</dd>
        </div>
        <div>
          <dt>Phone</dt>
          <dd>{profile.phoneNumber}</dd>
        </div>
        <div>
          <dt>Address</dt>
          <dd>{profile.address || "-"}</dd>
        </div>
        <div>
          <dt>National ID</dt>
          <dd>{profile.nationalId}</dd>
        </div>
        <div>
          <dt>Allergies</dt>
          <dd>{profile.allergies || "None recorded"}</dd>
        </div>
        <div>
          <dt>Last Emergency Alert</dt>
          <dd>{profile.lastEmergencyBroadcastAt ? new Date(profile.lastEmergencyBroadcastAt).toLocaleString() : "Never"}</dd>
        </div>
      </dl>
    </div>
  );
}
