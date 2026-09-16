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
  const [thread, setThread] = useState(null);
  const [text, setText] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const bottom = useRef(null);

  const load = async () => {
    const response = await api.mySupport();
    setThread(response.data);
  };

  useEffect(() => {
    if (!open || !user) return undefined;
    let alive = true;
    load().catch((err) => { if (alive) setError(api.errorMessage(err, 'Không mở được chat với vận hành.')); });
    const timer = setInterval(() => { load().catch(() => {}); }, 4000);
    const stopHub = api.connectSupportHub(localStorage.getItem('wavv_token'), () => { load().catch(() => {}); });
    return () => { alive = false; clearInterval(timer); stopHub(); };
  }, [open]);

  useEffect(() => { bottom.current?.scrollIntoView({ behavior: 'smooth' }); }, [thread?.tinNhans?.length, open]);

  const send = async (event) => {
    event.preventDefault();
    if (busy || !user || !text.trim()) return;
    setBusy(true); setError('');
    try {
      const response = await api.sendSupport(text.trim());
      setThread(response.data);
      setText('');
    } catch (err) {
      setError(api.errorMessage(err, 'Không gửi được tin nhắn.'));
    } finally { setBusy(false); }
  };

  if (!user) return null;

  return (
    <div className={`support-dock${open ? ' open' : ''}`}>
      {open && (
        <div className="support-panel design-chat">
          <header className="chat-head">
            <div>
              <span className="stamp">ANAM hỗ trợ</span>
              <h2>Chat với vận hành</h2>
              <p>Nhân viên admin/sale sẽ trả lời trực tiếp.</p>
            </div>
            <button className="text-button" type="button" onClick={() => setOpen(false)}>Đóng</button>
          </header>
          <div className="chat-thread" role="log">
            {(thread?.tinNhans || []).map((item) => (
              <article key={item.maTinNhan} className={`chat-bubble ${item.vaiTro === 'KhachHang' ? 'me' : 'bot'}`}>
                <p>{item.noiDung}</p>
              </article>
            ))}
            {!thread?.tinNhans?.length && <p className="muted">Gửi tin nhắn để được hỗ trợ đặt tour, thanh toán, hồ sơ…</p>}
            <div ref={bottom} />
          </div>
          {error && <div className="form-error" role="alert">{error}</div>}
          <form className="chat-compose" onSubmit={send}>
            <input value={text} disabled={busy} onChange={(event) => setText(event.target.value)} placeholder="Nhắn cho ANAM…" />
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

