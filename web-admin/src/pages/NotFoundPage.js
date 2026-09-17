import { Link } from "react-router-dom";
import "../components/StatusView.css";

export default function NotFoundPage() {
  return (
    <div className="status-view">
      <p>Page not found.</p>
      <Link to="/patients">Go to Patients</Link>
    </div>
  );
}
