import { NavLink } from 'react-router-dom';

const links = [
  { to: '/doctors', label: 'Doctors' },
  { to: '/roster', label: 'Weekly roster' },
  { to: '/slots', label: 'Appointment slots' },
  { to: '/consultations', label: 'Consultations' }
];

export default function Sidebar() {
  return (
    <aside className="sidebar">
      <div className="sidebar-brand">
        CarePulse
        <span>Clinic scheduling</span>
      </div>
      <nav className="sidebar-nav">
        {links.map((link) => (
          <NavLink
            key={link.to}
            to={link.to}
            className={({ isActive }) => 'sidebar-link' + (isActive ? ' active' : '')}
          >
            {link.label}
          </NavLink>
        ))}
      </nav>
    </aside>
  );
}
