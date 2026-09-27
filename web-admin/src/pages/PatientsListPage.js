import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getPatients } from "../api/patientsApi";
import { useAuth } from "../context/AuthContext";
import { EmptyState, ErrorState, LoadingState } from "../components/StatusView";
import "../components/StatusView.css";
import "./PatientsListPage.css";

const PAGE_SIZE = 10;
const BLOOD_TYPES = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"];

export default function PatientsListPage() {
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");

  const [search, setSearch] = useState("");
  const [bloodGroup, setBloodGroup] = useState("");
  const [condition, setCondition] = useState("");
  const [minAge, setMinAge] = useState("");
  const [maxAge, setMaxAge] = useState("");
  const [status, setStatus] = useState("active");
  const [sortBy, setSortBy] = useState("fullName");
  const [descending, setDescending] = useState(false);
  const [page, setPage] = useState(1);

  const [result, setResult] = useState(null);
  const [status_, setLoadStatus] = useState("loading");
  const [errorMessage, setErrorMessage] = useState("");

  const loadPatients = useCallback(async () => {
    setLoadStatus("loading");
    try {
      const data = await getPatients({
        search: search || undefined,
        bloodGroup: bloodGroup || undefined,
        condition: condition || undefined,
        minAge: minAge || undefined,
        maxAge: maxAge || undefined,
        status,
        sortBy,
        descending,
        page,
        pageSize: PAGE_SIZE,
      });
      setResult(data);
      setLoadStatus("success");
    } catch (err) {
      setErrorMessage(err.response?.data?.message || "Failed to load patients.");
      setLoadStatus("error");
    }
  }, [search, bloodGroup, condition, minAge, maxAge, status, sortBy, descending, page]);

  useEffect(() => {
    loadPatients();
  }, [loadPatients]);

  const handleSort = (column) => {
    if (sortBy === column) {
      setDescending((prev) => !prev);
    } else {
      setSortBy(column);
      setDescending(false);
    }
    setPage(1);
  };

  const sortIndicator = (column) => (sortBy === column ? (descending ? " ↓" : " ↑") : "");
  const resetPage = (setter) => (value) => {
    setPage(1);
    setter(value);
  };

  return (
    <div>
      <div className="page-header-row">
        <div>
          <h1>Patient Master Registry</h1>
          <p className="page-subtitle">Search, filter and review registered patients.</p>
        </div>
        {isAdmin && (
          <Link to="/patients/new" className="btn-primary-link">
            + Register New Patient
          </Link>
        )}
      </div>

      <div className="filters">
        <input
          type="search"
          placeholder="Search by name, phone or National ID"
          value={search}
          onChange={(e) => resetPage(setSearch)(e.target.value)}
        />
        <input
          type="text"
          placeholder="Condition (e.g. asthma)"
          value={condition}
          onChange={(e) => resetPage(setCondition)(e.target.value)}
        />
        <select value={bloodGroup} onChange={(e) => resetPage(setBloodGroup)(e.target.value)}>
          <option value="">All blood types</option>
          {BLOOD_TYPES.map((type) => (
            <option key={type} value={type}>
              {type}
            </option>
          ))}
        </select>
        <input
          type="number"
          min="0"
          placeholder="Min age"
          className="age-input"
          value={minAge}
          onChange={(e) => resetPage(setMinAge)(e.target.value)}
        />
        <input
          type="number"
          min="0"
          placeholder="Max age"
          className="age-input"
          value={maxAge}
          onChange={(e) => resetPage(setMaxAge)(e.target.value)}
        />
        {isAdmin && (
          <select value={status} onChange={(e) => resetPage(setStatus)(e.target.value)}>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
            <option value="all">All</option>
          </select>
        )}
      </div>

      {status_ === "loading" && <LoadingState label="Loading patients..." />}
      {status_ === "error" && <ErrorState message={errorMessage} onRetry={loadPatients} />}
      {status_ === "success" && result.items.length === 0 && (
        <EmptyState message="No patients match the current filters." />
      )}

      {status_ === "success" && result.items.length > 0 && (
        <>
          <table className="patients-table">
            <thead>
              <tr>
                <th onClick={() => handleSort("fullName")}>Full Name{sortIndicator("fullName")}</th>
                <th onClick={() => handleSort("age")}>Age{sortIndicator("age")}</th>
                <th>Gender</th>
                <th>Blood Type</th>
                <th>Main Conditions</th>
                <th>National ID</th>
                <th>Status</th>
                <th onClick={() => handleSort("createdAt")}>Registered{sortIndicator("createdAt")}</th>
              </tr>
            </thead>
            <tbody>
              {result.items.map((patient) => (
                <tr key={patient.id}>
                  <td>
                    <Link to={`/patients/${patient.id}`}>{patient.fullName}</Link>
                  </td>
                  <td>{patient.age}</td>
                  <td>{patient.gender}</td>
                  <td>{patient.bloodType || "-"}</td>
                  <td>{patient.mainConditions || "-"}</td>
                  <td>{patient.nationalId}</td>
                  <td>
                    <span className={`status-pill status-${patient.status.toLowerCase()}`}>{patient.status}</span>
                  </td>
                  <td>{new Date(patient.createdAt).toLocaleDateString()}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="pagination">
            <button type="button" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
              Previous
            </button>
            <span>
              Page {result.page} of {Math.max(result.totalPages, 1)} ({result.totalCount} patients)
            </span>
            <button
              type="button"
              disabled={page >= result.totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
            </button>
          </div>
        </>
      )}
    </div>
  );
}
