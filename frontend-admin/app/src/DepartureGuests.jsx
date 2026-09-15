import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import * as api from './api';
import { ExpandRecord, Notice } from './components';
import { useAuth } from './context';

const instant = (value) => value ? new Date(/(?:Z|[+-]\d\d:\d\d)$/i.test(value) ? value : value + 'Z') : null;
const stamp = (value) => instant(value)?.toLocaleString('vi-VN') || 'Chưa xác định';
const inputDate = (value) => {
  const date = instant(value);
  return date ? new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16) : '';
};
const day = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : '—';
const isoDay = (value) => value ? String(value).slice(0, 10) : '';
const STATUS = {
  ChoXacNhan: 'Chờ xác nhận', DaXacNhan: 'Đã xác nhận (cọc)', DaThanhToan: 'Đã thanh toán',
  HoanThanh: 'Hoàn thành', ChoHoanTien: 'Chờ hoàn tiền', DaHuy: 'Đã hủy',
};
const paidOf = (state) => /DaThanhToan|HoanThanh/.test(String(state || '').trim());
const emptyDeparture = (capacity) => ({ maKhoiHanh: '', ngayKhoiHanh: '', ngayKetThuc: '', diaDiem: '', soCho: capacity });
const emptyProfile = () => ({
  ho: '', ten: '', hoGiayTo: '', tenGiayTo: '', quocTich: '', danhXung: '', gioiTinh: '', ngaySinh: '', email: '',
});
const emptyDoc = () => ({ loaiGiayTo: 'CCCD', soTrenGiayTo: '', ngayCap: '', ngayHetHan: '', noiCap: '' });

export default function DepartureGuests({ tourId, defaultCapacity, disabled = false, hideCapacity = false }) {
  const { can } = useAuth();
  const canSuaTour = can('Tour', 'Sua');
  const canXoaTour = can('Tour', 'Xoa');
  const canSuaBooking = can('Booking', 'Sua');
  const canXoaBooking = can('Booking', 'Xoa');
  const [dates, setDates] = useState([]);
  const [roster, setRoster] = useState(null);
  const [profile, setProfile] = useState(null);
  const [form, setForm] = useState(emptyDeparture(defaultCapacity));
  const [profileForm, setProfileForm] = useState(emptyProfile());
  const [docForm, setDocForm] = useState(emptyDoc());
  const [editingDoc, setEditingDoc] = useState('');
  const [editing, setEditing] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [ok, setOk] = useState('');
  const [refresh, setRefresh] = useState(0);
  const [departureId, setDepartureId] = useState('');
  const [bookingId, setBookingId] = useState('');
  const [paidOnly, setPaidOnly] = useState(false);

  useEffect(() => {
    if (!editing) setForm((current) => ({ ...current, soCho: defaultCapacity }));
  }, [defaultCapacity, editing]);

  useEffect(() => {
    let active = true;
    setError('');
    setBusy(true);
    Promise.all([api.departures(tourId), departureId ? api.departureGuests(departureId) : Promise.resolve(null)])
      .then(([d, r]) => {
        if (!active) return;
        setDates(d.data || []);
        setRoster(r?.data || null);
        if (bookingId && r?.data && !r.data.bookings.some((b) => b.maBooking === bookingId)) {
          setBookingId('');
          setProfile(null);
        }
      })
      .catch((e) => { if (active) setError(api.errorMessage(e)); })
      .finally(() => { if (active) setBusy(false); });
    return () => { active = false; };
  }, [tourId, defaultCapacity, departureId, refresh]);

  useEffect(() => {
    let active = true;
    setProfile(null);
    if (!bookingId) return undefined;
    api.guestProfile(bookingId).then((r) => {
      if (!active) return;
      setProfile(r.data);
      setProfileForm({
        ho: r.data.ho || '', ten: r.data.ten || '', hoGiayTo: r.data.hoGiayTo || '', tenGiayTo: r.data.tenGiayTo || '',
        quocTich: r.data.quocTich || '', danhXung: r.data.danhXung || '', gioiTinh: r.data.gioiTinh || '',
        ngaySinh: isoDay(r.data.ngaySinh), email: r.data.email || '',
      });
      setDocForm(emptyDoc());
      setEditingDoc('');
    }).catch((e) => { if (active) setError(api.errorMessage(e)); });
    return () => { active = false; };
  }, [bookingId, refresh]);

  const blocked = busy || disabled;
  const guests = (roster?.bookings || []).filter((b) => !paidOnly || paidOf(b.trangThai));
  const saveDeparture = async (event) => {
    event.preventDefault();
    if (blocked) return;
    setBusy(true); setError(''); setOk('');
    try {
      const payload = {
        ...form, maTour: tourId, maKhoiHanh: editing || undefined, soCho: hideCapacity || form.soCho === '' ? null : Number(form.soCho),
        ngayKhoiHanh: form.ngayKhoiHanh ? new Date(form.ngayKhoiHanh).toISOString() : null,
        ngayKetThuc: form.ngayKetThuc ? new Date(form.ngayKetThuc).toISOString() : null,
      };
      if (payload.ngayKetThuc && payload.ngayKetThuc < payload.ngayKhoiHanh) throw Error('Ngày kết thúc phải sau ngày khởi hành.');
      if (editing) await api.updateDeparture(editing, payload);
      else await api.createDeparture(payload);
      setEditing(''); setForm(emptyDeparture(defaultCapacity)); setRefresh((x) => x + 1);
      setOk(editing ? 'Đã lưu lịch khởi hành.' : 'Đã thêm lịch khởi hành.');
    } catch (e) { setError(e.response ? api.errorMessage(e) : e.message); }
    finally { setBusy(false); }
  };
  const saveProfile = async (event) => {
    event.preventDefault();
    if (blocked || !bookingId) return;
    setBusy(true); setError(''); setOk('');
    try {
      const response = await api.updateGuestProfile(bookingId, {
        ho: profileForm.ho.trim(), ten: profileForm.ten.trim(), hoGiayTo: profileForm.hoGiayTo.trim() || null,
        tenGiayTo: profileForm.tenGiayTo.trim() || null, quocTich: profileForm.quocTich.trim() || null,
        danhXung: profileForm.danhXung.trim() || null, gioiTinh: profileForm.gioiTinh.trim() || null,
        ngaySinh: profileForm.ngaySinh || null, email: profileForm.email.trim() || null,
      });
      setProfile(response.data);
      setOk('Đã lưu hồ sơ khách. Không đổi trạng thái vé hay số chỗ.');
    } catch (e) { setError(api.errorMessage(e, 'Không lưu được hồ sơ khách.')); }
    finally { setBusy(false); }
  };
  const saveDoc = async (event) => {
    event.preventDefault();
    if (blocked || !bookingId) return;
    setBusy(true); setError(''); setOk('');
    try {
      const payload = {
        loaiGiayTo: docForm.loaiGiayTo, soTrenGiayTo: docForm.soTrenGiayTo.trim(),
        ngayCap: docForm.ngayCap, ngayHetHan: docForm.ngayHetHan, noiCap: docForm.noiCap.trim(),
      };
      const response = editingDoc
        ? await api.updateGuestDocument(bookingId, editingDoc, payload)
        : await api.addGuestDocument(bookingId, payload);
      setProfile(response.data);
      setDocForm(emptyDoc()); setEditingDoc('');
      setOk(editingDoc ? 'Đã cập nhật giấy tờ.' : 'Đã thêm giấy tờ.');
    } catch (e) { setError(api.errorMessage(e, 'Không lưu được giấy tờ.')); }
    finally { setBusy(false); }
  };
  const removeDoc = async (id) => {
    if (blocked || !window.confirm('Xóa giấy tờ này khỏi hồ sơ khách?')) return;
    setBusy(true); setError(''); setOk('');
    try {
      const response = await api.deleteGuestDocument(bookingId, id);
      setProfile(response.data);
      if (editingDoc === id) { setEditingDoc(''); setDocForm(emptyDoc()); }
      setOk('Đã xóa giấy tờ.');
    } catch (e) { setError(api.errorMessage(e, 'Không xóa được giấy tờ.')); }
    finally { setBusy(false); }
  };

  return (
    <section className="panel" style={{ marginBottom: 24, overflowWrap: 'anywhere' }}>
      <header className="panel-head">
        <h2>Lịch khởi hành đang vận hành</h2>
        <button type="button" disabled={blocked} onClick={() => setRefresh((x) => x + 1)}>Tải lại chỗ và khách</button>
      </header>
      <p className="muted">Chọn một lịch để xem khách đã giữ chỗ / đã thanh toán. Chỉ được sửa hồ sơ và giấy tờ, không sửa vé hay thanh toán tại đây.</p>
      <Notice error={error} />
      {ok && <div className="notice ok" role="status">{ok}</div>}
      <label className="inline" style={{ marginBottom: 12 }}>
        <input type="checkbox" checked={paidOnly} onChange={(e) => setPaidOnly(e.target.checked)} /> Chỉ hiện vé đã thanh toán / hoàn thành
      </label>
      {!dates.length && !busy && <p>Chưa có lịch khởi hành. Thêm lịch bên dưới để mở bán chuyến.</p>}
      <div className="table">
        {dates.map((d) => {
          const open = departureId === d.maKhoiHanh;
          const fill = d.sucChua ? Math.round((Number(d.daDat || 0) / Number(d.sucChua)) * 100) : 0;
          return (
            <ExpandRecord key={d.maKhoiHanh} open={open} onClose={() => { setDepartureId(''); setRoster(null); setBookingId(''); setProfile(null); }} summary={
              <div className="row" style={{ display: 'flex', gap: 12, flexWrap: 'wrap', cursor: 'pointer' }}
                onClick={() => {
                  if (blocked) return;
                  if (open) { setDepartureId(''); setRoster(null); setBookingId(''); setProfile(null); }
                  else { setBookingId(''); setProfile(null); setDepartureId(d.maKhoiHanh); setRefresh((x) => x + 1); }
                }}>
                <b>{stamp(d.ngayKhoiHanh)}</b>
                <span>{d.diaDiem || '—'}</span>
                <span>Sức chứa {d.sucChua} · Đã đặt {d.daDat} · Còn trống {d.conTrong}</span>
                <span className="chart-track" style={{ width: 120 }} title={`${fill}%`}>
                  <i style={{ width: `${fill}%`, background: fill >= 80 ? 'var(--coral)' : 'var(--jade)' }} />
                </span>
                {canSuaTour && <button type="button" disabled={blocked} onClick={(event) => {
                  event.stopPropagation();
                  setEditing(d.maKhoiHanh);
                  setForm({ ...d, ngayKhoiHanh: inputDate(d.ngayKhoiHanh), ngayKetThuc: inputDate(d.ngayKetThuc), soCho: d.soCho ?? defaultCapacity });
                }}>Sửa lịch / số chỗ</button>}
              </div>
            }>
              {open && roster && <>
                <h3>{stamp(roster.ngayKhoiHanh)} · {roster.soTaiKhoan} tài khoản · {roster.daDat}/{roster.sucChua} chỗ</h3>
                <div style={{ overflowX: 'auto' }}>
                  <table style={{ width: '100%', textAlign: 'left' }}>
                    <thead><tr><th>SĐT</th><th>Họ tên</th><th>Số chỗ</th><th>Mã vé</th><th>Trạng thái</th></tr></thead>
                    <tbody>
                      {guests.map((b) => (
                        <tr key={b.maBooking} tabIndex={0} style={{ cursor: 'pointer', outline: bookingId === b.maBooking ? '3px solid var(--gold)' : undefined }}
                          onClick={() => !blocked && setBookingId(b.maBooking)}
                          onKeyDown={(e) => { if (e.key === 'Enter' && !blocked) setBookingId(b.maBooking); }}>
                          <td>{b.soDienThoai || '—'}</td>
                          <td>{b.maKhachHang
                            ? <Link to={`/khach-hang?id=${encodeURIComponent(b.maKhachHang)}`}>{b.hoTen || 'Chưa có hồ sơ'}</Link>
                            : (b.hoTen || 'Chưa có hồ sơ')}</td>
                          <td>{b.soCho} = {b.slnguoiLon} NL + {b.sltreEm} TE</td>
                          <td><button type="button" disabled={blocked} onClick={() => setBookingId(b.maBooking)}>{b.maBooking}</button></td>
                          <td><em className={`badge ${String(b.trangThai || '').trim()}`}>{STATUS[b.trangThai] || b.trangThai}</em></td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                {!guests.length && <p>Không có vé {paidOnly ? 'đã thanh toán' : 'còn giữ chỗ'} trên lịch này.</p>}
              </>}
              {open && bookingId && profile && (
                <form className="panel" style={{ marginTop: 16 }} onSubmit={saveProfile}>
                  <h3>Hồ sơ khách — {profile.maBooking} {profile.maKhachHang && <Link to={`/khach-hang?id=${encodeURIComponent(profile.maKhachHang)}`}>Mở trang khách</Link>}</h3>
                  <p className="muted">Vé {STATUS[profile.trangThai] || profile.trangThai || '—'}. Số điện thoại theo tài khoản, không sửa tại đây.</p>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 12 }}>
                    <label>Họ<input required maxLength={20} value={profileForm.ho} onChange={(e) => setProfileForm({ ...profileForm, ho: e.target.value })} /></label>
                    <label>Tên<input required maxLength={50} value={profileForm.ten} onChange={(e) => setProfileForm({ ...profileForm, ten: e.target.value })} /></label>
                    <label>Họ trên giấy tờ<input maxLength={20} value={profileForm.hoGiayTo} onChange={(e) => setProfileForm({ ...profileForm, hoGiayTo: e.target.value })} /></label>
                    <label>Tên trên giấy tờ<input maxLength={50} value={profileForm.tenGiayTo} onChange={(e) => setProfileForm({ ...profileForm, tenGiayTo: e.target.value })} /></label>
                    <label>Danh xưng<input maxLength={20} value={profileForm.danhXung} onChange={(e) => setProfileForm({ ...profileForm, danhXung: e.target.value })} /></label>
                    <label>Giới tính<input maxLength={20} value={profileForm.gioiTinh} onChange={(e) => setProfileForm({ ...profileForm, gioiTinh: e.target.value })} /></label>
                    <label>Ngày sinh<input type="date" value={profileForm.ngaySinh} onChange={(e) => setProfileForm({ ...profileForm, ngaySinh: e.target.value })} /></label>
                    <label>Quốc tịch<input value={profileForm.quocTich} onChange={(e) => setProfileForm({ ...profileForm, quocTich: e.target.value })} /></label>
                    <label>Email<input type="email" value={profileForm.email} onChange={(e) => setProfileForm({ ...profileForm, email: e.target.value })} /></label>
                    <label>Điện thoại<input readOnly value={profile.soDienThoai || ''} /></label>
                  </div>
                  <button disabled={blocked || !canSuaBooking} style={{ marginTop: 12 }}>{profile.maKhachHang ? 'Lưu hồ sơ' : 'Tạo hồ sơ khách'}</button>
                  <h4>Giấy tờ</h4>
                  {!(profile.giayTo || []).length && <p>Chưa có giấy tờ.</p>}
                  {(profile.giayTo || []).map((g) => (
                    <div key={g.maGiayTo} className="notice">
                      <b>{g.loaiGiayTo} · {g.soTrenGiayTo}</b>
                      <p>Cấp: {day(g.ngayCap)} · Hết hạn: {day(g.ngayHetHan)} · Nơi cấp: {g.noiCap}</p>
                      <div className="inline" style={{ margin: 0 }}>
                        {canSuaBooking && <button type="button" disabled={blocked} onClick={() => {
                          setEditingDoc(g.maGiayTo);
                          setDocForm({
                            loaiGiayTo: g.loaiGiayTo || 'CCCD', soTrenGiayTo: g.soTrenGiayTo || '',
                            ngayCap: isoDay(g.ngayCap), ngayHetHan: isoDay(g.ngayHetHan), noiCap: g.noiCap || '',
                          });
                        }}>Sửa giấy tờ</button>}
                        {canXoaBooking && <button type="button" className="danger" disabled={blocked} onClick={() => removeDoc(g.maGiayTo)}>Xóa</button>}
                      </div>
                    </div>
                  ))}
                </form>
              )}
              {open && bookingId && profile && (
                <form onSubmit={saveDoc} style={{ marginTop: 12 }}>
                  <h4>{editingDoc ? 'Sửa giấy tờ' : 'Thêm giấy tờ'}</h4>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: 12 }}>
                    <label>Loại<select value={docForm.loaiGiayTo} onChange={(e) => setDocForm({ ...docForm, loaiGiayTo: e.target.value })}>
                      <option>CCCD</option><option>CMND</option><option>Passport</option><option>Khác</option>
                    </select></label>
                    <label>Số<input required maxLength={50} value={docForm.soTrenGiayTo} onChange={(e) => setDocForm({ ...docForm, soTrenGiayTo: e.target.value })} /></label>
                    <label>Ngày cấp<input type="date" required value={docForm.ngayCap} onChange={(e) => setDocForm({ ...docForm, ngayCap: e.target.value })} /></label>
                    <label>Hết hạn<input type="date" required value={docForm.ngayHetHan} onChange={(e) => setDocForm({ ...docForm, ngayHetHan: e.target.value })} /></label>
                    <label>Nơi cấp<input required value={docForm.noiCap} onChange={(e) => setDocForm({ ...docForm, noiCap: e.target.value })} /></label>
                  </div>
                  <div className="inline" style={{ marginTop: 12 }}>
                    <button disabled={blocked || !profile.maKhachHang || !canSuaBooking}>{editingDoc ? 'Lưu giấy tờ' : 'Thêm giấy tờ'}</button>
                    {editingDoc && <button type="button" disabled={blocked} onClick={() => { setEditingDoc(''); setDocForm(emptyDoc()); }}>Hủy sửa</button>}
                  </div>
                </form>
              )}
            </ExpandRecord>
          );
        })}
      </div>
      <form onSubmit={saveDeparture}>
        <fieldset disabled={blocked || !canSuaTour} style={{ border: 0, padding: 0 }}>
          <h3>{editing ? 'Sửa lịch khởi hành' : 'Thêm lịch khởi hành'}</h3>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 12 }}>
            <label>Khởi hành<input type="datetime-local" required value={form.ngayKhoiHanh} onChange={(e) => setForm({ ...form, ngayKhoiHanh: e.target.value })} /></label>
            <label>Kết thúc<input type="datetime-local" value={form.ngayKetThuc} onChange={(e) => setForm({ ...form, ngayKetThuc: e.target.value })} /></label>
            <label>Địa điểm<input maxLength={100} value={form.diaDiem || ''} onChange={(e) => setForm({ ...form, diaDiem: e.target.value })} /></label>
            {!hideCapacity && <label>Số chỗ<input type="number" min="0" max="2147483647" step="1" value={form.soCho} onChange={(e) => setForm({ ...form, soCho: e.target.value })} /><small>Để trống: theo tour ({defaultCapacity}).</small></label>}
          </div>
          <button>Lưu lịch</button>
          {editing && <button type="button" onClick={() => { setEditing(''); setForm(emptyDeparture(defaultCapacity)); }}>Thêm lịch khác</button>}
        </fieldset>
      </form>
    </section>
  );
}
