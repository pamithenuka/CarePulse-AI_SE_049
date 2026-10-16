import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import {
  Mail, Lock, Eye, EyeOff, ArrowRight, Loader2, HeartPulse, Users, BrainCircuit, Truck, ShieldCheck,
} from "lucide-react";
import { useAuth } from "../context/AuthContext";
import "./LoginPage.css";

const SLIDES = [
  { icon: Users, title: "One record for every patient", text: "Profiles, medical history, documents and care plans together in one secure place." },
  { icon: BrainCircuit, title: "AI assistance, doctor approved", text: "Care plans and triage suggestions always wait for a clinician's review." },
  { icon: Truck, title: "Faster response when it matters", text: "Dispatch nurses, follow their arrival and capture vitals on site." },
];

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showSecret, setShowSecret] = useState(false);
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isLeaving, setIsLeaving] = useState(false);
  const [slide, setSlide] = useState(0);
  const [shakeKey, setShakeKey] = useState(0);
  const leaveTimer = useRef(null);

  useEffect(() => {
    const timer = setInterval(() => setSlide((s) => (s + 1) % SLIDES.length), 5500);
    return () => clearInterval(timer);
  }, []);

  useEffect(() => () => clearTimeout(leaveTimer.current), []);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setError("");
    setIsSubmitting(true);
    try {
      await login(email, password);
      const redirectTo = location.state?.from?.pathname || "/patients";
      setIsLeaving(true);
      leaveTimer.current = setTimeout(() => navigate(redirectTo, { replace: true }), 260);
    } catch (err) {
      setError(err.response?.data?.message || "Unable to sign in. Check your credentials.");
      setShakeKey((k) => k + 1);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className={`login-page login-split${isLeaving ? " is-leaving" : ""}`}>
      <aside className="login-hero" aria-hidden="true">
        <span className="hero-orb hero-orb-a" />
        <span className="hero-orb hero-orb-b" />
        <span className="hero-orb hero-orb-c" />
        <div className="hero-brand">
          <span className="hero-brand-mark"><HeartPulse size={22} strokeWidth={2.4} /></span>
          <span>CarePulse</span>
        </div>

        <div className="hero-slides">
          {SLIDES.map(({ icon: Icon, title, text }, i) => (
            <div key={title} className={`hero-slide${i === slide ? " is-active" : ""}`}>
              <span className="hero-slide-icon"><Icon size={26} strokeWidth={1.8} /></span>
              <h2>{title}</h2>
              <p>{text}</p>
            </div>
          ))}
        </div>

        <div className="hero-dots">
          {SLIDES.map((s, i) => (
            <span key={s.title} className={`hero-dot${i === slide ? " is-active" : ""}`} />
          ))}
        </div>

        <svg className="hero-ecg" viewBox="0 0 600 120" preserveAspectRatio="none">
          <path d="M0 60 H120 L145 60 L165 18 L195 102 L215 60 H330 L350 60 L370 30 L392 88 L408 60 H600" />
        </svg>

        <ul className="hero-chips">
          <li><ShieldCheck size={15} /> Role-based access</li>
          <li><ShieldCheck size={15} /> Full audit trail</li>
          <li><ShieldCheck size={15} /> Human-approved AI</li>
        </ul>
      </aside>

      <section className="login-panel">
        <form className="login-card" onSubmit={handleSubmit}>
          <span className="login-logo"><HeartPulse size={26} strokeWidth={2.3} /></span>
          <h1>CarePulse Clinical Portal</h1>
          <p className="subtitle">Sign in to manage patient records</p>

          {error && (
            <div key={shakeKey} className="alert alert-error login-error" role="alert">
              {error}
            </div>
          )}

          <label htmlFor="email">Email</label>
          <div className="login-field">
            <Mail size={18} aria-hidden="true" />
            <input
              id="email"
              autoComplete="username"
              type="email"
              placeholder="you@hospital.lk"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>

          <label htmlFor="password">Password</label>
          <div className="login-field">
            <Lock size={18} aria-hidden="true" />
            <input
              id="password"
              autoComplete="current-password"
              type={showSecret ? "text" : "password"}
              placeholder="Enter your password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
            <button
              type="button"
              className="login-eye"
              aria-label="Toggle visibility"
              aria-pressed={showSecret}
              onClick={() => setShowSecret((v) => !v)}
            >
              {showSecret ? <EyeOff size={18} /> : <Eye size={18} />}
            </button>
          </div>

          <button type="submit" className="login-submit" disabled={isSubmitting}>
            {isSubmitting ? (
              <><Loader2 size={18} className="spin" aria-hidden="true" /> Signing in...</>
            ) : (
              <>Sign in <ArrowRight size={18} aria-hidden="true" /></>
            )}
          </button>

          <p className="hint">
            Use your staff account. Contact your administrator if you need access.
          </p>
        </form>
      </section>
    </div>
  );
}
