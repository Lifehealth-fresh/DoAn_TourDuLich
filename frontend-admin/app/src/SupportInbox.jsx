import { useEffect, useRef, useState } from 'react';
import * as api from './api';
import { Notice } from './components';
import { useAuth } from './context';

export default function SupportInbox() {
  const { can } = useAuth();
  const canReply = can('HoTro', 'Them');
  const [items, setItems] = useState([]);
  const [open, setOpen] = useState('');
  const [thread, setThread] = useState(null);
  const [text, setText] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const bottom = useRef(null);

  const loadList = () => api.supportInbox().then((r) => setItems(r.data || [])).catch((e) => setError(api.errorMessage(e, 'Không tải được hộp thư.')));

  useEffect(() => { loadList(); const t = setInterval(loadList, 8000); return () => clearInterval(t); }, []);

  const openThread = async (id) => {
    setOpen(id); setBusy(true); setError('');
    try {
      const r = await api.supportThread(id);
      setThread(r.data);
    } catch (e) {
      setThread(null);
      setError(api.errorMessage(e, 'Không mở được cuộc chat.'));
    } finally { setBusy(false); }
  };

  useEffect(() => {
    if (!open) return undefined;
    const t = setInterval(() => {
      api.supportThread(open).then((r) => setThread(r.data)).catch(() => {});
    }, 5000);
    return () => clearInterval(t);
  }, [open]);

  useEffect(() => { bottom.current?.scrollIntoView({ behavior: 'smooth' }); }, [thread?.tinNhans?.length]);

  const send = async (event) => {
    event.preventDefault();
    if (!open || !text.trim() || !canReply) return;
    setBusy(true);
    try {
      const r = await api.supportReply(open, text.trim());
      setThread(r.data);
      setText('');
      await loadList();
    } catch (e) {
      setError(api.errorMessage(e, 'Không gửi được tin nhắn.'));
    } finally { setBusy(false); }
  };

  return (
    <div>
      <h1>Hỗ trợ khách hàng</h1>
      <p className="muted">Chat trực tiếp với khách. Trả lời ở khung bên phải.</p>
      <Notice error={error} />
      <div className="chat-inbox">
        <div className="chat-list">
          {items.length ? items.map((row) => (
            <button key={row.maCuoc} type="button" className={open === row.maCuoc ? 'on' : ''} onClick={() => openThread(row.maCuoc)}>
              <b>{row.khach?.hoTen}</b>
              <span className="muted">{row.khach?.soDienThoai} {row.soChuaDoc ? `· ${row.soChuaDoc} chưa đọc` : ''}</span>
            </button>
          )) : <p className="muted" style={{ padding: 16 }}>Chưa có cuộc chat.</p>}
        </div>
        <div className="chat-thread-box">
          {thread ? (
            <>
              <header style={{ padding: '12px 16px', borderBottom: '2px solid #1c1612' }}>
                <strong>{thread.khach?.hoTen}</strong>
                <span className="muted"> · {thread.khach?.soDienThoai}</span>
              </header>
              <div className="chat-log">
                {(thread.tinNhans || []).map((m) => (
                  <article key={m.maTinNhan} className={`bubble ${m.vaiTro === 'NhanVien' ? 'me' : 'them'}`}>
                    <p>{m.noiDung}</p>
                    <small>{m.thoiGian ? new Date(m.thoiGian).toLocaleString('vi-VN') : ''}</small>
                  </article>
                ))}
                <div ref={bottom} />
              </div>
              {canReply ? (
                <form className="chat-compose" onSubmit={send}>
                  <input value={text} onChange={(e) => setText(e.target.value)} placeholder="Trả lời khách…" disabled={busy} />
                  <button type="submit" disabled={busy || !text.trim()}>Gửi</button>
                </form>
              ) : <p className="muted" style={{ padding: 12 }}>Bạn không có quyền trả lời.</p>}
            </>
          ) : <p className="muted" style={{ padding: 24 }}>Chọn một cuộc chat.</p>}
        </div>
      </div>
    </div>
  );
}
