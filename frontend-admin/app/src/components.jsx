import { useEffect, useRef } from 'react';
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
        <Link to="/uu-dai">Ưu đãi</Link>
        <Link to="/diem-tham-quan">Điểm tham quan</Link>
        <Link to="/doi-tac">Đối tác</Link>
        <Link to="/thiet-ke">Thiết kế</Link>
        <button onClick={() => { logout(); nav('/dang-nhap'); }}>Đăng xuất</button>
      </aside>
      <main><Outlet /></main>
    </div>
  );
}

export const Notice = ({ error }) => (error ? <div className="notice error">{error}</div> : null);

export function ExpandRecord({ open, summary, children }) {
  const body = useRef(null);
  useEffect(() => {
    if (open) body.current?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
  }, [open]);
  return (
    <article className={'record' + (open ? ' open' : '')}>
      <div className="record-head">{summary}</div>
      {open ? <div className="record-body" ref={body}>{children}</div> : null}
    </article>
  );
}
