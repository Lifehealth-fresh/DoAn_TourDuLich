import { useEffect, useRef } from 'react';
import { Link, Navigate, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from './context';

export function Protected() {
  return useAuth().isStaff ? <Outlet /> : <Navigate to="/dang-nhap" replace />;
}

export function Layout() {
  const { logout, navItems } = useAuth();
  const nav = useNavigate();
  return (
    <div className="shell">
      <aside>
        <Link className="brand" to="/">ANAM<small>Trang vận hành</small></Link>
        {navItems.map((item) => <Link key={item.to} to={item.to}>{item.label}</Link>)}
        <button onClick={() => { logout(); nav('/dang-nhap'); }}>Đăng xuất</button>
      </aside>
      <main><Outlet /></main>
    </div>
  );
}

export function ModuleGate({ module, children }) {
  const { can } = useAuth();
  if (can(module, 'Xem')) return children;
  return (
    <section className="panel">
      <h1>Bị hạn chế quyền</h1>
      <p>Bạn bị hạn chế quyền: không được sử dụng chức năng này.</p>
    </section>
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

export function BarList({ rows, empty = 'Chưa có dữ liệu.' }) {
  const max = Math.max(1, ...rows.map((row) => Number(row.value || 0)));
  if (!rows.length) return <p className="muted">{empty}</p>;
  return (
    <div className="chart-bars">
      {rows.map((row) => (
        <div className="chart-row" key={row.id || row.label}>
          <span className="chart-label">{row.label}</span>
          <div className="chart-track" title={row.hint || row.display}>
            <i style={{ width: `${Math.round((Number(row.value || 0) / max) * 100)}%`, background: row.color }} />
          </div>
          <b>{row.display}</b>
        </div>
      ))}
    </div>
  );
}

const MIX = {
  ChoXacNhan: '#ffe08a', DaXacNhan: '#7aa7d9', DaThanhToan: '#1e5c45',
  HoanThanh: '#163041', DaHuy: '#b33a1a', ChoHoanTien: '#c49a3c',
};

export function StatusMix({ items, labels = {} }) {
  const total = items.reduce((sum, item) => sum + Number(item.soLuong || 0), 0);
  if (!total) return <p className="muted">Chưa có booking để thống kê.</p>;
  let acc = 0;
  const stops = items.map((item) => {
    const start = acc;
    acc += (Number(item.soLuong || 0) / total) * 360;
    return `${MIX[item.trangThai] || '#c49a3c'} ${start}deg ${acc}deg`;
  });
  return (
    <div className="status-mix">
      <div className="donut" style={{ background: `conic-gradient(${stops.join(',')})` }} />
      <ul>
        {items.map((item) => (
          <li key={item.trangThai}>
            <i style={{ background: MIX[item.trangThai] || '#c49a3c' }} />
            {labels[item.trangThai] || item.trangThai}: {item.soLuong}
          </li>
        ))}
      </ul>
    </div>
  );
}
