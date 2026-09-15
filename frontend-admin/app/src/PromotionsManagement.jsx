import { useEffect, useState } from 'react';
import * as api from './api';
import { ExpandRecord, Notice } from './components';
import { useAuth } from './context';

const asUtc = (value) => value ? new Date(/[zZ]$|[+-]\d\d:\d\d$/.test(value) ? value : value + 'Z') : null;
const localInput = (date) => {
  if (!date || Number.isNaN(date.getTime())) return '';
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
};
const money = (value) => Number(value || 0).toLocaleString('vi-VN') + ' đ';
const rows = (response) => Array.isArray(response.data) ? response.data : response.data.items || [];
const empty = () => ({
  tenKm: '', maCode: '', ngayBd: localInput(new Date()), ngayKt: localInput(new Date(Date.now() + 30 * 86400000)),
  donVi: '%', giamGia: 10, coCongDon: false, trangThai: 'HoatDong',
  donToiThieu: '', lanDatDau: false, soLuong: '', maTours: [],
});
const fromPromotion = (item) => {
  const conditions = item.dieuKien || [];
  const minimums = conditions.flatMap((c) => c.donToiThieu == null ? [] : [c.donToiThieu]);
  const limits = conditions.flatMap((c) => c.soLuong == null ? [] : [c.soLuong]);
  return {
    tenKm: item.tenKm || '', maCode: (item.maCode || '').trim(),
    ngayBd: localInput(asUtc(item.ngayBd)), ngayKt: localInput(asUtc(item.ngayKt)),
    donVi: (item.donVi || '%').trim(), giamGia: item.giamGia ?? 0, coCongDon: item.coCongDon === true,
    trangThai: (item.trangThai || 'HoatDong').trim(), donToiThieu: minimums.length ? Math.max(...minimums) : '',
    lanDatDau: conditions.some((c) => c.lanDatDau), soLuong: limits.length ? Math.min(...limits) : '',
    maTours: (item.maTours || []).map((id) => id.trim()),
  };
};

export default function PromotionsManagement() {
  const { can } = useAuth();
  const canThem = can('UuDai', 'Them');
  const canSua = can('UuDai', 'Sua');
  const canXoa = can('UuDai', 'Xoa');
  const [items, setItems] = useState([]);
  const [tours, setTours] = useState([]);
  const [selected, setSelected] = useState('');
  const [creating, setCreating] = useState(false);
  const [form, setForm] = useState(empty);
  const [busy, setBusy] = useState(true);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const grid = { display: 'grid', gridTemplateColumns: 'repeat(auto-fit,minmax(min(100%,220px),1fr))', gap: 14 };
  const label = { display: 'grid', gap: 6, minWidth: 0 };
  const fieldStyle = { minWidth: 0, width: '100%', padding: 10 };
  const loadList = async () => setItems(rows(await api.promotions({ all: 1 })));
  const loadDetail = async (id) => {
    const response = await api.promotionDetail(id);
    setCreating(false); setSelected(id); setForm(fromPromotion(response.data));
  };
  const run = async (action, fallback) => {
    if (busy) return;
    setBusy(true); setError(''); setMessage('');
    try { await action(); } catch (e) { setError(api.errorMessage(e, fallback)); }
    finally { setBusy(false); }
  };
  useEffect(() => {
    let active = true;
    const loadTours = async () => {
      const result = [];
      for (let page = 1; active; page++) {
        const response = await api.tours({ page, pageSize: 100 });
        const current = rows(response); result.push(...current);
        if (!current.length || page * 100 >= (response.data.totalCount ?? current.length)) break;
      }
      return result;
    };
    Promise.allSettled([api.promotions({ all: 1 }), loadTours()]).then(([promos, tourResult]) => {
      if (!active) return;
      const errors = [];
      if (promos.status === 'fulfilled') setItems(rows(promos.value));
      else errors.push(api.errorMessage(promos.reason, 'Không tải được ưu đãi.'));
      if (tourResult.status === 'fulfilled') setTours(tourResult.value);
      else errors.push(api.errorMessage(tourResult.reason, 'Không tải được danh sách tour.'));
      setError(errors.join(' ')); setBusy(false);
    });
    return () => { active = false; };
  }, []);
  const field = (key) => (event) => setForm((current) => ({ ...current, [key]: event.target.value }));
  const fresh = () => {
    setSelected(''); setCreating(true);
    setForm(empty());
    setError(''); setMessage('');
    requestAnimationFrame(() => document.getElementById('promo-create')?.scrollIntoView({ block: 'nearest', behavior: 'smooth' }));
  };
  const openRow = (id) => run(() => loadDetail(id), 'Không tải được chi tiết ưu đãi.');
  const toggleRow = (id) => {
    if (selected === id) { setSelected(''); setCreating(false); return; }
    openRow(id);
  };
  const save = (event) => {
    event.preventDefault();
    run(async () => {
      const start = new Date(form.ngayBd), end = new Date(form.ngayKt);
      if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || end <= start) {
        setError('Ngày kết thúc phải lớn hơn ngày bắt đầu.'); return;
      }
      const data = {
        tenKm: form.tenKm.trim(), maCode: form.maCode.trim(), ngayBd: start.toISOString(), ngayKt: end.toISOString(),
        donVi: form.donVi, giamGia: Number(form.giamGia), coCongDon: form.coCongDon, trangThai: form.trangThai,
        dieuKien: { donToiThieu: form.donToiThieu === '' ? null : Number(form.donToiThieu),
          lanDatDau: form.lanDatDau, soLuong: form.soLuong === '' ? null : Number(form.soLuong) },
        maTours: form.maTours,
      };
      let id = selected;
      if (id) await api.updatePromotion(id, data);
      else { const response = await api.createPromotion(data); id = response.data.maKm; }
      setMessage(selected ? 'Đã cập nhật ưu đãi.' : 'Đã thêm ưu đãi.');
      await loadList(); await loadDetail(id);
    }, 'Không lưu được ưu đãi hoặc tải lại dữ liệu.');
  };
  const remove = (id) => {
    if (!window.confirm('Xóa hẳn ưu đãi này? Nếu đã có vé dùng mã, hãy sửa trạng thái thành Ngừng hoạt động.')) return;
    run(async () => {
      await api.deletePromotion(id);
      setMessage('Đã xóa ưu đãi.');
      await loadList();
      if (selected === id) { setSelected(''); setCreating(false); setForm(empty()); }
    }, 'Không xóa được ưu đãi.');
  };
  const options = [...tours, ...form.maTours.filter((id) => !tours.some((tour) => tour.maTour.trim() === id))
    .map((id) => ({ maTour: id, tenTour: 'Tour đã gắn với mã' }))];
  const editor = (
    <form className="panel" style={{ margin: 0, boxShadow: 'none', border: 0, padding: 0 }} aria-label="Thông tin ưu đãi" onSubmit={save}>
      <h2>{selected ? 'Sửa ưu đãi ' + selected : 'Thêm ưu đãi mới'}</h2>
      <fieldset disabled={busy || (selected ? !canSua : !canThem)} style={{ border: 0, padding: 0, minWidth: 0 }}>
        <div style={grid}>
          <label style={label}>Tên ưu đãi<input required maxLength={50} style={fieldStyle} value={form.tenKm} onChange={field('tenKm')} /></label>
          <label style={label}>Mã ưu đãi<input required maxLength={10} style={fieldStyle} value={form.maCode} onChange={field('maCode')} /></label>
          <label style={label}>Ngày bắt đầu<input type="datetime-local" required style={fieldStyle} value={form.ngayBd} onChange={field('ngayBd')} /></label>
          <label style={label}>Ngày kết thúc<input type="datetime-local" required style={fieldStyle} value={form.ngayKt} onChange={field('ngayKt')} /></label>
          <label style={label}>Đơn vị<select aria-label="Đơn vị" style={fieldStyle} value={form.donVi} onChange={field('donVi')}>
            <option value="%">%</option><option value="VND">VND</option>
          </select></label>
          <label style={label}>Mức giảm<input type="number" required min="1" max={form.donVi === '%' ? 100 : 2147483647} step="1"
            style={fieldStyle} value={form.giamGia} onChange={field('giamGia')} /></label>
          <label style={label}>Trạng thái<select aria-label="Trạng thái ưu đãi" style={fieldStyle} value={form.trangThai} onChange={field('trangThai')}>
            <option value="HoatDong">Hoạt động</option><option value="NgungHoatDong">Ngừng hoạt động</option>
          </select></label>
          <label style={label}>Đơn tối thiểu (đ)<input type="number" min="0" max="2147483647" style={fieldStyle}
            value={form.donToiThieu} onChange={field('donToiThieu')} /></label>
          <label style={label}>Giới hạn lượt sử dụng<input type="number" min="0" max="2147483647" style={fieldStyle}
            placeholder="Để trống = không giới hạn" value={form.soLuong} onChange={field('soLuong')} /></label>
          <label><input type="checkbox" checked={form.coCongDon} onChange={(event) => setForm({ ...form, coCongDon: event.target.checked })} /> Cho cộng dồn</label>
          <label><input type="checkbox" checked={form.lanDatDau} onChange={(event) => setForm({ ...form, lanDatDau: event.target.checked })} /> Chỉ lần đặt đầu</label>
        </div>
        <h3>Tour áp dụng</h3>
        <p className="muted">Không chọn tour = áp dụng mọi tour. Ngừng hoạt động: đổi trạng thái rồi Lưu. Xóa là xóa hẳn.</p>
        <div style={{ ...grid, maxHeight: 220, overflow: 'auto' }}>
          {options.map((tour) => <label key={tour.maTour} style={{ overflowWrap: 'anywhere' }}>
            <input type="checkbox" checked={form.maTours.includes(tour.maTour.trim())} onChange={(event) => {
              const id = tour.maTour.trim();
              setForm((current) => ({ ...current, maTours: event.target.checked ? [...current.maTours, id] : current.maTours.filter((key) => key !== id) }));
            }} /> {tour.maTour} · {tour.tenTour}
          </label>)}
        </div>
        <button style={{ marginTop: 18 }} type="submit">{selected ? 'Lưu ưu đãi' : 'Thêm ưu đãi'}</button>
      </fieldset>
    </form>
  );

  return <div style={{ minWidth: 0, overflowWrap: 'anywhere' }}
    onInvalidCapture={() => { setMessage(''); setError('Hãy điền đủ thông tin và kiểm tra giới hạn số trong biểu mẫu.'); }}>
    <h1>Quản lý ưu đãi</h1>
    <div id="promotion-notice" aria-live="polite"><Notice error={error} />{message && <div className="notice ok" role="status">{message}</div>}</div>
    {busy && <p role="status">Đang xử lý…</p>}
    <div className="inline">
      {canThem && <button disabled={busy} onClick={() => fresh()}>Thêm mới</button>}
      <button disabled={busy} onClick={() => run(async () => {
        await loadList(); if (selected) await loadDetail(selected);
      }, 'Không tải lại được ưu đãi.')}>Tải lại ưu đãi</button>
    </div>
    {creating && <div id="promo-create" className="create-slot record open"><div className="record-body">{editor}</div></div>}
    <div className="table" aria-label="Danh sách ưu đãi">
      {items.map((item) => <ExpandRecord key={item.maKm} open={selected === item.maKm} summary={
        <div className="row" style={{ gridTemplateColumns: 'repeat(auto-fit,minmax(min(100%,140px),1fr))', cursor: busy ? 'wait' : 'pointer' }}
          onClick={() => { if (!busy) toggleRow(item.maKm); }}>
          <b>{item.maCode}</b><span>{item.tenKm}</span><span>{item.donVi === '%' ? item.giamGia + '%' : money(item.giamGia)}</span>
          <span>{item.trangThai === 'HoatDong' ? 'Hoạt động' : 'Ngừng hoạt động'}</span>
          <span>Hết hạn: {asUtc(item.ngayKt)?.toLocaleString('vi-VN') || '—'}</span>
          <div className="inline" style={{ margin: 0 }}>
            {canSua && <button disabled={busy} aria-label={'Sửa ưu đãi ' + item.maCode}
              onClick={(event) => { event.stopPropagation(); openRow(item.maKm); }}>Sửa</button>}
            {canXoa && <button className="danger" disabled={busy} aria-label={'Xóa ưu đãi ' + item.maCode}
              onClick={(event) => { event.stopPropagation(); remove(item.maKm); }}>Xóa</button>}
          </div>
        </div>
      }>{editor}</ExpandRecord>)}
      {!items.length && !busy && <p>Chưa có ưu đãi.</p>}
    </div>
  </div>;
}
