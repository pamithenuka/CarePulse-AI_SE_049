import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { registerNurse } from "../api/staffApi";
import "./RegisterPatientPage.css";

const emptyForm = {
  email: "",
  password: "",
  fullName: "",
  licenseNumber: "",
  specialization: "",
};

export default function RegisterNursePage() {
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
    if (!form.licenseNumber.trim()) nextErrors.licenseNumber = "License number is required.";
    if (!form.specialization.trim()) nextErrors.specialization = "Specialization is required.";

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!validate()) return;

    setIsSaving(true);
    setSubmitError("");
    try {
      await registerNurse(form);
      navigate("/patients");
    } catch (err) {
      setSubmitError(err.response?.data?.message || "Failed to register nurse.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="register-patient-page">
      <Link to="/patients" className="back-link">
        &larr; Back to registry
      </Link>
      <h1>Register New Nurse</h1>
      <p className="page-subtitle">
        Creates the nurse's login account and a minimal profile together. Dispatch availability and location are managed separately.
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

        <h2>Nurse Details</h2>
        <div className="form-grid">
          <label>
            Full name
            <input value={form.fullName} onChange={(e) => setForm((f) => ({ ...f, fullName: e.target.value }))} />
            {errors.fullName && <span className="field-error">{errors.fullName}</span>}
          </label>

          <label>
            License number
            <input value={form.licenseNumber} onChange={(e) => setForm((f) => ({ ...f, licenseNumber: e.target.value }))} />
            {errors.licenseNumber && <span className="field-error">{errors.licenseNumber}</span>}
          </label>

          <label>
            Specialization
            <input value={form.specialization} onChange={(e) => setForm((f) => ({ ...f, specialization: e.target.value }))} />
            {errors.specialization && <span className="field-error">{errors.specialization}</span>}
          </label>
        </div>

        <div className="form-actions">
          <button type="submit" disabled={isSaving}>
            {isSaving ? "Registering..." : "Register nurse"}
          </button>
        </div>
      </form>
    </div>
  );
}
