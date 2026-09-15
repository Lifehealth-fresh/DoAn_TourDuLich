import { useEffect, useState } from 'react';
import * as api from './api';
import { ExpandRecord, Notice, OpenPanel } from './components';
import { useAuth } from './context';

const types = [
  { id: 'LuuTru', label: 'Khách sạn / lưu trú' },
  { id: 'HoatDong', label: 'Hoạt động' },
  { id: 'AnUong', label: 'Ẩm thực' },
  { id: 'VanChuyen', label: 'Vận chuyển' },
];
const money = (n) => Number(n || 0).toLocaleString('vi-VN') + ' đ';
const rows = (response) => Array.isArray(response.data) ? response.data : response.data.items || [];
const emptyPartner = () => ({ tenDoiTac: '', loaiDoiTac: 'LuuTru', maKhuVuc: '', nguoiLienHe: '', soDienThoai: '', email: '', trangThai: 'HoatDong' });
const emptyRoom = () => ({ tenSanPham: 'Phòng Deluxe', donViTinh: 'dem', giaNiemYet: 1200000, mota: '', trangThai: 'HoatDong' });
const fill = (item) => ({
  tenDoiTac: item.tenDoiTac || '', loaiDoiTac: item.loaiDoiTac || 'LuuTru', maKhuVuc: item.maKhuVuc || '',
  nguoiLienHe: item.nguoiLienHe || '', soDienThoai: item.soDienThoai || '', email: item.email || '',
  trangThai: item.trangThai || 'HoatDong',
});

export default function PartnersManagement() {
  const { can } = useAuth();
  const canThem = can('DoiTac', 'Them');
  const canSua = can('DoiTac', 'Sua');
  const canXoa = can('DoiTac', 'Xoa');
  const [partners, setPartners] = useState([]);
  const [products, setProducts] = useState([]);
  const [regions, setRegions] = useState([]);
  const [form, setForm] = useState(emptyPartner);
  const [room, setRoom] = useState(emptyRoom);
  const [selected, setSelected] = useState('');
  const [creating, setCreating] = useState(false);
  const [busy, setBusy] = useState(true);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const selectedPartner = partners.find((item) => item.maDoiTac === selected);
  const load = async () => {
    const [list, khu, sp] = await Promise.all([api.partners(), api.regions(), api.partnerProducts()]);
    setPartners(rows(list)); setRegions(rows(khu)); setProducts(rows(sp));
  };
  useEffect(() => {
    load().catch((e) => setError(api.errorMessage(e, 'Không tải được đối tác.'))).finally(() => setBusy(false));
  }, []);
  const field = (key) => (event) => setForm((current) => ({ ...current, [key]: event.target.value }));
  const open = (item) => { setCreating(false); setSelected(item.maDoiTac); setForm(fill(item)); setRoom(emptyRoom()); };
  const toggle = (item) => {
    if (selected === item.maDoiTac) { setSelected(''); setCreating(false); return; }
    open(item);
  };
  const save = (event) => {
    event.preventDefault();
    if (busy) return;
    if (form.loaiDoiTac === 'LuuTru' && !form.maKhuVuc) { setError('Khách sạn phải gắn khu vực.'); return; }
    setBusy(true); setError(''); setMessage('');
    const data = { ...form, tenDoiTac: form.tenDoiTac.trim() };
    const run = selected ? api.updatePartner(selected, data) : api.createPartner(data);
    run.then(async (response) => {
      setMessage(selected ? 'Đã cập nhật đối tác.' : 'Đã thêm đối tác.');
      const id = selected || response.data.maDoiTac;
      setCreating(false); setSelected(id);
      await load();
    }).catch((e) => setError(api.errorMessage(e, 'Không lưu được đối tác.'))).finally(() => setBusy(false));
  };
  const remove = (id) => {
    if (!window.confirm('Xóa đối tác? Không xóa được nếu còn sản phẩm.')) return;
    setBusy(true); setError(''); setMessage('');
    api.deletePartner(id).then(async () => {
      setMessage('Đã xóa đối tác.');
      if (selected === id) { setSelected(''); setCreating(false); setForm(emptyPartner()); }
      await load();
    }).catch((e) => setError(api.errorMessage(e, 'Không xóa được đối tác.'))).finally(() => setBusy(false));
  };
  const saveRoom = (event) => {
    event.preventDefault();
    if (!selected || busy) return;
    setBusy(true); setError(''); setMessage('');
    api.createPartnerProduct({
      maDoiTac: selected, tenSanPham: room.tenSanPham.trim(), donViTinh: form.loaiDoiTac === 'LuuTru' ? 'dem' : (room.donViTinh || 'suat'),
      giaNiemYet: Number(room.giaNiemYet), mota: room.mota.trim(), trangThai: room.trangThai,
    }).then(async () => {
      setMessage('Đã thêm sản phẩm / loại phòng.');
      setRoom(emptyRoom());
      await load();
    }).catch((e) => setError(api.errorMessage(e, 'Không thêm được sản phẩm.'))).finally(() => setBusy(false));
  };
  const removeProduct = (id) => {
    if (!window.confirm('Xóa sản phẩm này?')) return;
    setBusy(true); setError(''); setMessage('');
    api.deletePartnerProduct(id).then(async () => { setMessage('Đã xóa sản phẩm.'); await load(); })
      .catch((e) => setError(api.errorMessage(e, 'Không xóa được sản phẩm.'))).finally(() => setBusy(false));
  };
  const rooms = products.filter((item) => item.maDoiTac === selected);
  const editor = (
    <form className="panel" style={{ margin: 0, boxShadow: 'none', border: 0, padding: 0 }} onSubmit={save}>
      <h2>{selected ? 'Sửa đối tác' : 'Thêm đối tác mới'}</h2>
      <label>Tên<input required maxLength={150} value={form.tenDoiTac} onChange={field('tenDoiTac')} /></label>
      <label>Loại<select value={form.loaiDoiTac} onChange={field('loaiDoiTac')}>
        {types.map((item) => <option key={item.id} value={item.id}>{item.label}</option>)}
      </select></label>
      <label>Khu vực<select required={form.loaiDoiTac === 'LuuTru'} value={form.maKhuVuc} onChange={field('maKhuVuc')}>
        <option value="">— chọn —</option>
        {regions.map((region) => <option key={region.maKhuVuc} value={region.maKhuVuc}>{region.tenKhuVuc}</option>)}
      </select></label>
      <label>Người liên hệ<input value={form.nguoiLienHe} onChange={field('nguoiLienHe')} /></label>
      <label>Điện thoại<input value={form.soDienThoai} onChange={field('soDienThoai')} /></label>
      <label>Email<input value={form.email} onChange={field('email')} /></label>
      <label>Trạng thái<select value={form.trangThai} onChange={field('trangThai')}>
        <option value="HoatDong">Hoạt động</option><option value="Ngung">Ngừng</option>
      </select></label>
      <button disabled={busy} style={{ marginTop: 12 }}>{selected ? 'Lưu đối tác' : 'Thêm đối tác'}</button>
    </form>
  );

  return <div>
    <h1>Đối tác và khách sạn</h1>
    <p className="muted">Khách sạn (LuuTru) bắt buộc chọn khu vực để tự thiết kế ghép đúng vùng.</p>
    <Notice error={error} />{message && <div className="notice ok">{message}</div>}
    {canThem && <button disabled={busy} onClick={() => { setSelected(''); setCreating(true); setForm(emptyPartner()); requestAnimationFrame(() => document.getElementById('partner-create')?.scrollIntoView({ block: 'nearest', behavior: 'smooth' })); }}>Thêm đối tác</button>}
    {creating && <OpenPanel id="partner-create" onClose={() => setCreating(false)}>{editor}</OpenPanel>}
    <div className="table">
      {partners.map((item) => <ExpandRecord key={item.maDoiTac} open={selected === item.maDoiTac} onClose={() => toggle(item)} summary={
        <div className="row" style={{ cursor: busy ? 'wait' : 'pointer' }} onClick={() => { if (!busy) toggle(item); }}>
          <b>{item.tenDoiTac}</b><span>{types.find((t) => t.id === item.loaiDoiTac)?.label || item.loaiDoiTac}</span>
          <span>{item.tenKhuVuc || item.maKhuVuc || '—'}</span>
          <div className="inline" style={{ margin: 0 }}>
            {canSua && <button disabled={busy} onClick={(event) => { event.stopPropagation(); open(item); }}>Sửa</button>}
            {canXoa && <button className="danger" disabled={busy} onClick={(event) => { event.stopPropagation(); remove(item.maDoiTac); }}>Xóa</button>}
          </div>
        </div>
      }>
        {editor}
        {selectedPartner && <section style={{ marginTop: 16 }}>
          <h2>Sản phẩm / loại phòng của {selectedPartner.tenDoiTac}</h2>
          <ul>{rooms.map((roomItem) => <li key={roomItem.maSanPham}>
            <b>{roomItem.tenSanPham}</b> · {roomItem.donViTinh || '—'} · {money(roomItem.giaNiemYet)}
            {canXoa && <button className="danger" disabled={busy} onClick={() => removeProduct(roomItem.maSanPham)}>Xóa</button>}
          </li>)}</ul>
          {canSua && <form onSubmit={saveRoom}>
            <h3>Thêm {selectedPartner.loaiDoiTac === 'LuuTru' ? 'loại phòng (giá 1 đêm)' : 'sản phẩm'}</h3>
            <label>Tên / loại phòng<input required value={room.tenSanPham} onChange={(e) => setRoom({ ...room, tenSanPham: e.target.value })} /></label>
            <label>Giá<input type="number" required min="1" value={room.giaNiemYet} onChange={(e) => setRoom({ ...room, giaNiemYet: e.target.value })} /></label>
            <label>Mô tả<textarea rows={2} value={room.mota} onChange={(e) => setRoom({ ...room, mota: e.target.value })} /></label>
            <button disabled={busy}>Thêm</button>
          </form>}
        </section>}
      </ExpandRecord>)}
    </div>
  </div>;
}
