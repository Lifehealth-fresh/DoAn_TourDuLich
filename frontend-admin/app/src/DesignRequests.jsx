import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import * as api from './api';
import { ExpandRecord, Notice, SearchSelect } from './components';
import { useAuth } from './context';

const foldVi = (value) => String(value || '').normalize('NFD').replace(/đ/gi, 'd').replace(/[\u0300-\u036f]/g, '').toLowerCase();
const trim = (value) => String(value ?? '').trim();
const money = (value) => `${Number(value || 0).toLocaleString('vi-VN')} đ`;
const dateText = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : '—';
const itemsOf = (response) => Array.isArray(response.data) ? response.data : response.data?.items || [];
const rowsOf = itemsOf;
const labels = { Moi: 'Mới gửi', Huy: 'Đã hủy', DangThietKe: 'Đang thiết kế', CanChinhSua: 'Cần chỉnh sửa', ChoKhachXacNhan: 'Chờ khách xác nhận', ChoDuyet: 'Chờ duyệt', DaDuyet: 'Đã duyệt' };
const statusLabel = (value) => labels[trim(value)] || trim(value) || '—';
const toRow = (item = {}) => ({ ngayThu: item.ngayThu ?? 1, thuTuTrongNgay: item.thuTuTrongNgay ?? 1, maDthamQuan: trim(item.maDthamQuan), maSanPham: trim(item.maSanPham), soLuong: item.soLuong ?? 1, mota: item.mota || '' });

export function DesignRequests() {
  const { can } = useAuth();
  const [params] = useSearchParams();
  const allowed = can('ThietKe', 'Sua');
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState(params.get('yeuCau') || '');
  const [plans, setPlans] = useState([]);
  const [schedule, setSchedule] = useState(null);
  const [rows, setRows] = useState([]);
  const [editing, setEditing] = useState(false);
  const [refresh, setRefresh] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadingDetails, setLoadingDetails] = useState(false);
  const [busy, setBusy] = useState('');
  const [listError, setListError] = useState('');
  const [detailError, setDetailError] = useState('');
  const [error, setError] = useState('');
  const [ok, setOk] = useState('');
  const [lyDo, setLyDo] = useState('');
  const [places, setPlaces] = useState([]);
  const [products, setProducts] = useState([]);
  const [destMaTinh, setDestMaTinh] = useState('');
  const selected = items.find((item) => item.maYeuCau === selectedId);
  const tourId = trim(selected?.maTourTao);
  const state = trim(schedule?.trangThai ?? selected?.trangThai);
  const blocked = Boolean(busy || loading || loadingDetails || listError || detailError);
  const revision = schedule || selected;
  const canGenerate = allowed && !blocked && !editing && state === 'Moi';
  const canEdit = allowed && !blocked && Boolean(tourId) && ['DangThietKe', 'CanChinhSua'].includes(state);
  const canSubmit = canEdit && !editing;
  const canApprove = allowed && !blocked && !editing && Boolean(tourId) && state === 'ChoDuyet';

  useEffect(() => {
    api.sightseeingPlaces().then((response) => setPlaces(rowsOf(response))).catch(() => {});
    api.partnerProducts().then((response) => setProducts(rowsOf(response))).catch(() => {});
  }, []);

  useEffect(() => {
    const dest = trim(selected?.diemDenMongMuon);
    if (!dest) { setDestMaTinh(''); return; }
    let active = true;
    api.provinces(dest).then((response) => {
      if (!active) return;
      const hit = (response.data || [])[0];
      setDestMaTinh(trim(hit?.maTinh));
    }).catch(() => { if (active) setDestMaTinh(''); });
    return () => { active = false; };
  }, [selected?.diemDenMongMuon]);

  useEffect(() => {
    let active = true;
    setLoading(true); setListError('');
    api.designRequests().then((response) => {
      if (!active) return;
      const next = itemsOf(response);
      setItems(next); setSelectedId((id) => next.some((item) => item.maYeuCau === id) ? id : '');
    }).catch((err) => { if (active) setListError(api.errorMessage(err, 'Không tải được yêu cầu thiết kế.')); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [refresh]);

  useEffect(() => {
    let active = true;
    setPlans([]); setSchedule(null); setRows([]); setDetailError('');
    setLoadingDetails(Boolean(selectedId));
    if (selectedId) {
      Promise.all([api.proposals(selectedId), tourId ? api.currentDesignSchedule(selectedId) : Promise.resolve(null)]).then(([proposals, current]) => {
        if (!active) return;
        setPlans(itemsOf(proposals)); setSchedule(current?.data ?? null);
        setRows((current?.data?.lichTrinh || []).map(toRow));
      }).catch((err) => { if (active) setDetailError(api.errorMessage(err, 'Không tải được đề xuất hoặc lịch trình hiện tại.')); })
        .finally(() => { if (active) setLoadingDetails(false); });
    }
    return () => { active = false; };
  }, [selectedId, tourId, refresh]);

  const reload = () => { setLoading(true); setLoadingDetails(Boolean(selectedId)); setRefresh((value) => value + 1); };
  const select = (item) => {
    if (selectedId === item.maYeuCau) {
      setSelectedId(''); setPlans([]); setSchedule(null); setRows([]); setEditing(false);
      setError(''); setOk(''); setLyDo('');
      return;
    }
    setSelectedId(item.maYeuCau); setLoadingDetails(true); setPlans([]); setSchedule(null);
    setError(''); setOk(''); setLyDo(''); setEditing(false);
  };
  const act = async (action, payload) => {
    if (!selected || blocked) return;
    setError(''); setOk('');
    if (action === 'reject' && !trim(lyDo)) { setError('Vui lòng nhập lý do từ chối.'); return; }
    setBusy(action);
    try {
      if (action === 'generate') {
        await api.generate(selectedId);
        setOk(trim(selected?.maTourTao)
          ? `Đã sinh 1 đề xuất và gắn vào tour. Kiểm tra lịch, chỉnh sửa rồi gửi khách xác nhận.`
          : `Đã sinh đề xuất cho ${selectedId}. Khách có thể xem và chọn phương án.`);
      } else if (action === 'save') {
        const result = await api.editDesignSchedule(selectedId, payload);
        setEditing(false); setOk(`Đã lưu lịch trình. Giá tour hiện tại: ${money(result.data.giaTour)}.`);
      } else if (action === 'submit') {
        await api.submitDesignForApproval(selectedId); setOk(`Đã gửi lịch hiện tại cho khách xác nhận: ${selectedId}.`);
      } else if (action === 'approve') {
        await api.approve(selectedId); setOk(`Đã duyệt yêu cầu ${selectedId}. Khách có thể mở tour để đặt.`);
      } else if (action === 'reject') {
        await api.reject(selectedId, { lyDo: trim(lyDo) }); setLyDo('');
        setOk(`Đã từ chối yêu cầu ${selectedId}, chuyển về Cần chỉnh sửa.`);
      }
      reload();
    } catch (err) {
      setError(api.errorMessage(err, 'Không thực hiện được thao tác thiết kế.'));
      if (err.response?.status === 409) { setEditing(false); reload(); }
    } finally { setBusy(''); }
  };
  const changeRow = (index, name, value) => setRows((current) => current.map((row, i) => i === index ? { ...row, [name]: value } : row));
  const save = (event) => {
    event.preventDefault();
    if (!editing || !canEdit) return;
    const chiTiets = rows.map((row) => ({
      ngayThu: Number(row.ngayThu), thuTuTrongNgay: Number(row.thuTuTrongNgay), soLuong: Number(row.soLuong),
      maDthamQuan: trim(row.maDthamQuan) || null, maSanPham: trim(row.maSanPham) || null, mota: trim(row.mota),
    }));
    const maxDay = Math.min(30, Math.max(1, Number(selected.soNgay || 1)));
    if (!chiTiets.length || chiTiets.some((row) => ![row.ngayThu, row.thuTuTrongNgay, row.soLuong].every(Number.isInteger) ||
        row.ngayThu < 1 || row.ngayThu > maxDay || row.thuTuTrongNgay < 1 || row.thuTuTrongNgay > 2147483647 || row.soLuong < 1 || row.soLuong > 2147483647 || (!row.maDthamQuan && !row.maSanPham))) {
      setError('Mỗi dòng cần ngày hợp lệ, thứ tự và số lượng nguyên dương, cùng mã điểm hoặc sản phẩm.'); return;
    }
    if (new Set(chiTiets.map((row) => `${row.ngayThu}-${row.thuTuTrongNgay}`)).size !== chiTiets.length) {
      setError('Không được trùng thứ tự trong cùng một ngày.'); return;
    }
    const byDay = {};
    chiTiets.forEach((row) => { (byDay[row.ngayThu] ||= []).push(row); });
    const hotelsUsed = [...new Set(chiTiets.map((row) => products.find((item) => item.maSanPham === row.maSanPham))
      .filter((item) => item?.loaiDoiTac === 'LuuTru').map((item) => item.maDoiTac))];
    if (hotelsUsed.length !== 1) { setError('Cả lịch trình chỉ dùng một khách sạn lưu trú.'); return; }
    const missingDay = Object.entries(byDay).some(([day, dayRows]) => {
      const hasVisit = dayRows.some((row) => row.maDthamQuan || products.find((item) => item.maSanPham === row.maSanPham)?.loaiDoiTac === 'HoatDong');
      const hasMeal = dayRows.some((row) => products.find((item) => item.maSanPham === row.maSanPham)?.loaiDoiTac === 'AnUong');
      const hasStay = dayRows.some((row) => products.find((item) => item.maSanPham === row.maSanPham)?.loaiDoiTac === 'LuuTru');
      if (!hasVisit || !hasMeal || !hasStay) { setError(`Ngày ${day} cần tham quan (hoặc vui chơi), ăn uống và khách sạn đã chọn.`); return true; }
      return false;
    });
    if (missingDay) return;
    const regionMismatch = Object.entries(byDay).some(([, dayRows]) => {
      const ids = dayRows.map((row) => row.maDthamQuan).filter(Boolean);
      const point = places.find((place) => ids.includes(place.maDthamQuan));
      const hotel = products.find((item) => item.maDoiTac === hotelsUsed[0]);
      if (point?.maTinh && hotel?.maTinh) return hotel.maTinh !== point.maTinh;
      return point?.maKhuVuc && hotel?.maKhuVuc && hotel.maKhuVuc !== point.maKhuVuc;
    });
    if (regionMismatch) { setError('Khách sạn phải cùng tỉnh với điểm tham quan.'); return; }
    act('save', { chiTiets });
  };
  const actionButton = (action, caption, allowed) => <button disabled={!allowed} style={{ opacity: allowed ? 1 : 0.5 }} onClick={() => act(action)}>{busy === action ? 'Đang xử lý…' : caption}</button>;

  return (
    <div style={{ minWidth: 0, overflowWrap: 'anywhere' }}>
      <header className="panel-head"><h1>Yêu cầu thiết kế</h1><button disabled={Boolean(busy) || loading || loadingDetails || editing} onClick={() => { setError(''); reload(); }}>Tải lại danh sách và đề xuất</button></header>
      <p>Khách gửi → chọn đề xuất → Sale sửa và lưu → gửi khách xác nhận → khách đồng ý → Admin duyệt → khách đặt.</p>
      <p className="notice">Chỉ được duyệt sau khi khách bấm Đồng ý lịch này. Khi khách yêu cầu chỉnh lại, sửa và gửi khách xác nhận lần nữa.</p>
      <Notice error={listError} /><Notice error={error} />{ok && <div className="notice ok" role="status">{ok}</div>}
      {loading ? <p role="status">Đang tải yêu cầu…</p> : !listError && <div className="table">
        {items.map((item) => <ExpandRecord key={item.maYeuCau} open={selectedId === item.maYeuCau} onClose={() => select(item)} summary={
          <button type="button" className={`row booking-row${selectedId === item.maYeuCau ? ' on' : ''}`} aria-pressed={selectedId === item.maYeuCau} disabled={Boolean(busy) || (editing && selectedId !== item.maYeuCau)} onClick={() => select(item)}>
            <b>{item.maYeuCau}</b><span>{item.diemDenMongMuon || 'Chưa chọn điểm đến'}</span><span>Ngày đi: {dateText(item.ngayDuKienDi)}</span>
            <span>{item.soNgay ?? '—'} ngày</span><span>{item.nganSachDuKien == null ? 'Chưa có ngân sách' : money(item.nganSachDuKien)}</span><span className="badge">{statusLabel(item.trangThai)}</span>
          </button>
        }>
      {selectedId === item.maYeuCau && selected && <>
        <header className="panel-head"><h2>Yêu cầu {selected.maYeuCau}</h2><span className="badge">{statusLabel(state)}</span></header>
        <p>Khách hàng: {selected.maUser} · Ngày gửi: {dateText(selected.ngayGui)}</p>
        {tourId && <p>Tour đã tạo: <b>{tourId}</b></p>}
        {revision?.lyDo && <p className="notice">{revision.nguonLyDo==='KhachHang'?'Khách đã gửi yêu cầu chỉnh: ':'Lý do Admin cần chỉnh / từ chối: '}{revision.lyDo}</p>}
        {state==='ChoKhachXacNhan'&&<p className="notice">Đang chờ khách xác nhận lịch đã lưu. Không thể gửi lại hoặc duyệt lúc này.</p>}
        {state === 'Moi' && <p>{trim(selected.maTourTao) ? 'Tour đã tạo sẵn. Sinh đề xuất sẽ tạo 1 lịch trình rồi chuyển sang Đang thiết kế để bạn chỉnh và gửi khách.' : 'Sinh lại sẽ thay thế các phương án chưa được khách chọn. Chỉ thực hiện khi cần xử lý lại.'}</p>}
        <div className="inline">
          {actionButton('generate', plans.length ? 'Sinh lại đề xuất' : (trim(selected.maTourTao) ? 'Sinh 1 đề xuất' : 'Sinh đề xuất'), canGenerate)}
          {actionButton('submit', 'Gửi duyệt', canSubmit)}
          {actionButton('approve', 'Duyệt', canApprove)}
        </div>
        {state === 'ChoDuyet' && <form onSubmit={(event) => { event.preventDefault(); act('reject'); }}>
          <label htmlFor="design-reject-reason">Lý do từ chối</label><div className="inline">
            <textarea id="design-reject-reason" required rows={3} value={lyDo} disabled={blocked} onChange={(event) => setLyDo(event.target.value)} />
            <button className="danger" disabled={!canApprove}>{busy === 'reject' ? 'Đang từ chối…' : 'Từ chối'}</button>
          </div>
        </form>}
        <Notice error={detailError} />
        {loadingDetails ? <p role="status">Đang tải đề xuất và lịch trình…</p> : !detailError && <>
          {schedule && <section className="panel">
            <header className="panel-head"><h2>Lịch trình hiện tại</h2>{!editing && canEdit && <button onClick={() => { setEditing(true); setError(''); setOk(''); }}>Sửa lịch trình</button>}</header>
            <p>Giá lịch trình đã lưu: <b>{money(schedule.tongGiaHienTai)}</b>. Máy chủ tính lại giá từ sản phẩm và số lượng khi lưu.</p>
            {editing ? <form onSubmit={save}>
              <p>Mỗi ngày cần tham quan/vui chơi, một bữa ăn, và cùng một khách sạn cho cả tour. Giờ ghi trong mô tả (07:00 có mặt, 09:00 check-in, 11:00 ăn, 15:00 tham quan).</p>
              <div className="table">{rows.map((row, index) => <fieldset className="panel" key={index} disabled={Boolean(busy)}>
                <legend>Dòng {index + 1}</legend>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: 12 }}>
                  <label>Ngày thứ<input style={{ width: '100%' }} type="number" required min="1" max={Math.min(30, Math.max(1, selected.soNgay || 1))} step="1" value={row.ngayThu} onChange={(event) => changeRow(index, 'ngayThu', event.target.value)} /></label>
                  <label>Thứ tự trong ngày<input style={{ width: '100%' }} type="number" required min="1" max="2147483647" step="1" value={row.thuTuTrongNgay} onChange={(event) => changeRow(index, 'thuTuTrongNgay', event.target.value)} /></label>
                  <label>Điểm tham quan<SearchSelect emptyLabel="— không chọn điểm —" placeholder="Gõ Hồ Hoàn Kiếm, Nha Trang…" value={row.maDthamQuan} onChange={(value) => changeRow(index, 'maDthamQuan', value)} options={places
                    .filter((place) => {
                      if (destMaTinh && trim(place.maTinh) === destMaTinh) return true;
                      const dest = foldVi(selected.diemDenMongMuon);
                      if (!dest) return true;
                      return foldVi(`${place.tenDiaDanh} ${place.diaChi} ${place.tenKhuVuc} ${place.maTinh}`).includes(dest);
                    })
                    .map((place) => ({ value: place.maDthamQuan, label: `${place.tenDiaDanh} (${place.maTinh || place.tenKhuVuc || '—'})` }))} /></label>
                  <label>Sản phẩm / khách sạn<SearchSelect emptyLabel="— không chọn sản phẩm —" placeholder="Gõ khách sạn, món ăn…" value={row.maSanPham} onChange={(value) => {
                    changeRow(index, 'maSanPham', value);
                    const product = products.find((item) => item.maSanPham === value);
                    if (product?.loaiDoiTac === 'LuuTru') changeRow(index, 'soLuong', 1);
                  }} options={products.filter((item) => {
                    const ids = rows.filter((line) => Number(line.ngayThu) === Number(row.ngayThu) && line.maDthamQuan).map((line) => line.maDthamQuan);
                    const point = places.find((place) => ids.includes(place.maDthamQuan));
                    if (item.loaiDoiTac === 'LuuTru') {
                      if (point?.maTinh) return trim(item.maTinh) === trim(point.maTinh);
                      if (destMaTinh) return trim(item.maTinh) === destMaTinh;
                      if (point?.maKhuVuc) return trim(item.maKhuVuc) === trim(point.maKhuVuc);
                      return true;
                    }
                    if (destMaTinh) return trim(item.maTinh) === destMaTinh;
                    const dest = foldVi(selected.diemDenMongMuon);
                    return !dest || foldVi(`${item.tenDoiTac} ${item.tenSanPham}`).includes(dest);
                  }).map((item) => ({ value: item.maSanPham, label: `${item.loaiDoiTac === 'LuuTru' ? 'KS' : 'SP'} · ${item.tenDoiTac} · ${item.tenSanPham} · ${Number(item.giaNiemYet || 0).toLocaleString('vi-VN')}đ${item.loaiDoiTac === 'LuuTru' ? '/đêm' : ''}` }))} /></label>
                  <label>Số lượng<input style={{ width: '100%' }} type="number" required min="1" max="2147483647" step="1" value={row.soLuong} onChange={(event) => changeRow(index, 'soLuong', event.target.value)} /></label>
                  <div><label htmlFor={`design-description-${index}`}>Mô tả</label><textarea id={`design-description-${index}`} style={{ width: '100%' }} rows={2} value={row.mota} onChange={(event) => changeRow(index, 'mota', event.target.value)} /></div>
                </div>
                <button type="button" className="danger" onClick={() => setRows((current) => current.filter((_, i) => i !== index))}>Xóa dòng {index + 1}</button>
              </fieldset>)}</div>
              <div className="inline">
                <button type="button" disabled={Boolean(busy)} onClick={() => setRows((current) => [...current, toRow({ ngayThu: current.at(-1)?.ngayThu || 1, thuTuTrongNgay: Number(current.at(-1)?.thuTuTrongNgay || 0) + 1 })])}>Thêm dòng</button>
                <button type="button" disabled={Boolean(busy)} onClick={() => {
                  setRows((current) => {
                    const day = Number(current.at(-1)?.ngayThu || 1);
                    const ids = current.filter((row) => Number(row.ngayThu) === day && row.maDthamQuan).map((row) => row.maDthamQuan);
                    const point = places.find((place) => ids.includes(place.maDthamQuan));
                    const hotels = products.filter((item) => item.loaiDoiTac === 'LuuTru' && (
                      point?.maTinh ? trim(item.maTinh) === trim(point.maTinh)
                        : destMaTinh ? trim(item.maTinh) === destMaTinh
                        : !point?.maKhuVuc || trim(item.maKhuVuc) === trim(point.maKhuVuc)
                    ));
                    if (!hotels.length) { setError(point ? 'Chưa có khách sạn cùng tỉnh với điểm trong ngày. Thêm ở mục Đối tác.' : 'Chưa có khách sạn đối tác. Hãy thêm ở mục Đối tác.'); return current; }
                    const hotel = hotels[0];
                    const order = Math.max(0, ...current.filter((row) => Number(row.ngayThu) === day).map((row) => Number(row.thuTuTrongNgay || 0))) + 1;
                    setError('');
                    return [...current, toRow({ ngayThu: day, thuTuTrongNgay: order, maSanPham: hotel.maSanPham, soLuong: 1, mota: `Nghỉ đêm: ${hotel.tenDoiTac} · ${hotel.tenSanPham}` })];
                  });
                }}>Thêm khách sạn cuối ngày</button>
                <button disabled={!canEdit}>{busy === 'save' ? 'Đang lưu…' : 'Lưu lịch trình'}</button>
                <button type="button" disabled={Boolean(busy)} onClick={() => { setEditing(false); setRows((schedule.lichTrinh || []).map(toRow)); setError(''); }}>Hủy chỉnh sửa</button>
              </div>
            </form> : <ol>{(schedule.lichTrinh || []).map((detail) => <li key={detail.maLichTrinh}>
              <b>Ngày {detail.ngayThu} · Mục {detail.thuTuTrongNgay}: {detail.laKhachSan ? `KS · ${detail.tenDoiTac || ''} · ${detail.tenSanPham}` : (detail.mota || detail.tenDiaDanh || detail.tenSanPham)}</b>
              <p>{detail.laKhachSan ? `${money(detail.donGia || detail.thanhTien)} / đêm` : `${detail.maDthamQuan || '—'} · ${detail.maSanPham || 'Không kèm sản phẩm'} · SL ${detail.soLuong}`} — {money(detail.thanhTien)}</p>
            </li>)}</ol>}
          </section>}
          <h2>Đề xuất ban đầu</h2><div className="table">
            {!plans.length && <p className="muted">Hệ thống chưa ghép được điểm phù hợp — Sale sẽ xử lý.</p>}
            {plans.map((plan) => <article className="panel" key={plan.maDeXuat}>
              <header className="panel-head"><h3>{plan.thuTuPhuongAn}. {plan.tenPhuongAn}</h3><span className="badge">{({ DeXuat: 'Đề xuất', DaChon: 'Đã chọn', KhongChon: 'Không chọn' })[trim(plan.trangThai)] || plan.trangThai}</span></header>
              <p><b>{money(plan.tongTienDuKien)}</b> (dự kiến) · Mã {plan.maDeXuat}</p><p>{plan.ghiChu}</p>
              <ol>{[...(plan.chiTiets || [])].sort((a, b) => a.ngayThu - b.ngayThu || a.thuTuTrongNgay - b.thuTuTrongNgay).map((detail) => <li key={detail.maChiTiet}>
                <b>Ngày {detail.ngayThu} · Mục {detail.thuTuTrongNgay}: {detail.mota || detail.maDthamQuan || 'Điểm tham quan'}</b>
                <p>{detail.maSanPham || 'Không kèm sản phẩm'} · SL {detail.soLuong} — {money(detail.thanhTien)}</p>
              </li>)}</ol>
            </article>)}
          </div>
        </>}
      </>}
        </ExpandRecord>)}
        {!items.length && <p className="muted">Chưa có yêu cầu thiết kế.</p>}
      </div>}
    </div>
  );
}
