import { NavLink, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "./Layout.css";

export default function Layout() {
  const { user, logout, hasRole } = useAuth();
  const { pathname } = useLocation();
  const section = pathname.startsWith("/triage") ? "ai" : /^\/(doctors|slots|roster|consultations)/.test(pathname) ? "appointments" : pathname.startsWith("/patients") ? "records" : "care";
  const isAdmin = hasRole("Admin");

  return (
    <div className="app-shell" data-section={section}>
      <a className="skip-link" href="#main-content">Skip to content</a>
      <header className="app-header">
        <NavLink to="/patients" className="brand">
          CarePulse
        </NavLink>
        <nav className="app-nav" aria-label="Main navigation">
          <NavLink to="/patients">Patients</NavLink>
          <NavLink to="/emergency-alerts">Emergency Alerts</NavLink>
          <NavLink to="/dispatch">Dispatch Center</NavLink>
          <NavLink to="/triage">AI Triage</NavLink>
          <NavLink to="/roster">Rosters</NavLink>
          <NavLink to="/slots">Slots</NavLink>
          <NavLink to="/consultations">Consultations</NavLink>
          {isAdmin && <NavLink to="/audit-log">Audit Log</NavLink>}
          {isAdmin && <NavLink to="/doctors">Doctors</NavLink>}
          {isAdmin && <NavLink to="/nurses">Nurses</NavLink>}
        </nav>
        <div className="app-user">
          <span>
            {user?.fullName} <em>({user?.roles?.join(", ")})</em>
          </span>
          <button type="button" onClick={logout}>
            Log out
          </button>
        </div>
      </header>
      <main id="main-content" tabIndex={-1} className="app-content">
        <Outlet />
      </main>
    </div>
  );
}
