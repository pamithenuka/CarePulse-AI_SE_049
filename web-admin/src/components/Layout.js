import { Link, Outlet } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "./Layout.css";

export default function Layout() {
  const { user, logout, hasRole } = useAuth();
  const isAdmin = hasRole("Admin");

  return (
    <div className="app-shell">
      <header className="app-header">
        <Link to="/patients" className="brand">
          CarePulse
        </Link>
        <nav className="app-nav">
          <Link to="/patients">Patients</Link>
          <Link to="/emergency-alerts">Emergency Alerts</Link>
          <Link to="/dispatch">Dispatch Center</Link>
          {isAdmin && <Link to="/audit-log">Audit Log</Link>}
          {isAdmin && <Link to="/doctors">Doctors</Link>}
          {isAdmin && <Link to="/nurses">Nurses</Link>}
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
      <main className="app-content">
        <Outlet />
      </main>
    </div>
  );
}
