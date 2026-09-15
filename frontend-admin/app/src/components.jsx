import { useEffect, useRef, useState } from 'react';
import { Link, Navigate, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from './context';

export function Protected() {
  return useAuth().isStaff ? <Outlet /> : <Navigate to="/dang-nhap" replace />;
}

export function Layout() {
  const { logout, navItems, profile, role } = useAuth();
  const nav = useNavigate();
  const fullName = [profile?.ho, profile?.ten].filter(Boolean).join(' ').trim();
  const title = fullName || profile?.soDienThoai || 'Nhân viên ANAM';
  return (
    <div className="shell">
      <aside>
        <Link className="brand" to="/">ANAM<small>Trang vận hành</small></Link>
        {navItems.map((item) => <Link key={item.to} to={item.to}>{item.label}</Link>)}
      </aside>
      <div className="workspace">
        <header className="topbar">
          <div className="topbar-user">
            <strong>{title}</strong>
            <span>{profile?.chucVu || role} · {profile?.soDienThoai || ''}</span>
          </div>
          <button type="button" className="topbar-logout" onClick={() => { logout(); nav('/dang-nhap'); }}>Đăng xuất</button>
        </header>
        <main><Outlet /></main>
      </div>
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

export function PanelClose({ onClose, label = 'Đóng' }) {
  if (!onClose) return null;
  return <button type="button" className="panel-close" aria-label={label} onClick={onClose}>×</button>;
}

export function ExpandRecord({ open, summary, children, onClose }) {
  const body = useRef(null);
  useEffect(() => {
    if (open) body.current?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
  }, [open]);
  return (
    <article className={'record' + (open ? ' open' : '')}>
      <div className="record-head">{summary}</div>
      {open ? <div className="record-body" ref={body}><PanelClose onClose={onClose} />{children}</div> : null}
    </article>
  );
}

export function OpenPanel({ id, children, onClose }) {
  return (
    <div id={id} className="create-slot record open">
      <div className="record-body"><PanelClose onClose={onClose} />{children}</div>
    </div>
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

export function LineChart({ points, empty = 'Chưa có dữ liệu.' }) {
  if (!points.length) return <p className="muted">{empty}</p>;
  const w = 320;
  const h = 120;
  const pad = 18;
  const values = points.map((p) => Number(p.diemTb || 0));
  const counts = points.map((p) => Number(p.soDanhGia || 0));
  const maxV = Math.max(5, ...values);
  const maxC = Math.max(1, ...counts);
  const x = (i) => pad + (i * (w - pad * 2)) / Math.max(1, points.length - 1);
  const yV = (v) => h - pad - (v / maxV) * (h - pad * 2);
  const yC = (v) => h - pad - (v / maxC) * (h - pad * 2);
  return (
    <div className="line-chart">
      <svg viewBox={`0 0 ${w} ${h}`} role="img" aria-label="Xu hướng đánh giá">
        <polyline fill="none" stroke="var(--navy)" strokeWidth="2.5" points={values.map((v, i) => `${x(i)},${yV(v)}`).join(' ')} />
        <polyline fill="none" stroke="var(--coral)" strokeWidth="2" strokeDasharray="4 3" points={counts.map((v, i) => `${x(i)},${yC(v)}`).join(' ')} />
        {points.map((p, i) => (
          <circle key={p.nhan} cx={x(i)} cy={yV(Number(p.diemTb || 0))} r="3" fill="var(--navy)" />
        ))}
      </svg>
      <ul className="line-legend">
        <li><i style={{ background: 'var(--navy)' }} /> Điểm trung bình</li>
        <li><i style={{ background: 'var(--coral)' }} /> Số review</li>
      </ul>
      <div className="line-labels">{points.map((p) => <span key={p.nhan}>{p.nhan}</span>)}</div>
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

export function SearchSelect({ id, value, onChange, options, emptyLabel = '— không chọn —', placeholder = 'Tìm theo từ khóa…' }) {
  const [query, setQuery] = useState('');
  const needle = query.trim().toLowerCase();
  const shown = (needle
    ? options.filter((item) => String(item.label || '').toLowerCase().includes(needle))
    : options).slice();
  if (value && !shown.some((item) => item.value === value)) {
    const current = options.find((item) => item.value === value);
    if (current) shown.unshift(current);
  }
  return (
    <div>
      <input id={id} style={{ width: '100%', marginBottom: 6 }} value={query} onChange={(event) => setQuery(event.target.value)} placeholder={placeholder} />
      <select style={{ width: '100%' }} value={value} onChange={(event) => onChange(event.target.value)}>
        <option value="">{emptyLabel}</option>
        {shown.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
      </select>
    </div>
  );
}
