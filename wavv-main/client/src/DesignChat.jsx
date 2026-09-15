import { useEffect, useRef, useState } from 'react';
import * as api from './api';

function foldVi(value) {
  return String(value || '')
    .normalize('NFD')
    .replace(/đ/gi, 'd')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .trim();
}

export function ProvinceField({ label, value, onChange }) {
  const [query, setQuery] = useState(value?.tenTinh || '');
  const [rows, setRows] = useState([]);
  const [open, setOpen] = useState(false);
  useEffect(() => { setQuery(value?.tenTinh || ''); }, [value?.maTinh, value?.tenTinh]);
  useEffect(() => {
    const handle = setTimeout(() => {
      api.provinces(query).then((response) => setRows(response.data || [])).catch(() => setRows([]));
    }, 180);
    return () => clearTimeout(handle);
  }, [query]);
  const needle = foldVi(query);
  const shown = (needle
    ? rows.filter((row) => foldVi(`${row.tenTinh} ${row.tenKhuVuc}`).includes(needle))
    : rows).slice(0, needle ? 20 : 12);
  return (
    <label className="province-field">
      {label}
      <input
        value={query}
        autoComplete="off"
        placeholder="Gõ Nha Trang, Da Lat, Ha Noi…"
        onFocus={() => setOpen(true)}
        onChange={(event) => { setQuery(event.target.value); setOpen(true); onChange(null); }}
      />
      {value?.tenTinh && <small>Đã chọn: {value.tenTinh}</small>}
      {open && (
        <div className="province-menu">
          {shown.map((row) => (
            <button type="button" key={row.maTinh} onClick={() => { onChange(row); setQuery(row.tenTinh); setOpen(false); }}>
              {row.tenTinh}
            </button>
          ))}
          {!shown.length && <p className="muted">Không khớp tỉnh. Gõ không dấu: nha trang, hoi an.</p>}
        </div>
      )}
    </label>
  );
}

export default function SupportChat() {
  const user = (() => { try { return JSON.parse(localStorage.getItem('wavv_user') || 'null'); } catch { return null; } })();
  const [open, setOpen] = useState(false);
  const [turn, setTurn] = useState(null);
  const [text, setText] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const bottom = useRef(null);

  useEffect(() => { bottom.current?.scrollIntoView({ behavior: 'smooth' }); }, [turn, open]);

  const send = async (payload) => {
    if (busy || !user) return;
    setBusy(true); setError('');
    try {
      const response = await api.supportChat({ maHoiThoai: turn?.maHoiThoai, ...payload });
      setTurn(response.data);
      setText('');
    } catch (err) {
      setError(api.errorMessage(err, 'Không gửi được. Chạy SQL 023 nếu chưa có bảng hội thoại.'));
    } finally { setBusy(false); }
  };

  useEffect(() => {
    if (open && user && !turn && !busy) send({});
  }, [open]);

  if (!user) return null;

  return (
    <div className={`support-dock${open ? ' open' : ''}`}>
      {open && (
        <div className="support-panel design-chat">
          <header className="chat-head">
            <div>
              <span className="stamp">Trợ lý ANAM</span>
              <h2>Hỗ trợ thao tác</h2>
              <p>Hỏi cách đặt tour, thanh toán, hủy, tự thiết kế… Không sinh lịch trong chat.</p>
            </div>
            <button className="text-button" type="button" onClick={() => setOpen(false)}>Đóng</button>
          </header>
          <div className="chat-thread" role="log">
            {(turn?.messages || []).map((item) => (
              <article key={item.maTinNhan} className={`chat-bubble ${item.vaiTro === 'Khach' ? 'me' : 'bot'}`}>
                <p>{item.noiDung}</p>
              </article>
            ))}
            <div ref={bottom} />
          </div>
          {error && <div className="form-error" role="alert">{error}</div>}
          <form className="chat-compose" onSubmit={(event) => { event.preventDefault(); if (text.trim()) send({ message: text.trim() }); }}>
            <input value={text} disabled={busy} onChange={(event) => setText(event.target.value)} placeholder="Hỏi trợ lý…" />
            <button className="primary-button" disabled={busy || !text.trim()}>{busy ? '…' : 'Gửi'}</button>
          </form>
        </div>
      )}
      <button type="button" className="support-fab" onClick={() => setOpen((value) => !value)}>
        {open ? '×' : 'Chat hỗ trợ'}
      </button>
    </div>
  );
}
