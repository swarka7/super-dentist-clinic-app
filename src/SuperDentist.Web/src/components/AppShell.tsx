import {
  Activity,
  CalendarDays,
  ClipboardList,
  LayoutDashboard,
  Stethoscope,
  Users,
} from 'lucide-react';
import { NavLink, Outlet } from 'react-router-dom';

const navigation = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/doctors', label: 'Doctors', icon: Stethoscope },
  { to: '/patients', label: 'Patients', icon: Users },
  { to: '/appointments', label: 'Appointments', icon: CalendarDays },
  { to: '/audit', label: 'Audit History', icon: ClipboardList },
];

export function AppShell() {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand__mark" aria-hidden="true">
            <Activity size={22} />
          </span>
          <div>
            <strong>Super Dentist</strong>
            <span>Clinic operations</span>
          </div>
        </div>
        <nav className="primary-nav" aria-label="Primary navigation">
          {navigation.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) => (isActive ? 'nav-link nav-link--active' : 'nav-link')}
            >
              <Icon aria-hidden="true" size={18} />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>
        <div className="sidebar__status">
          <span aria-hidden="true" />
          Read-only web client
        </div>
      </aside>
      <div className="app-content">
        <header className="mobile-header">
          <div className="brand brand--mobile">
            <span className="brand__mark" aria-hidden="true">
              <Activity size={20} />
            </span>
            <strong>Super Dentist</strong>
          </div>
        </header>
        <main id="main-content" className="main-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
