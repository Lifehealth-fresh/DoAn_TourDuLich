import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import * as api from './api';
import { ExpandRecord, Notice } from './components';
import { useAuth } from './context';

const emptyDoc = () => ({ loaiGiayTo: 'CCCD', soTrenGiayTo: '', ngayCap: '', ngayHetHan: '', noiCap: '' });
const dateText = (value) => (value ? new Date(value).toLocaleDateString('vi-VN') : '—');
const isoDay = (value) => (value ? String(value).slice(0, 10) : '');

export default function CustomersManagement() {
  const { can } = useAuth();
  const canThem = can('KhachHang', 'Them');
  const canSua = can('KhachHang', 'Sua');
  const canXoa = can('KhachHang', 'Xoa');
  const [params, setParams] = useSearchParams();
  const [q, setQ] = useState(params.get('q') || '');
  const [items, setItems] = useState([]);
  const [open, setOpen] = useState(params.get('id') || '');
  const [detail, setDetail] = useState(null);
  const [docForm, setDocForm] = useState(emptyDoc);
  const [editingDoc, setEditingDoc] = useState('');
  const [frontFile, setFrontFile] = useState(null);
  const [backFile, setBackFile] = useState(null);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);

  const load = (query) => {
    setBusy(true); setError('');
    api.customers({ q: query || undefined, pageSize: 50 })
      .then((r) => setItems(r.data?.items || []))
      .catch((e) => setError(api.errorMessage(e, 'Không tải được khách hàng.')))
      .finally(() => setBusy(false));
  };

  useEffect(() => { load(q); }, []);

  useEffect(() => {
    const id = params.get('id');
    if (id) openCustomer(id);
  }, []);

  const openCustomer = async (id) => {
    if (open === id && detail?.hoSo?.maKhachHang === id) { setOpen(''); setDetail(null); return; }
    setOpen(id);
    setBusy(true); setError('');
    try {
      const r = await api.customerDetail(id);
      setDetail(r.data);
      setEditingDoc(''); setDocForm(emptyDoc()); setFrontFile(null); setBackFile(null);
      setParams({ id }, { replace: true });
    } catch (e) {
      setDetail(null);
      setError(api.errorMessage(e, 'Không tải được hồ sơ khách.'));
    } finally { setBusy(false); }
  };

  const uploadSides = async (maKhachHang, maGiayTo) => {
    if (frontFile) await api.uploadCustomerDocImage(maKhachHang, maGiayTo, frontFile, 'Truoc');
    if (backFile) await api.uploadCustomerDocImage(maKhachHang, maGiayTo, backFile, 'Sau');
  };

  const saveDoc = async (event) => {
    event.preventDefault();
    const id = detail?.hoSo?.maKhachHang;
    if (!id) return;
    setBusy(true); setError(''); setMessage('');
    try {
      let row;
      if (editingDoc) {
        const r = await api.staffUpdateDocument(id, editingDoc, docForm);
        row = r.data;
      } else {
        const r = await api.staffAddDocument(id, docForm);
        row = r.data;
      }
      await uploadSides(id, row.maGiayTo);
      setMessage(editingDoc ? 'Đã cập nhật giấy tờ.' : 'Đã thêm giấy tờ.');
      const refreshed = await api.customerDetail(id);
      setDetail(refreshed.data);
      setEditingDoc(''); setDocForm(emptyDoc()); setFrontFile(null); setBackFile(null);
    } catch (e) {
      setError(api.errorMessage(e, 'Không lưu được giấy tờ.'));
    } finally { setBusy(false); }
  };

  const removeDoc = async (doc) => {
    const id = detail?.hoSo?.maKhachHang;
    if (!id || !window.confirm(`Xóa ${doc.loaiGiayTo} ${doc.soTrenGiayTo}?`)) return;
    setBusy(true);
    try {
      await api.staffDeleteDocument(id, doc.maGiayTo);
      const refreshed = await api.customerDetail(id);
      setDetail(refreshed.data);
      setMessage('Đã xóa giấy tờ.');
    } catch (e) {
      setError(api.errorMessage(e, 'Không xóa được giấy tờ.'));
    } finally { setBusy(false); }
  };

  const hoSo = detail?.hoSo;
  const docs = hoSo?.giayTos || [];
  const trips = detail?.chuyenDi || [];

  return (
    <>
      <h1>Quản lý khách hàng</h1>
      <p className="muted">Xem hồ sơ và chuyến đi. Thêm/sửa/xóa giấy tờ và ảnh CCCD. Không đổi số điện thoại hay mật khẩu tài khoản.</p>
      <Notice error={error} />
      {message && <div className="notice ok" role="status">{message}</div>}
      <form className="toolbar" onSubmit={(e) => { e.preventDefault(); load(q.trim()); }} style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 16 }}>
        <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Tên, số điện thoại, mã khách, email" style={{ minWidth: 260, flex: 1 }} />
        <button className="primary" disabled={busy} type="submit">{busy ? 'Đang tìm…' : 'Tìm'}</button>
      </form>
      {items.length === 0 ? <p className="muted">Chưa có khách khớp.</p> : items.map((row) => (
        <ExpandRecord key={row.maKhachHang} open={open === row.maKhachHang} onClose={() => openCustomer(row.maKhachHang)} summary={(
          <button type="button" className="row" onClick={() => openCustomer(row.maKhachHang)}>
            <b>{row.maKhachHang}</b>
            <span>{[row.ho, row.ten].filter(Boolean).join(' ')}</span>
            <span>{row.soDienThoai || '—'}</span>
            <span>{row.soChuyenDi || 0} chuyến · {row.soGiayTo || 0} giấy tờ</span>
          </button>
        )}>
          {hoSo?.maKhachHang === row.maKhachHang ? (
            <div className="review-staff-list">
              <section className="panel" style={{ boxShadow: 'none' }}>
                <h2>Hồ sơ</h2>
                <p><b>{hoSo.ho} {hoSo.ten}</b> · {hoSo.danhXung || '—'} · {hoSo.gioiTinh || '—'}</p>
                <p>Sinh {dateText(hoSo.ngaySinh)} · Quốc tịch {hoSo.quocTich || '—'}</p>
                <p>Email {hoSo.email || '—'} · SĐT {hoSo.soDienThoai || '—'} (tài khoản, không sửa tại đây)</p>
              </section>
              <section>
                <h2>Giấy tờ nội bộ</h2>
                {docs.length ? docs.map((doc) => (
                  <article className="review-staff-card" key={doc.maGiayTo}>
                    <header>
                      <strong>{doc.loaiGiayTo}</strong>
                      <span>{doc.soTrenGiayTo}</span>
                      <span>Cấp {dateText(doc.ngayCap)} · {doc.noiCap}</span>
                    </header>
                    <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
                      {doc.anhMatTruoc && <a href={doc.anhMatTruoc} target="_blank" rel="noreferrer"><img src={doc.anhMatTruoc} alt="Mặt trước" style={{ height: 88, border: '2px solid #1c1612' }} /></a>}
                      {doc.anhMatSau && <a href={doc.anhMatSau} target="_blank" rel="noreferrer"><img src={doc.anhMatSau} alt="Mặt sau" style={{ height: 88, border: '2px solid #1c1612' }} /></a>}
                    </div>
                    <div className="inline" style={{ marginTop: 8 }}>
                      {canSua && <button type="button" onClick={() => { setEditingDoc(doc.maGiayTo); setDocForm({ loaiGiayTo: doc.loaiGiayTo, soTrenGiayTo: doc.soTrenGiayTo, ngayCap: isoDay(doc.ngayCap), ngayHetHan: isoDay(doc.ngayHetHan), noiCap: doc.noiCap }); }}>Sửa</button>}
                      {canXoa && <button type="button" className="danger" onClick={() => removeDoc(doc)}>Xóa</button>}
                    </div>
                  </article>
                )) : <p className="muted">Chưa có giấy tờ.</p>}
                {(canThem || (canSua && editingDoc)) && (
                  <form className="panel" style={{ boxShadow: 'none' }} onSubmit={saveDoc}>
                    <h3>{editingDoc ? 'Sửa giấy tờ' : 'Thêm giấy tờ'}</h3>
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit,minmax(160px,1fr))', gap: 10 }}>
                      <label>Loại<select value={docForm.loaiGiayTo} onChange={(e) => setDocForm({ ...docForm, loaiGiayTo: e.target.value })}>
                        <option>CCCD</option><option>Hộ chiếu</option><option>CMND</option>
                      </select></label>
                      <label>Số<input required value={docForm.soTrenGiayTo} onChange={(e) => setDocForm({ ...docForm, soTrenGiayTo: e.target.value })} /></label>
                      <label>Ngày cấp<input type="date" required value={docForm.ngayCap} onChange={(e) => setDocForm({ ...docForm, ngayCap: e.target.value })} /></label>
                      <label>Hết hạn<input type="date" required value={docForm.ngayHetHan} onChange={(e) => setDocForm({ ...docForm, ngayHetHan: e.target.value })} /></label>
                      <label>Nơi cấp<input required value={docForm.noiCap} onChange={(e) => setDocForm({ ...docForm, noiCap: e.target.value })} /></label>
                      <label>Ảnh mặt trước<input type="file" accept="image/jpeg,image/png,image/webp" onChange={(e) => setFrontFile(e.target.files?.[0] || null)} /></label>
                      <label>Ảnh mặt sau<input type="file" accept="image/jpeg,image/png,image/webp" onChange={(e) => setBackFile(e.target.files?.[0] || null)} /></label>
                    </div>
                    <div className="inline" style={{ marginTop: 12 }}>
                      <button type="submit" disabled={busy}>{busy ? 'Đang lưu…' : editingDoc ? 'Lưu giấy tờ' : 'Thêm giấy tờ'}</button>
                      {editingDoc && <button type="button" onClick={() => { setEditingDoc(''); setDocForm(emptyDoc()); }}>Hủy</button>}
                    </div>
                  </form>
                )}
              </section>
              <section>
                <h2>Chuyến đi đã đặt</h2>
                {trips.length ? trips.map((trip) => (
                  <article className="review-staff-card" key={trip.maBooking}>
                    <header>
                      <strong>{trip.tenTour}</strong>
                      <span>{trip.maBooking}</span>
                      <span>{dateText(trip.ngayDat)}</span>
                      <em className={'badge ' + trip.trangThai}>{trip.trangThai}</em>
                    </header>
                    <p>{trip.slnguoiLon || 0} người lớn · {trip.sltreEm || 0} trẻ em · {(trip.thanhTien || 0).toLocaleString('vi-VN')} đ</p>
                    <Link to="/booking">Mở quản lý booking</Link>
                    {' · '}
                    <Link to="/tours">Mở quản lý tour</Link>
                  </article>
                )) : <p className="muted">Khách chưa đặt chuyến nào.</p>}
              </section>
            </div>
          ) : <p className="muted">Đang mở…</p>}
        </ExpandRecord>
      ))}
    </>
  );
}
