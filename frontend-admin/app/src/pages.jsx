import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import * as api from './api';
import { useAuth } from './context';
import { Notice } from './components';
export { DesignRequests } from './DesignRequests';

const v = (o, ...ks) => ks.map((k) => o?.[k]).find((x) => x !== undefined && x !== null);
const itemsOf = (r) => (Array.isArray(r?.data) ? r.data : (r?.data?.items || []));
const money = (n) => `${Number(n || 0).toLocaleString('vi-VN')} đ`;
const dateText = (value) => (value ? new Date(value).toLocaleDateString('vi-VN') : '—');

const STATUS = {
  ChoXacNhan: 'Chờ xác nhận',
  DaXacNhan: 'Đã xác nhận',
  DaThanhToan: 'Đã thanh toán',
  HoanThanh: 'Hoàn thành',
  DaHuy: 'Đã hủy',
};

const NEXT = {
  ChoXacNhan: ['DaXacNhan', 'DaHuy'],
  DaXacNhan: ['DaThanhToan', 'DaHuy'],
  DaThanhToan: ['HoanThanh'],
};

const label = (s) => STATUS[String(s || '').trim()] || s || '—';

export function Login() {
  const { login } = useAuth();
  const nav = useNavigate();
  const [f, setF] = useState({ SoDienThoai: '', MatKhau: '' });
  const [e, setE] = useState('');

  const submit = async (x) => {
    x.preventDefault();
    try {
      await login(f);
      nav('/');
    } catch (err) {
      setE(err.message || api.errorMessage(err, 'Đăng nhập thất bại.'));
    }
  };

  return (
    <section className="login">
      <h1>ANAM Admin</h1>
      <p>Đăng nhập để vận hành tour và xác nhận booking.</p>
      <Notice error={e} />
      <form onSubmit={submit}>
        <input placeholder="Số điện thoại" onChange={(x) => setF({ ...f, SoDienThoai: x.target.value })} />
        <input type="password" placeholder="Mật khẩu" onChange={(x) => setF({ ...f, MatKhau: x.target.value })} />
        <button>Đăng nhập</button>
      </form>
    </section>
  );
}

export function Dashboard() {
  const [tours, setTours] = useState([]);
  const [bookings, setBookings] = useState([]);
  const [error, setError] = useState('');

  useEffect(() => {
    Promise.allSettled([
      api.tours({ pageSize: 50 }),
      api.bookings({ pageSize: 50 }),
    ]).then(([tourRes, bookRes]) => {
      if (tourRes.status === 'fulfilled') setTours(itemsOf(tourRes.value));
      else setError(api.errorMessage(tourRes.reason, 'Không tải được tour.'));
      if (bookRes.status === 'fulfilled') setBookings(itemsOf(bookRes.value));
      else setError((prev) => prev || api.errorMessage(bookRes.reason, 'Không tải được booking. Đăng nhập lại bằng tài khoản Admin/Sale.'));
    });
  }, []);

  const pending = bookings.filter((x) => String(x.trangThai || '').trim() === 'ChoXacNhan');
  const paid = bookings.filter((x) => /DaThanhToan|HoanThanh/.test(String(x.trangThai || '').trim()));
  const revenue = paid.reduce((sum, x) => sum + Number(x.thanhTien || 0), 0);

  return (
    <>
      <h1>Tổng quan vận hành</h1>
      <Notice error={error} />
      <div className="stats">
        <div><b>{tours.length}</b><span>Tour đang bán</span></div>
        <div><b>{bookings.length}</b><span>Booking gần đây</span></div>
        <div><b>{pending.length}</b><span>Chờ xác nhận</span></div>
        <div><b>{money(revenue)}</b><span>Đã thu (đã TT / hoàn thành)</span></div>
      </div>
      <section className="panel">
        <header className="panel-head">
          <h2>Cần xử lý</h2>
          <Link to="/booking">Mở toàn bộ booking</Link>
        </header>
        {pending.length ? (
          <div className="table">
            {pending.slice(0, 8).map((x) => (
              <div className="row booking-row" key={x.maBooking}>
                <b>{x.maBooking}</b>
                <span>{x.tenTour || x.maTour}</span>
                <span>{x.hoTen || x.soDienThoai || '—'}</span>
                <span>{money(x.thanhTien)}</span>
                <em className="badge wait">{label(x.trangThai)}</em>
              </div>
            ))}
          </div>
        ) : (
          <p className="muted">Không có booking chờ xác nhận.</p>
        )}
      </section>
    </>
  );
}

export function BookingManagement() {
  const [items, setItems] = useState([]);
  const [filter, setFilter] = useState('');
  const [id, setId] = useState('');
  const [item, setItem] = useState(null);
  const [error, setError] = useState('');
  const [ok, setOk] = useState('');

  const load = () => api.bookings({ pageSize: 50, trangThai: filter || undefined })
    .then((r) => setItems(itemsOf(r)))
    .catch((x) => setError(api.errorMessage(x, 'Không tải được danh sách booking.')));

  useEffect(() => { load(); }, [filter]);

  const find = async (x) => {
    x.preventDefault();
    if (!id.trim()) return;
    setError('');
    setOk('');
    try {
      const r = await api.booking(id.trim());
      setItem(r.data);
    } catch (err) {
      setItem(null);
      setError(api.errorMessage(err, 'Không tìm thấy booking.'));
    }
  };

  const change = async (status) => {
    const target = item?.maBooking || id;
    if (!target) return;
    setError('');
    setOk('');
    try {
      await api.status(target, status);
      const next = { ...item, trangThai: status };
      setItem(next);
      setItems((list) => list.map((b) => (b.maBooking === target ? { ...b, trangThai: status } : b)));
      setOk(`Đã chuyển sang ${label(status)}.`);
    } catch (err) {
      setError(api.errorMessage(err, 'Không cập nhật được trạng thái.'));
    }
  };

  const next = NEXT[String(item?.trangThai || '').trim()] || [];

  return (
    <>
      <h1>Quản lý booking</h1>
      <Notice error={error} />
      {ok && <div className="notice ok">{ok}</div>}

      <form className="panel inline" onSubmit={find}>
        <input placeholder="Mã booking (tuỳ chọn)" value={id} onChange={(x) => setId(x.target.value)} />
        <button>Tra cứu</button>
        <select value={filter} onChange={(x) => setFilter(x.target.value)}>
          <option value="">Tất cả trạng thái</option>
          {Object.entries(STATUS).map(([k, name]) => <option value={k} key={k}>{name}</option>)}
        </select>
      </form>

      {item && (
        <div className="panel">
          <h3>{item.maBooking} · {item.tenTour}</h3>
          <p>{item.hoTen || 'Khách'} · {item.soDienThoai || '—'} · đặt ngày {dateText(item.ngayDat)}</p>
          <p>{item.slnguoiLon || 0} người lớn, {item.sltreEm || 0} trẻ em · {money(item.thanhTien)}</p>
          <p>Trạng thái: <em className={`badge ${String(item.trangThai || '').trim()}`}>{label(item.trangThai)}</em></p>
          <div className="inline">
            {next.map((s) => (
              <button key={s} className={s === 'DaHuy' ? 'danger' : undefined} onClick={() => change(s)}>
                {label(s)}
              </button>
            ))}
            {!next.length && <span className="muted">Không còn bước tiếp theo.</span>}
          </div>
        </div>
      )}

      <div className="table">
        {items.map((x) => (
          <button
            type="button"
            className={`row booking-row ${item?.maBooking === x.maBooking ? 'on' : ''}`}
            key={x.maBooking}
            onClick={() => { setItem(x); setId(x.maBooking); setError(''); setOk(''); }}
          >
            <b>{x.maBooking}</b>
            <span>{x.tenTour || x.maTour}</span>
            <span>{x.hoTen || x.soDienThoai || '—'}</span>
            <span>{money(x.thanhTien)}</span>
            <em className={`badge ${String(x.trangThai || '').trim()}`}>{label(x.trangThai)}</em>
          </button>
        ))}
        {!items.length && !error && <p className="muted">Chưa có booking nào.</p>}
      </div>
    </>
  );
}

export function TourAdminPage() {
  const [items, setItems] = useState([]);
  const [selected, setSelected] = useState('');
  const [media, setMedia] = useState([]);
  const [form, setForm] = useState({ TenTour: '', GiaTour: 0 });
  const [mediaForm, setMediaForm] = useState({ File: null, ThuTu: 0, IsAvatar: false });
  const [e, setE] = useState('');

  const load = () => api.tours({ pageSize: 50 }).then((r) => setItems(itemsOf(r))).catch((x) => setE(api.errorMessage(x, 'Không tải được tour.')));
  useEffect(load, []);

  const edit = (x) => setForm({ TenTour: v(x, 'tenTour', 'TenTour') || '', GiaTour: v(x, 'giaTour', 'GiaTour') || 0 });

  const save = async (x) => {
    x.preventDefault();
    try {
      if (selected) await api.updateTour(selected, { ...form, GiaTour: +form.GiaTour });
      else await api.createTour({ ...form, GiaTour: +form.GiaTour });
      setForm({ TenTour: '', GiaTour: 0 });
      setSelected('');
      load();
    } catch (err) {
      setE(api.errorMessage(err, 'Không thể lưu tour.'));
    }
  };

  const remove = async (id) => {
    if (!confirm('Xóa tour này?')) return;
    try {
      await api.deleteTour(id);
      setSelected('');
      load();
    } catch (err) {
      setE(api.errorMessage(err, 'Không thể xóa tour.'));
    }
  };

  const loadMedia = async (id) => {
    setSelected(id);
    try {
      const r = await api.tourMedia(id);
      setMedia(r.data.items || r.data || []);
    } catch (err) {
      setE(api.errorMessage(err, 'Không tải được media.'));
    }
  };

  const addMedia = async (x) => {
    x.preventDefault();
    if (!mediaForm.File) {
      setE('Hãy chọn ảnh hoặc video.');
      return;
    }
    try {
      await api.uploadTourMedia(selected, mediaForm.File, +mediaForm.ThuTu, mediaForm.IsAvatar);
      await loadMedia(selected);
      setMediaForm({ File: null, ThuTu: 0, IsAvatar: false });
      x.currentTarget.reset();
    } catch (err) {
      setE(api.errorMessage(err, 'Không thể upload media.'));
    }
  };

  const removeMedia = async (id) => {
    try {
      await api.deleteMedia(id);
      await loadMedia(selected);
    } catch (err) {
      setE(api.errorMessage(err, 'Không thể xóa media.'));
    }
  };

  return (
    <>
      <h1>Quản lý tour</h1>
      <Notice error={e} />
      <form className="panel inline" onSubmit={save}>
        <input placeholder="Tên tour" value={form.TenTour} onChange={(x) => setForm({ ...form, TenTour: x.target.value })} />
        <input type="number" placeholder="Giá" value={form.GiaTour} onChange={(x) => setForm({ ...form, GiaTour: x.target.value })} />
        <button>{selected ? 'Lưu thay đổi' : 'Thêm tour'}</button>
      </form>
      <div className="table">
        {items.map((x) => {
          const id = v(x, 'maTour', 'MaTour');
          return (
            <div className="row" key={id}>
              <b>{id}</b>
              <span>{v(x, 'tenTour', 'TenTour')}</span>
              <span>{v(x, 'giaTour', 'GiaTour')} đ</span>
              <button onClick={() => { setSelected(id); edit(x); }}>Sửa</button>
              <button onClick={() => remove(id)}>Xóa</button>
              <button onClick={() => loadMedia(id)}>Ảnh/video</button>
            </div>
          );
        })}
      </div>
      {selected && (
        <section className="panel">
          <h2>Media tour {selected}</h2>
          <form className="inline" onSubmit={addMedia}>
            <input type="file" accept=".jpg,.jpeg,.png,.webp,.mp4,.webm,image/jpeg,image/png,image/webp,video/mp4,video/webm" onChange={(x) => setMediaForm({ ...mediaForm, File: x.target.files?.[0] || null })} required />
            <input type="number" min="0" placeholder="Thứ tự" value={mediaForm.ThuTu} onChange={(x) => setMediaForm({ ...mediaForm, ThuTu: x.target.value })} />
            <label><input type="checkbox" checked={mediaForm.IsAvatar} onChange={(x) => setMediaForm({ ...mediaForm, IsAvatar: x.target.checked })} /> Ảnh đại diện</label>
            <button>Upload media</button>
          </form>
          <p>Ảnh: JPEG/PNG/WebP tối đa 10 MB. Video: MP4/WebM tối đa 100 MB.</p>
          <div className="list">
            {media.map((m) => (
              <div key={m.maAnhTour}>{m.loaiMedia} · <a href={m.url} target="_blank" rel="noreferrer">Xem media</a> <button onClick={() => removeMedia(m.maAnhTour)}>Xóa</button></div>
            ))}
          </div>
        </section>
      )}
    </>
  );
}
