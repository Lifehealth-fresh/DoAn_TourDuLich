import { useEffect, useState } from 'react';
import * as api from './api';
import { Notice } from './components';

const rows = (response) => Array.isArray(response.data) ? response.data : response.data.items || [];
const empty = () => ({ tenDiaDanh: '', diaChi: '', maKhuVuc: '', mota: '' });

export default function SightseeingManagement() {
  const [items, setItems] = useState([]);
  const [regions, setRegions] = useState([]);
  const [form, setForm] = useState(empty);
  const [selected, setSelected] = useState('');
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
  const save = (event) => {
    event.preventDefault();
    if (busy) return;
    setBusy(true); setError(''); setMessage('');
    const data = { ...form, tenDiaDanh: form.tenDiaDanh.trim(), diaChi: form.diaChi.trim(), maKhuVuc: form.maKhuVuc || null, mota: form.mota.trim() };
    const run = selected ? api.updateSightseeing(selected, data) : api.createSightseeing(data);
    run.then(async (response) => {
      setMessage(selected ? 'Đã cập nhật điểm tham quan.' : 'Đã thêm điểm tham quan.');
      setSelected(selected || response.data.maDthamQuan);
      await load();
    }).catch((e) => setError(api.errorMessage(e, 'Không lưu được điểm tham quan.'))).finally(() => setBusy(false));
  };
  const remove = (id) => {
    if (!window.confirm('Xóa điểm này? Không xóa được nếu đang gắn lịch trình.')) return;
    setBusy(true); setError(''); setMessage('');
    api.deleteSightseeing(id).then(async () => {
      setMessage('Đã xóa điểm tham quan.');
      if (selected === id) { setSelected(''); setForm(empty()); }
      await load();
    }).catch((e) => setError(api.errorMessage(e, 'Không xóa được điểm tham quan.'))).finally(() => setBusy(false));
  };

  return <div>
    <h1>Điểm tham quan</h1>
    <Notice error={error} />{message && <div className="notice ok">{message}</div>}
    <button disabled={busy} onClick={() => { setSelected(''); setForm(empty()); }}>Thêm mới</button>
    <div className="table">
      {items.map((item) => <div className="row" key={item.maDthamQuan}>
        <b>{item.tenDiaDanh}</b><span>{item.tenKhuVuc || item.maKhuVuc || '—'}</span><span>{item.diaChi || '—'}</span>
        <div className="inline">
          <button disabled={busy} onClick={() => { setSelected(item.maDthamQuan); setForm({ tenDiaDanh: item.tenDiaDanh || '', diaChi: item.diaChi || '', maKhuVuc: item.maKhuVuc || '', mota: item.mota || '' }); }}>Sửa</button>
          <button className="danger" disabled={busy} onClick={() => remove(item.maDthamQuan)}>Xóa</button>
        </div>
      </div>)}
      {!items.length && !busy && <p>Chưa có điểm tham quan.</p>}
    </div>
    <form className="panel" onSubmit={save} style={{ marginTop: 24 }}>
      <h2>{selected ? 'Sửa điểm' : 'Thêm điểm'}</h2>
      <label>Tên địa danh<input required maxLength={100} value={form.tenDiaDanh} onChange={field('tenDiaDanh')} /></label>
      <label>Địa chỉ<input maxLength={100} value={form.diaChi} onChange={field('diaChi')} /></label>
      <label>Khu vực<select required value={form.maKhuVuc} onChange={field('maKhuVuc')}>
        <option value="">— chọn —</option>
        {regions.map((region) => <option key={region.maKhuVuc} value={region.maKhuVuc}>{region.tenKhuVuc}</option>)}
      </select></label>
      <label>Mô tả<textarea rows={3} value={form.mota} onChange={field('mota')} /></label>
      <button disabled={busy} style={{ marginTop: 12 }}>{selected ? 'Lưu điểm' : 'Thêm điểm'}</button>
    </form>
  </div>;
}
