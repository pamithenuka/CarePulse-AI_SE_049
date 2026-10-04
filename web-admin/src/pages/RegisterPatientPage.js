import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { registerPatient } from "../api/patientsApi";
import "./RegisterPatientPage.css";

const NIC_PATTERN = /^(\d{9}[VvXx]|\d{12})$/;
const PHONE_PATTERN = /^0\d{9}$/;
const TODAY = new Date().toISOString().slice(0, 10);

const emptyContact = { fullName: "", relationshipToPatient: "", phoneNumber: "", isPrimary: false };

const emptyForm = {
  email: "",
  password: "",
  fullName: "",
  dateOfBirth: "",
  gender: "Female",
  bloodType: "",
  phoneNumber: "",
  address: "",
  nationalId: "",
  allergies: "",
};

export default function RegisterPatientPage() {
  const navigate = useNavigate();
  const [form, setForm] = useState(emptyForm);
  const [contacts, setContacts] = useState([{ ...emptyContact, isPrimary: true }]);
  const [errors, setErrors] = useState({});
  const [submitError, setSubmitError] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  const updateContact = (index, field, value) => {
    setContacts((prev) => prev.map((c, i) => (i === index ? { ...c, [field]: value } : c)));
  };

  const addContactRow = () => setContacts((prev) => [...prev, { ...emptyContact }]);
  const removeContactRow = (index) => setContacts((prev) => prev.filter((_, i) => i !== index));

  const validate = () => {
    const nextErrors = {};
    if (!/^\S+@\S+\.\S+$/.test(form.email)) nextErrors.email = "Enter a valid email address.";
    if (form.password.length < 8) nextErrors.password = "Password must be at least 8 characters.";
    if (!form.fullName.trim()) nextErrors.fullName = "Full name is required.";
    if (!form.dateOfBirth) {
      nextErrors.dateOfBirth = "Date of birth is required.";
    } else if (form.dateOfBirth > TODAY) {
      nextErrors.dateOfBirth = "Date of birth cannot be in the future.";
    }
    if (!NIC_PATTERN.test(form.nationalId)) {
      nextErrors.nationalId = "Enter 9 digits + V/X, or 12 digits.";
    }
    if (!PHONE_PATTERN.test(form.phoneNumber)) {
      nextErrors.phoneNumber = "Enter a 10-digit number starting with 0.";
    }

    const validContacts = contacts.filter((c) => c.fullName.trim() || c.relationshipToPatient.trim() || c.phoneNumber.trim());
    validContacts.forEach((c, i) => {
      if (!c.fullName.trim() || !c.relationshipToPatient.trim() || !PHONE_PATTERN.test(c.phoneNumber)) {
        nextErrors[`contact-${i}`] = "Fill in name, relationship and a valid phone number, or remove this row.";
      }
    });

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!validate()) return;

    setIsSaving(true);
    setSubmitError("");
    try {
      const payload = {
        ...form,
        emergencyContacts: contacts.filter((c) => c.fullName.trim() && c.relationshipToPatient.trim() && c.phoneNumber.trim()),
      };
      const created = await registerPatient(payload);
      navigate(`/patients/${created.id}`);
    } catch (err) {
      setSubmitError(err.response?.data?.message || "Failed to register patient.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="register-patient-page">
      <Link to="/patients" className="back-link">
        &larr; Back to registry
      </Link>
      <h1>Register New Patient</h1>
      <p className="page-subtitle">
        Onboard a patient entirely from the web app — creates their login account and profile together. No mobile app needed.
      </p>

      <form className="detail-card register-form" onSubmit={handleSubmit}>
        {submitError && (
          <div className="alert alert-error" role="alert">
            {submitError}
          </div>
        )}

        <h2>Login Account</h2>
        <div className="form-grid">
          <label>
            Email
            <input
              type="email"
              value={form.email}
              onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
            />
            {errors.email && <span className="field-error">{errors.email}</span>}
          </label>
          <label>
            Temporary password
            <input
              type="text"
              value={form.password}
              onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
            />
            {errors.password && <span className="field-error">{errors.password}</span>}
          </label>
        </div>

        <h2>Patient Details</h2>
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
              max={TODAY}
              value={form.dateOfBirth}
              onChange={(e) => setForm((f) => ({ ...f, dateOfBirth: e.target.value }))}
            />
            {errors.dateOfBirth && <span className="field-error">{errors.dateOfBirth}</span>}
          </label>

          <label>
            Gender
            <select value={form.gender} onChange={(e) => setForm((f) => ({ ...f, gender: e.target.value }))}>
              <option value="Female">Female</option>
              <option value="Male">Male</option>
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

        <h2>Emergency Contacts</h2>
        {contacts.map((contact, index) => (
          <div className="contact-row" key={index}>
            <input
              placeholder="Full name"
              value={contact.fullName}
              onChange={(e) => updateContact(index, "fullName", e.target.value)}
            />
            <input
              placeholder="Relationship"
              value={contact.relationshipToPatient}
              onChange={(e) => updateContact(index, "relationshipToPatient", e.target.value)}
            />
            <input
              placeholder="Phone number"
              value={contact.phoneNumber}
              onChange={(e) => updateContact(index, "phoneNumber", e.target.value)}
            />
            <label className="checkbox-label">
              <input
                type="checkbox"
                checked={contact.isPrimary}
                onChange={(e) => updateContact(index, "isPrimary", e.target.checked)}
              />
              Primary
            </label>
            {contacts.length > 1 && (
              <button type="button" className="btn-danger-link" onClick={() => removeContactRow(index)}>
                Remove
              </button>
            )}
            {errors[`contact-${index}`] && <span className="field-error span-full">{errors[`contact-${index}`]}</span>}
          </div>
        ))}
        <button type="button" className="btn-secondary" onClick={addContactRow}>
          + Add another contact
        </button>

        <div className="form-actions">
          <button type="submit" disabled={isSaving}>
            {isSaving ? "Registering..." : "Register patient"}
          </button>
        </div>
      </form>
    </div>
  );
}
