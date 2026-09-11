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
  ChoHoanTien: 'Chờ hoàn tiền',
  DaHuy: 'Đã hủy',
};

const NEXT = {
  ChoXacNhan: ['DaXacNhan', 'DaHuy'],
  DaXacNhan: ['DaThanhToan', 'DaHuy'],
  DaThanhToan: ['HoanThanh'],
  ChoHoanTien: [],
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
  const [busy, setBusy] = useState(false);

  const load = () => api.bookings({ pageSize: 50, trangThai: filter || undefined })
    .then((r) => setItems(itemsOf(r)))
    .catch((x) => setError(api.errorMessage(x, 'Không tải được danh sách booking.')));

  useEffect(() => { load(); }, [filter]);

  const openBooking = async (bookingId) => {
    if (!bookingId || busy) return;
    setBusy(true);
    setId(bookingId);
    setError('');
    setOk('');
    try {
      const r = await api.booking(bookingId);
      setItem(r.data);
      setItems((list) => list.map((booking) => booking.maBooking === bookingId ? { ...booking, ...r.data } : booking));
    } catch (err) {
      setItem(null);
      setError(api.errorMessage(err, 'Không tìm thấy booking.'));
    } finally { setBusy(false); }
  };

  const find = (event) => {
    event.preventDefault();
    openBooking(id.trim());
  };

  const change = async (status) => {
    const target = item?.maBooking || id;
    if (!target || busy) return;
    setBusy(true);
    setError('');
    setOk('');
    try {
      const response = await api.status(target, status);
      const next = { ...item, ...response.data };
      setItem(next);
      setItems((list) => list.map((b) => (b.maBooking === target ? { ...b, ...response.data } : b)));
      setOk(`Đã chuyển sang ${label(status)}.`);
    } catch (err) {
      setError(api.errorMessage(err, 'Không cập nhật được trạng thái.'));
    } finally { setBusy(false); }
  };

  const refund = async () => {
    const target = item?.maBooking || id;
    if (!target || busy) return;
    if (!confirm('Xác nhận đã hoàn tiền cho khách trên booking ' + target + '?')) return;
    setBusy(true);
    setError('');
    setOk('');
    try {
      const response = await api.confirmRefund(target);
      const next = { ...item, ...response.data };
      setItem(next);
      setItems((list) => list.map((b) => (b.maBooking === target ? { ...b, ...response.data } : b)));
      setOk(`Đã xác nhận hoàn tiền ${money(response.data?.soTienHoan)}.`);
    } catch (err) {
      setError(api.errorMessage(err, 'Không xác nhận được hoàn tiền.'));
    } finally { setBusy(false); }
  };

  const state = String(item?.trangThai || '').trim();
  const next = NEXT[state] || [];
  const total = Number(item?.thanhTien ?? 0);
  const paid = Number(item?.tongDaThanhToan ?? 0);
  const remaining = Number(item?.conLai ?? (total - paid));
  const paymentBlock = state === 'ChoXacNhan' && paid <= 0
    ? 'Chưa có thanh toán.'
    : state === 'DaXacNhan' && remaining > 0 ? 'Mới đặt cọc, chưa đủ.'
    : state === 'ChoHoanTien' ? 'Khách đã gửi hủy. Xác nhận hoàn tiền trước khi đóng vé.'
    : '';

  return (
    <>
      <h1>Quản lý booking</h1>
      <Notice error={error} />
      {ok && <div className="notice ok">{ok}</div>}

      <form className="panel inline" onSubmit={find}>
        <input placeholder="Mã booking (tuỳ chọn)" value={id} disabled={busy} onChange={(x) => setId(x.target.value)} />
        <button disabled={busy}>Tra cứu</button>
        <select value={filter} onChange={(x) => setFilter(x.target.value)}>
          <option value="">Tất cả trạng thái</option>
          {Object.entries(STATUS).map(([k, name]) => <option value={k} key={k}>{name}</option>)}
        </select>
      </form>

      {item && (
        <div className="panel">
          <h3>{item.maBooking} · {item.tenTour}</h3>
          <p>{item.hoTen || 'Khách'} · {item.soDienThoai || '—'} · đặt ngày {dateText(item.ngayDat)}</p>
          <p>{item.slnguoiLon || 0} người lớn, {item.sltreEm || 0} trẻ em</p>
          <div className="inline" aria-label="Số tiền booking" style={{ gap: 16 }}>
            <div className="notice"><span>Thành tiền</span><br /><strong>{money(total)}</strong></div>
            <div className="notice ok"><span>Đã thanh toán</span><br /><strong>{money(paid)}</strong></div>
            <div className={`notice ${remaining > 0 ? 'error' : 'ok'}`}><span>Còn lại</span><br /><strong>{money(remaining)}</strong></div>
          </div>
          <button type="button" disabled={busy} onClick={() => openBooking(item.maBooking)}>Tải lại số tiền</button>
          <p>Trạng thái: <em className={`badge ${String(item.trangThai || '').trim()}`}>{label(item.trangThai)}</em></p>
          {paymentBlock && <p id="booking-payment-block" className="notice" role="status">{paymentBlock}</p>}
          <div className="inline">
            {next.map((s) => {
              const cancelLocked = s === 'DaHuy' && paid > 0;
              const paymentLocked = (s === 'DaXacNhan' && paid <= 0) || (s === 'DaThanhToan' && remaining > 0) || cancelLocked;
              const disabled = busy || paymentLocked;
              return <button key={s} className={s === 'DaHuy' ? 'danger' : undefined} disabled={disabled}
                style={{ opacity: disabled ? 0.5 : 1, cursor: disabled ? 'not-allowed' : 'pointer' }}
                title={cancelLocked ? 'Khách đã thanh toán. Chờ yêu cầu hủy rồi xác nhận hoàn tiền.' : undefined}
                aria-describedby={paymentLocked ? 'booking-payment-block' : undefined} onClick={() => change(s)}>
                {label(s)}
              </button>;
            })}
            {state === 'ChoHoanTien' && (
              <button type="button" disabled={busy} onClick={refund}>Xác nhận hoàn tiền</button>
            )}
            {!next.length && state !== 'ChoHoanTien' && <span className="muted">Không còn bước tiếp theo.</span>}
          </div>
        </div>
      )}

      <div className="table">
        {items.map((x) => (
          <button
            type="button"
            className={`row booking-row ${item?.maBooking === x.maBooking ? 'on' : ''}`}
            key={x.maBooking}
            disabled={busy}
            onClick={() => openBooking(x.maBooking)}
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
  const emptyTour = () => ({
    MaTour: '', TenTour: '', Mota: '', ThoiGian: 1, GiaTour: 0, Slkhach: 1,
    SlhuongDanVien: 1, LoaiTour: 'Chuan', TrangThai: 'HoatDong', DieuKhoan: '',
  });
  const emptySchedule = () => ({ NgayThu: 1, ThuTuTrongNgay: 1, MaDthamQuan: '', MaSanPham: '', Mota: '', SoLuong: 1 });
  const emptyMedia = () => ({ File: null, ThuTu: 0, IsAvatar: false });
  const [items, setItems] = useState([]);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [listType, setListType] = useState('Chuan');
  const [selected, setSelected] = useState('');
  const [detail, setDetail] = useState(null);
  const [schedule, setSchedule] = useState(null);
  const [places, setPlaces] = useState([]);
  const [media, setMedia] = useState(null);
  const [form, setForm] = useState(emptyTour);
  const [scheduleForm, setScheduleForm] = useState(emptySchedule);
  const [mediaForm, setMediaForm] = useState(emptyMedia);
  const [fileKey, setFileKey] = useState(0);
  const [busy, setBusy] = useState(true);
  const [e, setE] = useState('');
  const [message, setMessage] = useState('');
  const grid = { display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 230px), 1fr))', gap: 14 };
  const field = { display: 'grid', gap: 6, minWidth: 0 };
  const control = { width: '100%', minWidth: 0 };
  const fieldset = { border: 0, padding: 0, margin: 0, minWidth: 0 };
  const stateNames = { Nhap: 'Nháp', HoatDong: 'Hoạt động', An: 'Ẩn' };

  const toForm = (data) => Object.fromEntries(Object.keys(emptyTour()).map((key) => {
    const camelKey = key[0].toLowerCase() + key.slice(1);
    const value = v(data, camelKey, key);
    return [key, ['MaTour', 'LoaiTour', 'TrangThai'].includes(key) ? String(value ?? '').trim() : (value ?? '')];
  }));
  const loadList = async (nextPage = page, type = listType) => {
    const response = await api.tours({ page: nextPage, pageSize: 50, loaiTour: type });
    setItems(itemsOf(response));
    setTotal(response.data.totalCount ?? itemsOf(response).length);
    setPage(nextPage);
    setListType(type);
  };
  const loadDetails = async (id) => {
    const results = await Promise.allSettled([api.tourDetail(id), api.tourSchedule(id), api.tourMedia(id)]);
    if (results[0].status === 'rejected') throw results[0].reason;
    const data = results[0].value.data;
    setSelected(id);
    setDetail(data);
    setForm(toForm(data));
    setSchedule(results[1].status === 'fulfilled' ? results[1].value.data.lichTrinh ?? [] : null);
    setMedia(results[2].status === 'fulfilled' ? itemsOf(results[2].value) : null);
    const errors = results.slice(1).flatMap((result, index) => result.status === 'rejected'
      ? [api.errorMessage(result.reason, index === 0 ? 'Không tải được lịch trình.' : 'Không tải được ảnh/video.')]
      : []);
    if (errors.length) setE(errors.join(' '));
    if (id !== selected) {
      setScheduleForm(emptySchedule());
      setMediaForm(emptyMedia());
      setFileKey((key) => key + 1);
    }
  };
  const run = async (action, fallback) => {
    if (busy) return;
    setBusy(true);
    setE('');
    setMessage('');
    try { await action(); }
    catch (err) { setE(api.errorMessage(err, fallback)); }
    finally { setBusy(false); }
  };
  useEffect(() => {
    let active = true;
    Promise.allSettled([api.tours({ page: 1, pageSize: 50 }), api.sightseeingPlaces()]).then(([toursResult, placesResult]) => {
      if (!active) return;
      const errors = [];
      if (toursResult.status === 'fulfilled') {
        setItems(itemsOf(toursResult.value));
        setTotal(toursResult.value.data.totalCount ?? itemsOf(toursResult.value).length);
      } else errors.push(api.errorMessage(toursResult.reason, 'Không tải được tour.'));
      if (placesResult.status === 'fulfilled') setPlaces(itemsOf(placesResult.value));
      else errors.push(api.errorMessage(placesResult.reason, 'Không tải được danh sách điểm. Bạn vẫn có thể nhập mã điểm.'));
      setE(errors.join(' '));
      setBusy(false);
    });
    return () => { active = false; };
  }, []);
  useEffect(() => {
    if (e || message) document.getElementById('tour-notice')?.scrollIntoView({ block: 'nearest' });
  }, [e, message]);

  const selectTour = (id) => run(() => loadDetails(id), 'Không tải được chi tiết tour.');
  const newTour = () => {
    if (busy) return;
    setSelected('');
    setDetail(null);
    setSchedule(null);
    setMedia(null);
    setForm(emptyTour());
    setScheduleForm(emptySchedule());
    setMediaForm(emptyMedia());
    setFileKey((key) => key + 1);
    setE('');
    setMessage('');
    document.getElementById('tour-form')?.scrollIntoView({ block: 'start' });
  };
  const save = (event) => {
    event.preventDefault();
    run(async () => {
      const optionalNumber = (value) => value === '' ? null : Number(value);
      const optionalText = (key) => form[key] === '' && selected && v(detail, key[0].toLowerCase() + key.slice(1), key) == null
        ? null : form[key];
      const data = {
        TenTour: form.TenTour.trim(), Mota: optionalText('Mota'), ThoiGian: optionalNumber(form.ThoiGian),
        GiaTour: Number(form.GiaTour), Slkhach: Number(form.Slkhach),
        SlhuongDanVien: optionalNumber(form.SlhuongDanVien), LoaiTour: form.LoaiTour,
        TrangThai: form.TrangThai, DieuKhoan: optionalText('DieuKhoan'),
      };
      if (!data.TenTour || !data.LoaiTour || (!selected && !form.MaTour.trim())) {
        setE('Hãy nhập mã, tên và loại tour.'); return;
      }
      let id = selected;
      if (id) {
        await api.updateTour(id, data);
        setMessage('Đã lưu thay đổi tour.');
      } else {
        const response = await api.createTour({ MaTour: form.MaTour.trim(), ...data });
        id = v(response.data, 'maTour', 'MaTour') || form.MaTour.trim();
        setSelected(id);
        setDetail({ ...response.data, trangThai: data.TrangThai });
        setForm({ ...form, MaTour: id });
        setMessage('Đã thêm tour ' + id + '.');
      }
      await loadDetails(id);
      await loadList();
    }, 'Không thể lưu tour hoặc tải lại chi tiết. Dữ liệu vừa lưu có thể đã thành công; hãy tải lại trước khi thử lại.');
  };
  const remove = (id) => {
    if (busy || !confirm('Xóa tour ' + id + '? Nếu đã có booking, hệ thống sẽ ẩn tour.')) return;
    run(async () => {
      const response = await api.deleteTour(id);
      setMessage(response.data?.message || 'Đã xóa tour ' + id + '.');
      if (response.status === 200) await loadDetails(id);
      else if (selected === id) {
        setSelected(''); setDetail(null); setForm(emptyTour()); setSchedule(null); setMedia(null);
        setScheduleForm(emptySchedule()); setMediaForm(emptyMedia()); setFileKey((key) => key + 1);
      }
      await loadList();
    }, 'Không thể xóa tour hoặc tải lại danh sách.');
  };
  const addSchedule = (event) => {
    event.preventDefault();
    if (!selected) return;
    run(async () => {
      if (!scheduleForm.MaDthamQuan.trim() && !scheduleForm.MaSanPham.trim()) {
        setE('Hãy chọn/nhập mã điểm tham quan hoặc mã sản phẩm đối tác.'); return;
      }
      await api.createTourSchedule({
        MaTour: selected, NgayThu: Number(scheduleForm.NgayThu), ThuTuTrongNgay: Number(scheduleForm.ThuTuTrongNgay),
        MaDthamQuan: scheduleForm.MaDthamQuan.trim() || null, MaSanPham: scheduleForm.MaSanPham.trim() || null,
        SoLuong: Number(scheduleForm.SoLuong), Mota: scheduleForm.Mota,
      });
      setMessage('Đã thêm dòng lịch trình. Giá tour được tính lại theo lịch trình.');
      setScheduleForm(emptySchedule());
      await loadDetails(selected);
      await loadList();
    }, 'Không thể thêm lịch trình hoặc tải lại chi tiết.');
  };
  const removeSchedule = (id) => {
    if (busy || !confirm('Xóa dòng lịch trình ' + id + '? Giá tour sẽ được tính lại.')) return;
    run(async () => {
      const response = await api.deleteTourSchedule(id);
      setMessage(response.data?.message || 'Đã xóa dòng lịch trình.');
      await loadDetails(selected);
      await loadList();
    }, 'Không thể xóa lịch trình hoặc tải lại chi tiết.');
  };
  const addMedia = (event) => {
    event.preventDefault();
    if (!selected) return;
    run(async () => {
      if (!mediaForm.File) { setE('Hãy chọn ảnh hoặc video.'); return; }
      await api.uploadTourMedia(selected, mediaForm.File, Number(mediaForm.ThuTu), mediaForm.IsAvatar);
      setMessage('Đã upload ảnh/video cho tour ' + selected + '.');
      setMediaForm(emptyMedia());
      setFileKey((key) => key + 1);
      await loadDetails(selected);
    }, 'Không thể upload ảnh/video hoặc tải lại chi tiết.');
  };
  const removeMedia = (id) => {
    if (busy || !confirm('Xóa ảnh/video này?')) return;
    run(async () => {
      await api.deleteMedia(id);
      setMessage('Đã xóa ảnh/video.');
      await loadDetails(selected);
    }, 'Không thể xóa ảnh/video hoặc tải lại chi tiết.');
  };
  const setTourField = (key) => (event) => setForm((current) => ({ ...current, [key]: event.target.value }));
  const setScheduleField = (key) => (event) => setScheduleForm((current) => ({ ...current, [key]: event.target.value }));
  const currentState = String(v(detail, 'trangThai', 'TrangThai') ?? '').trim();
  const scheduleLocked = selected && !['Nhap', 'HoatDong'].includes(currentState);

  return (
    <div className="tour-admin" style={{ minWidth: 0, overflowWrap: 'anywhere' }} onInvalidCapture={() => {
      setMessage('');
      setE('Hãy điền đủ các trường bắt buộc và kiểm tra giới hạn số trong biểu mẫu.');
    }}>
      <h1>Quản lý tour</h1>
      <div id="tour-notice" aria-live="polite">
        <Notice error={e} />
        {message && <div className="notice ok" role="status">{message}</div>}
      </div>
      {busy && <p role="status">Đang xử lý…</p>}
      <div className="inline">
        <button type="button" disabled={busy} onClick={newTour}>Thêm tour mới</button>
        <button type="button" disabled={busy} onClick={() => run(async () => {
          if (selected) await loadDetails(selected);
          await loadList();
        }, 'Không tải lại được dữ liệu tour.')}>Tải lại dữ liệu</button>
        <label style={field}>Danh sách loại tour
          <select aria-label="Danh sách loại tour" value={listType} disabled={busy} onChange={(event) => {
            const type = event.target.value;
            run(() => loadList(1, type), 'Không tải được danh sách tour.');
          }}>
            <option value="Chuan">Tour chuẩn</option>
            <option value="TuThietKe">Tour tự thiết kế</option>
          </select>
        </label>
      </div>
      <div className="table" aria-label="Danh sách tour">
        {items.map((item) => {
          const id = v(item, 'maTour', 'MaTour');
          const state = String(v(item, 'trangThai', 'TrangThai') || '').trim();
          return (
            <div className={'row' + (selected === id ? ' on' : '')} key={id}
              style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 120px), 1fr))', cursor: busy ? 'wait' : 'pointer',
                outline: selected === id ? '3px solid var(--gold)' : undefined }}
              onClick={() => !busy && selectTour(id)}>
              <button type="button" disabled={busy} aria-label={'Chọn tour ' + id}
                onClick={(event) => { event.stopPropagation(); selectTour(id); }}>{id}</button>
              <span>{v(item, 'tenTour', 'TenTour')}</span>
              <span>Giá: {money(v(item, 'giaTour', 'GiaTour'))}</span>
              <span>Số khách: {v(item, 'slkhach', 'Slkhach') ?? '—'}</span>
              <span className="badge">{stateNames[state] || state}</span>
              <div className="inline" style={{ margin: 0 }}>
                <button type="button" disabled={busy} onClick={(event) => {
                  event.stopPropagation();
                  run(async () => { await loadDetails(id); document.getElementById('tour-form')?.scrollIntoView({ block: 'start' }); },
                    'Không tải được đầy đủ thông tin để sửa tour.');
                }}>Sửa</button>
                <button type="button" className="danger" disabled={busy}
                  onClick={(event) => { event.stopPropagation(); remove(id); }}>Xóa</button>
              </div>
            </div>
          );
        })}
        {!items.length && !busy && <p>Không có tour trong danh sách này.</p>}
      </div>
      <div className="inline" style={{ marginTop: 18 }}>
        <button type="button" disabled={busy || page <= 1} onClick={() => run(() => loadList(page - 1), 'Không tải được tour.')}>Trang trước</button>
        <span>Trang {page} / {Math.max(1, Math.ceil(total / 50))} · {total} tour</span>
        <button type="button" disabled={busy || page * 50 >= total} onClick={() => run(() => loadList(page + 1), 'Không tải được tour.')}>Trang sau</button>
      </div>

      <section className="panel" id="tour-form" style={{ marginBottom: 24 }}>
        <h2>{selected ? 'Chi tiết / sửa tour ' + selected : 'Thêm tour'}</h2>
        <form onSubmit={save} aria-label="Thông tin tour">
          <fieldset disabled={busy} style={fieldset}>
            <div style={grid}>
              <label style={field}>Mã tour<input style={control} maxLength={20} required readOnly={!!selected}
                value={form.MaTour} onChange={setTourField('MaTour')} /></label>
              <label style={field}>Tên tour<input style={control} maxLength={150} required
                value={form.TenTour} onChange={setTourField('TenTour')} /></label>
              <label style={field}>Số ngày (ThoiGian)<input style={control} type="number" min="1" max="2147483647" step="1"
                required={!selected} value={form.ThoiGian} onChange={setTourField('ThoiGian')} /></label>
              <label style={field}>Giá tour (đ)<input style={control} type="number" min="0" max="2147483647" step="1" required
                value={form.GiaTour} onChange={setTourField('GiaTour')} /></label>
              <label style={field}>Số khách<input style={control} type="number" min="1" max="2147483647" step="1" required
                value={form.Slkhach} onChange={setTourField('Slkhach')} /></label>
              <label style={field}>Số hướng dẫn viên<input style={control} type="number" min="0" max="2147483647" step="1"
                value={form.SlhuongDanVien} onChange={setTourField('SlhuongDanVien')} /></label>
              <label style={field}>Loại tour<input style={control} readOnly required value={form.LoaiTour} /></label>
              <label style={field}>Trạng thái tour<select aria-label="Trạng thái tour" style={control} required value={form.TrangThai} onChange={setTourField('TrangThai')}>
                {!Object.hasOwn(stateNames, form.TrangThai) && <option value={form.TrangThai}>{form.TrangThai || 'Chọn trạng thái'}</option>}
                {Object.entries(stateNames).map(([key, name]) => <option key={key} value={key}>{name} ({key})</option>)}
              </select></label>
              <label style={{ ...field, gridColumn: '1 / -1' }}>Mô tả tour<textarea aria-label="Mô tả tour" style={control} rows={3}
                value={form.Mota} onChange={setTourField('Mota')} /></label>
              <label style={{ ...field, gridColumn: '1 / -1' }}>Điều khoản<textarea aria-label="Điều khoản" style={control} rows={3}
                value={form.DieuKhoan} onChange={setTourField('DieuKhoan')} /></label>
            </div>
            <p className="muted">Tour mới thuộc loại Chuan. Khi sửa, mã và loại tour được giữ nguyên; hãy lưu thông tin trước khi thao tác lịch trình/ảnh hoặc tải lại.</p>
            <button type="submit">{selected ? 'Lưu thay đổi' : 'Thêm tour'}</button>
          </fieldset>
        </form>
      </section>

      {selected && <>
        <section className="panel" style={{ marginBottom: 24 }}>
          <h2>Lịch trình tour {selected}</h2>
          <p className="muted">Chỉ sửa lịch trình khi tour Nhap/HoatDong và chưa có hợp đồng DaKy. Thêm/xóa dòng sẽ tính lại giá tour theo tổng thành tiền. Điểm không gắn sản phẩm đối tác có đơn giá 0 theo API hiện tại.</p>
          {scheduleLocked && <p className="notice error">Tour {currentState === 'An' ? 'đang ẩn (An)' : 'không ở trạng thái Nhap/HoatDong'}, không thể thay đổi lịch trình.</p>}
          {schedule === null ? <p>Chưa tải được lịch trình. Hãy bấm Tải lại dữ liệu.</p> : <>
            <div className="table" aria-label="Lịch trình tour">
              {schedule.map((line) => (
                <div className="row" key={line.maLichTrinh} style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 130px), 1fr))' }}>
                  <b>Ngày {line.ngayThu ?? '—'} · Thứ tự {line.thuTuTrongNgay ?? '—'}</b>
                  <span>{line.tenDiaDanh || line.maDthamQuan || 'Không gắn điểm'}{line.maSanPham && <small style={{ display: 'block' }}>Sản phẩm: {line.tenSanPham || line.maSanPham}</small>}</span>
                  <span>{line.mota || '—'}</span>
                  <span>SL: {line.soLuong ?? '—'} · Đơn giá: {money(line.donGia)}</span>
                  <b>Thành tiền: {money(line.thanhTien)}</b>
                  <button type="button" className="danger" disabled={busy || scheduleLocked} aria-label={'Xóa lịch trình ' + line.maLichTrinh}
                    onClick={() => removeSchedule(line.maLichTrinh)}>Xóa dòng</button>
                </div>
              ))}
            </div>
            {!schedule.length && <p>Chưa có dòng lịch trình.</p>}
          </>}
          <form onSubmit={addSchedule} aria-label="Thêm lịch trình" style={{ marginTop: 20 }}>
            <fieldset disabled={busy || scheduleLocked} style={fieldset}>
              <div style={grid}>
                <label style={field}>Ngày thứ<input style={control} type="number" min="1" max={v(detail, 'thoiGian', 'ThoiGian') || 2147483647} required
                  value={scheduleForm.NgayThu} onChange={setScheduleField('NgayThu')} /></label>
                <label style={field}>Thứ tự trong ngày<input style={control} type="number" min="1" max="2147483647" required
                  value={scheduleForm.ThuTuTrongNgay} onChange={setScheduleField('ThuTuTrongNgay')} /></label>
                <label style={field}>Mã điểm tham quan<input style={control} list="tour-places" maxLength={20}
                  value={scheduleForm.MaDthamQuan} onChange={setScheduleField('MaDthamQuan')} /></label>
                <datalist id="tour-places">{places.map((place) => <option key={place.maDthamQuan} value={place.maDthamQuan}>{place.tenDiaDanh}</option>)}</datalist>
                <label style={field}>Mã sản phẩm đối tác (nếu có)<input style={control} maxLength={20}
                  value={scheduleForm.MaSanPham} onChange={setScheduleField('MaSanPham')} /></label>
                <label style={field}>Số lượng<input style={control} type="number" min="1" max="2147483647" required
                  value={scheduleForm.SoLuong} onChange={setScheduleField('SoLuong')} /></label>
                <label style={{ ...field, gridColumn: '1 / -1' }}>Mô tả lịch trình<textarea aria-label="Mô tả lịch trình" style={control} rows={2}
                  value={scheduleForm.Mota} onChange={setScheduleField('Mota')} /></label>
              </div>
              <button style={{ marginTop: 16 }} type="submit">Thêm dòng lịch trình</button>
            </fieldset>
          </form>
        </section>
        <section className="panel">
          <h2>Ảnh / video tour {selected}</h2>
          <form onSubmit={addMedia} aria-label="Upload ảnh tour">
            <fieldset disabled={busy} style={fieldset}>
              <div style={grid}>
                <label style={field}>File ảnh / video<input key={fileKey} style={control} type="file"
                  accept=".jpg,.jpeg,.png,.webp,.mp4,.webm,image/jpeg,image/png,image/webp,video/mp4,video/webm" required
                  onChange={(event) => setMediaForm({ ...mediaForm, File: event.target.files?.[0] || null })} /></label>
                <label style={field}>Thứ tự ảnh<input style={control} type="number" min="0" max="2147483647" required value={mediaForm.ThuTu}
                  onChange={(event) => setMediaForm({ ...mediaForm, ThuTu: event.target.value })} /></label>
                <label><input type="checkbox" checked={mediaForm.IsAvatar}
                  onChange={(event) => setMediaForm({ ...mediaForm, IsAvatar: event.target.checked })} /> Ảnh đại diện</label>
              </div>
              <button type="submit" style={{ marginTop: 16 }}>Upload media</button>
            </fieldset>
          </form>
          <p className="muted">Ảnh: JPEG/PNG/WebP tối đa 10 MB. Video: MP4/WebM tối đa 100 MB.</p>
          {media === null ? <p>Chưa tải được ảnh/video. Hãy bấm Tải lại dữ liệu.</p> : <div style={grid}>
            {media.map((item) => <div key={item.maAnhTour} style={{ minWidth: 0 }}>
              {item.loaiMedia === 'Video'
                ? <video src={item.url} controls preload="metadata" style={{ width: '100%', height: 150, objectFit: 'contain' }} />
                : <img src={item.url} alt={'Ảnh tour ' + selected + ' · ' + item.maAnhTour} loading="lazy" style={{ width: '100%', height: 150, objectFit: 'cover' }} />}
              <p>{item.loaiMedia} · Thứ tự: {item.thuTu ?? 0}{item.isAvatar ? ' · Ảnh đại diện' : ''}</p>
              <div className="inline">
                <a href={item.url} target="_blank" rel="noreferrer">Xem media</a>
                <button type="button" className="danger" disabled={busy} aria-label={'Xóa ảnh ' + item.maAnhTour}
                  onClick={() => removeMedia(item.maAnhTour)}>Xóa ảnh/video</button>
              </div>
            </div>)}
            {!media.length && <p>Chưa có ảnh/video.</p>}
          </div>}
        </section>
      </>}
    </div>
  );
}
