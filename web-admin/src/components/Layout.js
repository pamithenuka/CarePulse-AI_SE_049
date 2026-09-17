import { Link, Outlet } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "./Layout.css";

export default function Layout() {
  const { user, logout } = useAuth();

  return (
    <div className="app-shell">
      <header className="app-header">
        <Link to="/patients" className="brand">
          CarePulse
        </Link>
        <nav className="app-nav">
          <Link to="/patients">Patients</Link>
          <Link to="/audit-log">Audit Log</Link>
          <Link to="/emergency-alerts">Emergency Alerts</Link>
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
