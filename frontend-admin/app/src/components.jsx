import { Link, Navigate, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from './context';

export function Protected() {
  return useAuth().isStaff ? <Outlet /> : <Navigate to="/dang-nhap" replace />;
}

export function Layout() {
  const { logout } = useAuth();
  const nav = useNavigate();
  return (
    <div className="shell">
      <aside>
        <Link className="brand" to="/">ANAM Admin<small>Vận hành lữ hành</small></Link>
        <Link to="/">Tổng quan</Link>
        <Link to="/tours">Quản lý tour</Link>
        <Link to="/booking">Booking</Link>
        <button onClick={() => { logout(); nav('/dang-nhap'); }}>Đăng xuất</button>
      </aside>
      <main><Outlet /></main>
    </div>
  );
}

export const Notice = ({ error }) => (error ? <div className="notice error">{error}</div> : null);
