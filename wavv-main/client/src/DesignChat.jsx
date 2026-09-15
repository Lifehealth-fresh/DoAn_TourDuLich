import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import * as api from './api';

const tomorrow = () => {
  const day = new Date();
  day.setDate(day.getDate() + 1);
  return `${day.getFullYear()}-${String(day.getMonth() + 1).padStart(2, '0')}-${String(day.getDate()).padStart(2, '0')}`;
};

function foldVi(value) {
  return String(value || '')
    .normalize('NFD')
    .replace(/đ/gi, 'd')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .trim();
}

function ProvincePicker({ onPick }) {
  const [query, setQuery] = useState('');
  const [rows, setRows] = useState([]);
  useEffect(() => {
    const handle = setTimeout(() => {
      api.provinces(query).then((response) => setRows(response.data || [])).catch(() => setRows([]));
    }, 200);
    return () => clearTimeout(handle);
  }, [query]);
  const needle = foldVi(query);
  const shown = (needle
    ? rows.filter((row) => foldVi(`${row.tenTinh} ${row.tenKhuVuc}`).includes(needle))
    : rows).slice(0, needle ? 63 : 14);
  return (
    <div className="chat-widget">
      <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Gõ Nha Trang, Da Lat, Sai Gon… (63 tỉnh)" />
      <div className="chat-chips">
        {shown.map((row) => (
          <button type="button" key={row.maTinh} onClick={() => onPick(row)}>{row.tenTinh}</button>
        ))}
      </div>
      {!shown.length && <p className="muted">Không khớp tỉnh. Thử gõ không dấu: nha trang, hoi an, can tho.</p>}
    </div>
  );
}

export default function DesignChat() {
  const [turn, setTurn] = useState(null);
  const [text, setText] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const bottom = useRef(null);
  const widget = turn?.widget;
  const done = turn?.trangThai === 'DaSinhDeXuat';

  useEffect(() => { bottom.current?.scrollIntoView({ behavior: 'smooth' }); }, [turn]);

  const send = async (payload) => {
    if (busy) return;
    setBusy(true); setError('');
    try {
      const response = await api.designChat({ maHoiThoai: turn?.maHoiThoai, ...payload });
      setTurn(response.data);
      setText('');
    } catch (err) {
      setError(api.errorMessage(err, 'Không gửi được tin nhắn. Chạy script 023 trên database rồi thử lại.'));
    } finally { setBusy(false); }
  };

  useEffect(() => { send({}); }, []);

  return (
    <div className="design-chat">
      <header className="chat-head">
        <div>
          <span className="stamp">Trợ lý ANAM</span>
          <h2>Thiết kế chuyến đi</h2>
          <p>Chỉ ghép điểm, khách sạn, nhà hàng đang có trong hệ thống. Bạn chọn tỉnh — không gõ tự do.</p>
        </div>
        {turn?.maHoiThoai && <button className="outline-button" disabled={busy} onClick={() => send({ message: 'làm lại' })}>Làm lại</button>}
      </header>
      <div className="chat-thread" role="log">
        {(turn?.messages || []).map((item) => (
          <article key={item.maTinNhan} className={`chat-bubble ${item.vaiTro === 'Khach' ? 'me' : 'bot'}`}>
            <p>{item.noiDung}</p>
          </article>
        ))}
        {!turn && <p role="status">Đang mở trợ lý…</p>}
        <div ref={bottom} />
      </div>
      {error && <div className="form-error" role="alert">{error}</div>}
      {widget?.type === 'province' && !done && <ProvincePicker onPick={(row) => send({ maTinh: row.maTinh, message: row.tenTinh })} />}
      {widget?.type === 'datetime' && !done && (
        <form className="chat-widget" onSubmit={(event) => { event.preventDefault(); const data = new FormData(event.target); send({ date: data.get('date'), time: data.get('time'), message: `${data.get('date')} ${data.get('time')}` }); }}>
          <input type="date" name="date" required min={tomorrow()} defaultValue={tomorrow()} />
          <input type="time" name="time" required defaultValue={widget.role === 'return' ? '20:00' : '08:00'} />
          <button className="primary-button" disabled={busy}>Gửi ngày giờ</button>
        </form>
      )}
      {widget?.type === 'people' && !done && (
        <form className="chat-widget" onSubmit={(event) => { event.preventDefault(); const data = new FormData(event.target); send({ number: Number(data.get('nl')), message: `${data.get('nl')} người lớn, ${data.get('te') || 0} trẻ em` }); }}>
          <label>Người lớn <input type="number" name="nl" min="1" defaultValue="2" required /></label>
          <label>Trẻ em <input type="number" name="te" min="0" defaultValue="0" /></label>
          <button className="primary-button" disabled={busy}>Gửi số khách</button>
        </form>
      )}
      {widget?.type === 'number' && !done && widget.role === 'budget' && (
        <form className="chat-widget" onSubmit={(event) => { event.preventDefault(); const value = Number(new FormData(event.target).get('n')); send({ number: value, message: String(value) }); }}>
          <input type="number" name="n" min="1000000" step="100000" placeholder="Ví dụ 15000000" required />
          <button className="primary-button" disabled={busy}>Gửi ngân sách</button>
        </form>
      )}
      {widget?.type === 'number' && !done && widget.role === 'events' && (
        <div className="chat-chips">
          {[1, 2, 3, 4].map((n) => <button type="button" key={n} disabled={busy} onClick={() => send({ number: n, message: `${n} hoạt động` })}>{n} / ngày</button>)}
        </div>
      )}
      {widget?.type === 'purpose' && !done && (
        <div className="chat-chips">
          {(widget.options || []).map((item) => <button type="button" key={item} disabled={busy} onClick={() => send({ mucDich: item, message: item })}>{item}</button>)}
        </div>
      )}
      {widget?.type === 'confirm' && !done && (
        <button className="primary-button" disabled={busy} onClick={() => send({ confirm: true, message: 'đồng ý sinh đề xuất' })}>Xác nhận và sinh 3 lịch</button>
      )}
      {widget?.type === 'proposals' && widget.maYeuCau && (
        <Link className="primary-button" to={`/tu-thiet-ke/${encodeURIComponent(widget.maYeuCau)}`}>Xem 3 phương án đã ghép</Link>
      )}
      {!done && (
        <form className="chat-compose" onSubmit={(event) => { event.preventDefault(); if (text.trim()) send({ message: text.trim() }); }}>
          <input value={text} disabled={busy} onChange={(event) => setText(event.target.value)} placeholder="Nhắn với trợ lý…" />
          <button className="primary-button" disabled={busy || !text.trim()}>{busy ? '…' : 'Gửi'}</button>
        </form>
      )}
    </div>
  );
}
