import { NavLink, Outlet, useLocation } from "react-router-dom";
import {
  Users, Siren, Truck, BrainCircuit, CalendarDays, Clock, Stethoscope,
  ScrollText, UserCog, Syringe, HeartPulse, LogOut,
} from "lucide-react";
import { useAuth } from "../context/AuthContext";
import "./Layout.css";

const PAGE_TITLES = [
  ["/patients/new", "Register patient"],
  ["/patients", "Patient records"],
  ["/emergency-alerts", "Emergency alerts"],
  ["/dispatch", "Dispatch center"],
  ["/triage", "AI triage"],
  ["/roster", "Weekly rosters"],
  ["/slots", "Appointment slots"],
  ["/consultations", "Consultations"],
  ["/audit-log", "Audit log"],
  ["/doctors", "Doctors"],
  ["/nurses", "Nurses"],
];

const initials = (name = "") =>
  name.split(/\s+/).filter(Boolean).slice(0, 2).map((p) => p[0].toUpperCase()).join("") || "U";

function NavItem({ to, icon: Icon, children }) {
  return (
    <NavLink to={to}>
      <Icon size={18} strokeWidth={2} aria-hidden="true" />
      <span>{children}</span>
    </NavLink>
  );
}

export default function Layout() {
  const { user, logout, hasRole } = useAuth();
  const { pathname } = useLocation();
  const section = pathname.startsWith("/triage") ? "ai" : /^\/(doctors|slots|roster|consultations)/.test(pathname) ? "appointments" : pathname.startsWith("/patients") ? "records" : "care";
  const isAdmin = hasRole("Admin");
  const title = (PAGE_TITLES.find(([prefix]) => pathname.startsWith(prefix)) || [])[1] || "CarePulse";
  const today = new Date().toLocaleDateString(undefined, { weekday: "long", day: "numeric", month: "long", year: "numeric" });

  return (
    <div className="app-shell" data-section={section}>
      <a className="skip-link" href="#main-content">Skip to content</a>
      <header className="app-header">
        <NavLink to="/patients" className="brand">
          <span className="brand-mark" aria-hidden="true"><HeartPulse size={20} strokeWidth={2.4} /></span>
          <span className="brand-text">
            CarePulse
            <small>Clinical Portal</small>
          </span>
        </NavLink>
        <nav className="app-nav" aria-label="Main navigation">
          <span className="nav-group-label">Clinical</span>
          <NavItem to="/patients" icon={Users}>Patients</NavItem>
          <NavItem to="/emergency-alerts" icon={Siren}>Emergency Alerts</NavItem>
          <NavItem to="/dispatch" icon={Truck}>Dispatch Center</NavItem>
          <NavItem to="/triage" icon={BrainCircuit}>AI Triage</NavItem>
          <span className="nav-group-label">Scheduling</span>
          <NavItem to="/roster" icon={CalendarDays}>Rosters</NavItem>
          <NavItem to="/slots" icon={Clock}>Slots</NavItem>
          <NavItem to="/consultations" icon={Stethoscope}>Consultations</NavItem>
          {isAdmin && <span className="nav-group-label">Administration</span>}
          {isAdmin && <NavItem to="/audit-log" icon={ScrollText}>Audit Log</NavItem>}
          {isAdmin && <NavItem to="/doctors" icon={UserCog}>Doctors</NavItem>}
          {isAdmin && <NavItem to="/nurses" icon={Syringe}>Nurses</NavItem>}
        </nav>
        <div className="app-user">
          <div className="app-user-card">
            <span className="avatar" aria-hidden="true">{initials(user?.fullName)}</span>
            <span className="app-user-info">
              <strong>{user?.fullName}</strong>
              <em>{user?.roles?.join(", ")}</em>
            </span>
          </div>
          <button type="button" onClick={logout}>
            <LogOut size={16} aria-hidden="true" />
            Log out
          </button>
        </div>
      </header>
      <div className="app-main">
        <div className="app-topbar">
          <h2 className="app-topbar-title">{title}</h2>
          <span className="app-topbar-date">{today}</span>
        </div>
        <main id="main-content" tabIndex={-1} className="app-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
