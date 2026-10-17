import { useEffect, useId, useRef, useState } from "react";
import { Search, UserRound } from "lucide-react";
import { getPatients } from "../api/patientsApi";

const describe = (p) => [p.nationalId, [p.age != null ? `${p.age}y` : null, p.gender].filter(Boolean).join(" ")].filter(Boolean).join(" · ");

/**
 * Search for a patient by name, National ID or phone and choose one.
 * `value` is the chosen patient object (or null); `onChange` receives the patient or null.
 */
export default function PatientPicker({ value, onChange, debounceMs = 300 }) {
  const uid = useId();
  const inputId = `${uid}-input`;
  const listId = `${uid}-list`;
  const boxRef = useRef(null);
  const [query, setQuery] = useState("");
  const [results, setResults] = useState([]);
  const [open, setOpen] = useState(false);
  const [status, setStatus] = useState("idle");
  const [active, setActive] = useState(-1);

  // Search the registry whenever the dropdown is open and the text changes.
  useEffect(() => {
    if (!open) return undefined;
    let cancelled = false;
    setStatus("loading");
    const timer = setTimeout(async () => {
      try {
        const data = await getPatients({ search: query.trim() || undefined, pageSize: 8 });
        if (cancelled) return;
        setResults(data.items || []);
        setActive(-1);
        setStatus("ready");
      } catch {
        if (!cancelled) setStatus("error");
      }
    }, debounceMs);
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [query, open, debounceMs]);

  // Close the list when the user clicks elsewhere.
  useEffect(() => {
    const onDown = (e) => {
      if (boxRef.current && !boxRef.current.contains(e.target)) setOpen(false);
    };
    document.addEventListener("mousedown", onDown);
    return () => document.removeEventListener("mousedown", onDown);
  }, []);

  const choose = (patient) => {
    onChange(patient);
    setOpen(false);
    setQuery("");
  };

  const onKeyDown = (e) => {
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setOpen(true);
      setActive((i) => Math.min(i + 1, results.length - 1));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setActive((i) => Math.max(i - 1, 0));
    } else if (e.key === "Enter" && open && active >= 0 && results[active]) {
      e.preventDefault();
      choose(results[active]);
    } else if (e.key === "Escape") {
      setOpen(false);
    }
  };

  if (value) {
    return (
      <div className="doctor-picker patient-picker patient-picker-selected">
        <span className="patient-picker-icon" aria-hidden="true"><UserRound size={18} /></span>
        <div className="patient-picker-chosen">
          <strong>{value.fullName}</strong>
          <small>{describe(value)}</small>
        </div>
        <button type="button" className="btn btn-secondary" onClick={() => onChange(null)}>
          Change
        </button>
      </div>
    );
  }

  return (
    <div className="doctor-picker patient-picker" ref={boxRef}>
      <label htmlFor={inputId}>Patient</label>
      <div className="patient-picker-field">
        <Search size={17} aria-hidden="true" />
        <input
          id={inputId}
          type="text"
          role="combobox"
          aria-expanded={open}
          aria-controls={listId}
          aria-autocomplete="list"
          aria-activedescendant={active >= 0 ? `${listId}-${active}` : undefined}
          autoComplete="off"
          placeholder="Search by name, National ID or phone"
          value={query}
          onFocus={() => setOpen(true)}
          onChange={(e) => {
            setQuery(e.target.value);
            setOpen(true);
          }}
          onKeyDown={onKeyDown}
        />
        {open && (
          <div className="patient-picker-menu">
            {status === "loading" && <div className="patient-picker-note" role="status">Searching...</div>}
            {status === "error" && <div className="patient-picker-note patient-picker-error">Could not load patients. Try again.</div>}
            {status === "ready" && results.length === 0 && <div className="patient-picker-note">No patients found.</div>}
            {status === "ready" && results.length > 0 && (
              <ul id={listId} role="listbox" aria-label="Patients">
                {results.map((p, i) => (
                  <li
                    key={p.id}
                    id={`${listId}-${i}`}
                    role="option"
                    aria-selected={i === active}
                    className={i === active ? "is-active" : undefined}
                    onMouseDown={(e) => {
                      e.preventDefault();
                      choose(p);
                    }}
                    onMouseEnter={() => setActive(i)}
                  >
                    <strong>{p.fullName}</strong>
                    <small>{describe(p)}</small>
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
