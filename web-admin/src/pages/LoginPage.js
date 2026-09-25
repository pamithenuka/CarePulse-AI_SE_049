import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "./LoginPage.css";

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("doctor@carepulse.dev");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setError("");
    setIsSubmitting(true);
    try {
      await login(email, password);
      const redirectTo = location.state?.from?.pathname || "/patients";
      navigate(redirectTo, { replace: true });
    } catch (err) {
      setError(err.response?.data?.message || "Unable to sign in. Check your credentials.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="login-page">
      <form className="login-card" onSubmit={handleSubmit}>
        <h1>CarePulse Clinical Portal</h1>
        <p className="subtitle">Sign in to manage patient records</p>

        {error && (
          <div className="alert alert-error" role="alert">
            {error}
          </div>
        )}

        <label htmlFor="email">Email</label>
        <input
          id="email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />

        <label htmlFor="password">Password</label>
        <input
          id="password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Signing in..." : "Sign in"}
        </button>

        <p className="hint">
          Seeded accounts: <code>doctor@carepulse.dev</code> / <code>Doctor@12345</code>
          <br />
          or <code>admin@carepulse.dev</code> / <code>Admin@12345</code>
        </p>
      </form>
    </div>
  );
}
