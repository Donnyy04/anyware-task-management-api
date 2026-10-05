import { useEffect, useState } from 'react';
import { Link, Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { useAuth } from './auth';
import { AdminUsers, Login, NotFound, Register, TaskDetail, TaskList } from './pages';

function Shell() {
  const { user, logout, message, clearMessage } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);
  const location = useLocation();
  const client = useQueryClient();
  useEffect(() => { setMenuOpen(false); }, [location.pathname]);
  return <div className="app-shell"><header className="topbar"><Link className="brand" to="/tasks"><span className="brand-mark">A</span><span>anyware<span className="brand-light"> / tasks</span></span></Link><button className="menu-toggle" onClick={() => setMenuOpen(!menuOpen)} aria-label="Toggle navigation">☰</button><nav className={menuOpen ? 'nav open' : 'nav'}><Link to="/tasks">My tasks</Link>{user?.role === 'Admin' && <Link to="/admin/users">Users</Link>}<div className="profile"><span className="avatar">{user?.name.slice(0, 1).toUpperCase()}</span><span className="profile-name">{user?.name}<small>{user?.role}</small></span></div><button className="button button-quiet" onClick={() => { client.clear(); logout(); }}>Log out</button></nav></header>{message && <div className="session-banner"><span>{message}</span><button onClick={clearMessage} aria-label="Dismiss">×</button></div>}<main className="main"><Outlet /></main><footer>ANYWARE TASKS <span>•</span> Built for focused work</footer></div>;
}
function Protected() { const { user, loading } = useAuth(); const location = useLocation(); if (loading) return <div className="center-state"><span className="spinner"/>Restoring your session…</div>; return user ? <Shell /> : <Navigate to="/login" replace state={{ from: location.pathname }} />; }
function AdminOnly() { const { user } = useAuth(); return user?.role === 'Admin' ? <Outlet /> : <section className="panel state-panel"><span className="state-icon">⌑</span><h2>Access denied</h2><p>This area is available to administrators only.</p><Link className="button button-primary" to="/tasks">Back to my tasks</Link></section>; }
export default function App() { const { user, loading } = useAuth(); if (loading) return <div className="center-state"><span className="spinner"/>Loading Anyware…</div>; return <Routes><Route path="/login" element={user ? <Navigate to="/tasks" replace /> : <Login />} /><Route path="/register" element={user ? <Navigate to="/tasks" replace /> : <Register />} /><Route element={<Protected />}><Route path="/tasks" element={<TaskList />} /><Route path="/tasks/:id" element={<TaskDetail />} /><Route element={<AdminOnly />}><Route path="/admin/users" element={<AdminUsers />} /></Route></Route><Route path="*" element={<NotFound />} /></Routes>; }
