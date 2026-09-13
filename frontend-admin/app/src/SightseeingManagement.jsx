import { useEffect, useState } from 'react';
import * as api from './api';
import { ExpandRecord, Notice } from './components';

const rows = (response) => Array.isArray(response.data) ? response.data : response.data.items || [];
const empty = () => ({ tenDiaDanh: '', diaChi: '', maKhuVuc: '', mota: '' });
const fill = (item) => ({
  tenDiaDanh: item.tenDiaDanh || '', diaChi: item.diaChi || '', maKhuVuc: item.maKhuVuc || '', mota: item.mota || '',
});

export default function SightseeingManagement() {
  const [items, setItems] = useState([]);
  const [regions, setRegions] = useState([]);
  const [form, setForm] = useState(empty);
  const [selected, setSelected] = useState('');
  const [creating, setCreating] = useState(false);
  const [busy, setBusy] = useState(true);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const load = async () => {
    const [places, khu] = await Promise.all([api.sightseeingPlaces(), api.regions()]);
    setItems(rows(places)); setRegions(rows(khu));
  };
  useEffect(() => {
    load().catch((e) => setError(api.errorMessage(e, 'Không tải được điểm tham quan.'))).finally(() => setBusy(false));
  }, []);
  const field = (key) => (event) => setForm((current) => ({ ...current, [key]: event.target.value }));
  const open = (item) => { setCreating(false); setSelected(item.maDthamQuan); setForm(fill(item)); };
  const toggle = (item) => {
    if (selected === item.maDthamQuan) { setSelected(''); setCreating(false); return; }
    open(item);
  };
  const save = (event) => {
    event.preventDefault();
    if (busy) return;
    setBusy(true); setError(''); setMessage('');
    const data = { ...form, tenDiaDanh: form.tenDiaDanh.trim(), diaChi: form.diaChi.trim(), maKhuVuc: form.maKhuVuc || null, mota: form.mota.trim() };
    const run = selected ? api.updateSightseeing(selected, data) : api.createSightseeing(data);
    run.then(async (response) => {
      setMessage(selected ? 'Đã cập nhật điểm tham quan.' : 'Đã thêm điểm tham quan.');
      setCreating(false);
      setSelected(selected || response.data.maDthamQuan);
      await load();
    }).catch((e) => setError(api.errorMessage(e, 'Không lưu được điểm tham quan.'))).finally(() => setBusy(false));
  };
  const remove = (id) => {
    if (!window.confirm('Xóa điểm này? Không xóa được nếu đang gắn lịch trình.')) return;
    setBusy(true); setError(''); setMessage('');
    api.deleteSightseeing(id).then(async () => {
      setMessage('Đã xóa điểm tham quan.');
      if (selected === id) { setSelected(''); setCreating(false); setForm(empty()); }
      await load();
    }).catch((e) => setError(api.errorMessage(e, 'Không xóa được điểm tham quan.'))).finally(() => setBusy(false));
  };
  const editor = (
    <form className="panel" style={{ margin: 0, boxShadow: 'none', border: 0, padding: 0 }} onSubmit={save}>
      <h2>{selected ? 'Sửa điểm' : 'Thêm điểm mới'}</h2>
      <label>Tên địa danh<input required maxLength={100} value={form.tenDiaDanh} onChange={field('tenDiaDanh')} /></label>
      <label>Địa chỉ<input maxLength={100} value={form.diaChi} onChange={field('diaChi')} /></label>
      <label>Khu vực<select required value={form.maKhuVuc} onChange={field('maKhuVuc')}>
        <option value="">— chọn —</option>
        {regions.map((region) => <option key={region.maKhuVuc} value={region.maKhuVuc}>{region.tenKhuVuc}</option>)}
      </select></label>
      <label>Mô tả<textarea rows={3} value={form.mota} onChange={field('mota')} /></label>
      <button disabled={busy} style={{ marginTop: 12 }}>{selected ? 'Lưu điểm' : 'Thêm điểm'}</button>
    </form>
  );

  return <div>
    <h1>Điểm tham quan</h1>
    <Notice error={error} />{message && <div className="notice ok">{message}</div>}
    <button disabled={busy} onClick={() => { setSelected(''); setCreating(true); setForm(empty()); requestAnimationFrame(() => document.getElementById('sight-create')?.scrollIntoView({ block: 'nearest', behavior: 'smooth' })); }}>Thêm mới</button>
    {creating && <div id="sight-create" className="create-slot record open"><div className="record-body">{editor}</div></div>}
    <div className="table">
      {items.map((item) => <ExpandRecord key={item.maDthamQuan} open={selected === item.maDthamQuan} summary={
        <div className="row" style={{ cursor: busy ? 'wait' : 'pointer' }} onClick={() => { if (!busy) toggle(item); }}>
          <b>{item.tenDiaDanh}</b><span>{item.tenKhuVuc || item.maKhuVuc || '—'}</span><span>{item.diaChi || '—'}</span>
          <div className="inline" style={{ margin: 0 }}>
            <button disabled={busy} onClick={(event) => { event.stopPropagation(); open(item); }}>Sửa</button>
            <button className="danger" disabled={busy} onClick={(event) => { event.stopPropagation(); remove(item.maDthamQuan); }}>Xóa</button>
          </div>
        </div>
      }>{editor}</ExpandRecord>)}
      {!items.length && !busy && <p>Chưa có điểm tham quan.</p>}
    </div>
  </div>;
}
