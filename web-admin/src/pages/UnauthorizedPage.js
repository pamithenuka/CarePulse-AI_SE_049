import { Link } from "react-router-dom";
import "../components/StatusView.css";

export default function UnauthorizedPage() {
  return (
    <div className="status-view">
      <p>You don't have permission to view this page.</p>
      <Link to="/patients">Go to Patients</Link>
    </div>
  );
}
