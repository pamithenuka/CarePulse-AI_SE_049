import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { registerDoctor } from "../api/staffApi";
import "./RegisterPatientPage.css";

const PHONE_PATTERN = /^0\d{9}$/;

const emptyForm = {
  email: "",
  password: "",
  fullName: "",
  specialty: "",
  phoneNumber: "",
};

export default function RegisterDoctorPage() {
  const navigate = useNavigate();
  const [form, setForm] = useState(emptyForm);
  const [errors, setErrors] = useState({});
  const [submitError, setSubmitError] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  const validate = () => {
    const nextErrors = {};
    if (!/^\S+@\S+\.\S+$/.test(form.email)) nextErrors.email = "Enter a valid email address.";
    if (form.password.length < 8) nextErrors.password = "Password must be at least 8 characters.";
    if (!form.fullName.trim()) nextErrors.fullName = "Full name is required.";
    if (!form.specialty.trim()) nextErrors.specialty = "Specialty is required.";
    if (!PHONE_PATTERN.test(form.phoneNumber)) {
      nextErrors.phoneNumber = "Enter a 10-digit number starting with 0.";
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!validate()) return;

    setIsSaving(true);
    setSubmitError("");
    try {
      await registerDoctor(form);
      navigate("/patients");
    } catch (err) {
      setSubmitError(err.response?.data?.message || "Failed to register doctor.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="register-patient-page">
      <Link to="/patients" className="back-link">
        &larr; Back to registry
      </Link>
      <h1>Register New Doctor</h1>
      <p className="page-subtitle">
        Creates the doctor's login account and a minimal profile together. Clinic rostering and scheduling are managed separately.
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
            <input type="email" value={form.email} onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))} />
            {errors.email && <span className="field-error">{errors.email}</span>}
          </label>
          <label>
            Temporary password
            <input type="text" value={form.password} onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))} />
            {errors.password && <span className="field-error">{errors.password}</span>}
          </label>
        </div>

        <h2>Doctor Details</h2>
        <div className="form-grid">
          <label>
            Full name
            <input value={form.fullName} onChange={(e) => setForm((f) => ({ ...f, fullName: e.target.value }))} />
            {errors.fullName && <span className="field-error">{errors.fullName}</span>}
          </label>

          <label>
            Specialty
            <input value={form.specialty} onChange={(e) => setForm((f) => ({ ...f, specialty: e.target.value }))} />
            {errors.specialty && <span className="field-error">{errors.specialty}</span>}
          </label>

          <label>
            Phone number
            <input value={form.phoneNumber} onChange={(e) => setForm((f) => ({ ...f, phoneNumber: e.target.value }))} />
            {errors.phoneNumber && <span className="field-error">{errors.phoneNumber}</span>}
          </label>
        </div>

        <div className="form-actions">
          <button type="submit" disabled={isSaving}>
            {isSaving ? "Registering..." : "Register doctor"}
          </button>
        </div>
      </form>
    </div>
  );
}
