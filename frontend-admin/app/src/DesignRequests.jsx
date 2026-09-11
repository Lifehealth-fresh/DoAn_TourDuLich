import { useEffect, useState } from 'react';
import * as api from './api';
import { Notice } from './components';

const trim = (value) => String(value ?? '').trim();
const money = (value) => `${Number(value || 0).toLocaleString('vi-VN')} đ`;
const dateText = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : '—';
const itemsOf = (response) => Array.isArray(response.data) ? response.data : response.data?.items || [];
const labels = { Moi: 'Mới gửi', Huy: 'Đã hủy', DangThietKe: 'Đang thiết kế', CanChinhSua: 'Cần chỉnh sửa', ChoDuyet: 'Chờ duyệt', DaDuyet: 'Đã duyệt' };
const statusLabel = (value) => labels[trim(value)] || trim(value) || '—';
const toRow = (item = {}) => ({ ngayThu: item.ngayThu ?? 1, thuTuTrongNgay: item.thuTuTrongNgay ?? 1, maDthamQuan: trim(item.maDthamQuan), maSanPham: trim(item.maSanPham), soLuong: item.soLuong ?? 1, mota: item.mota || '' });

export function DesignRequests() {
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState('');
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
  const selected = items.find((item) => item.maYeuCau === selectedId);
  const tourId = trim(selected?.maTourTao);
  const state = trim(selected?.trangThai);
  const blocked = Boolean(busy || loading || loadingDetails || listError || detailError);
  const canGenerate = !blocked && !editing && state === 'Moi';
  const canEdit = !blocked && Boolean(tourId) && ['DangThietKe', 'CanChinhSua'].includes(state);
  const canSubmit = canEdit && !editing;
  const canApprove = !blocked && !editing && Boolean(tourId) && state === 'ChoDuyet';

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
      Promise.all([api.proposals(selectedId), tourId ? api.designSchedule(tourId) : Promise.resolve(null)]).then(([proposals, current]) => {
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
        setOk(`Đã sinh đề xuất cho ${selectedId}. Khách có thể xem và chọn phương án.`);
      } else if (action === 'save') {
        const result = await api.editDesignSchedule(selectedId, payload);
        setEditing(false); setOk(`Đã lưu lịch trình. Giá tour hiện tại: ${money(result.data.giaTour)}.`);
      } else if (action === 'submit') {
        await api.submitDesignForApproval(selectedId); setOk(`Đã gửi duyệt yêu cầu ${selectedId}.`);
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
    act('save', { chiTiets });
  };
  const actionButton = (action, caption, allowed) => <button disabled={!allowed} style={{ opacity: allowed ? 1 : 0.5 }} onClick={() => act(action)}>{busy === action ? 'Đang xử lý…' : caption}</button>;

  return (
    <div style={{ minWidth: 0, overflowWrap: 'anywhere' }}>
      <header className="panel-head"><h1>Yêu cầu thiết kế</h1><button disabled={Boolean(busy) || loading || loadingDetails || editing} onClick={() => { setError(''); reload(); }}>Tải lại danh sách và đề xuất</button></header>
      <p>Khách gửi → hệ thống sinh đề xuất → khách chọn → Sale sửa nếu cần → gửi duyệt → duyệt → khách đặt/trả.</p>
      <p className="notice">Thỏa thuận với khách ngoài hệ thống rồi mới duyệt.</p>
      <Notice error={listError} /><Notice error={error} />{ok && <div className="notice ok" role="status">{ok}</div>}
      {loading ? <p role="status">Đang tải yêu cầu…</p> : !listError && <div className="table">
        {items.map((item) => <button key={item.maYeuCau} className={`row booking-row${selectedId === item.maYeuCau ? ' on' : ''}`} aria-pressed={selectedId === item.maYeuCau} disabled={Boolean(busy) || editing} onClick={() => { if (selectedId !== item.maYeuCau) select(item); }}>
          <b>{item.maYeuCau}</b><span>{item.diemDenMongMuon || 'Chưa chọn điểm đến'}</span><span>Ngày đi: {dateText(item.ngayDuKienDi)}</span>
          <span>{item.soNgay ?? '—'} ngày</span><span>{item.nganSachDuKien == null ? 'Chưa có ngân sách' : money(item.nganSachDuKien)}</span><span className="badge">{statusLabel(item.trangThai)}</span>
        </button>)}
        {!items.length && <p className="muted">Chưa có yêu cầu thiết kế.</p>}
      </div>}
      {selected && <section className="panel">
        <header className="panel-head"><h2>Yêu cầu {selected.maYeuCau}</h2><span className="badge">{statusLabel(state)}</span></header>
        <p>Khách hàng: {selected.maUser} · Ngày gửi: {dateText(selected.ngayGui)}</p>
        {tourId && <p>Tour đã tạo: <b>{tourId}</b></p>}
        {selected.lyDoTuChoiBoiSale && <p className="notice">Lý do cần chỉnh sửa: {selected.lyDoTuChoiBoiSale}</p>}
        {state === 'Moi' && <p>Sinh lại sẽ thay thế các phương án chưa được khách chọn. Chỉ thực hiện khi cần xử lý lại.</p>}
        <div className="inline">
          {actionButton('generate', plans.length ? 'Sinh lại đề xuất' : 'Sinh đề xuất', canGenerate)}
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
              <p>Nhập mã điểm tham quan hoặc mã sản phẩm có trong hệ thống. Lưu thay đổi hoặc hủy chỉnh sửa trước khi gửi duyệt.</p>
              <div className="table">{rows.map((row, index) => <fieldset className="panel" key={index} disabled={Boolean(busy)}>
                <legend>Dòng {index + 1}</legend>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: 12 }}>
                  <label>Ngày thứ<input style={{ width: '100%' }} type="number" required min="1" max={Math.min(30, Math.max(1, selected.soNgay || 1))} step="1" value={row.ngayThu} onChange={(event) => changeRow(index, 'ngayThu', event.target.value)} /></label>
                  <label>Thứ tự trong ngày<input style={{ width: '100%' }} type="number" required min="1" max="2147483647" step="1" value={row.thuTuTrongNgay} onChange={(event) => changeRow(index, 'thuTuTrongNgay', event.target.value)} /></label>
                  <label>Mã điểm tham quan<input style={{ width: '100%' }} maxLength={20} value={row.maDthamQuan} onChange={(event) => changeRow(index, 'maDthamQuan', event.target.value)} /></label>
                  <label>Mã sản phẩm<input style={{ width: '100%' }} maxLength={20} value={row.maSanPham} onChange={(event) => changeRow(index, 'maSanPham', event.target.value)} /></label>
                  <label>Số lượng<input style={{ width: '100%' }} type="number" required min="1" max="2147483647" step="1" value={row.soLuong} onChange={(event) => changeRow(index, 'soLuong', event.target.value)} /></label>
                  <div><label htmlFor={`design-description-${index}`}>Mô tả</label><textarea id={`design-description-${index}`} style={{ width: '100%' }} rows={2} value={row.mota} onChange={(event) => changeRow(index, 'mota', event.target.value)} /></div>
                </div>
                <button type="button" className="danger" onClick={() => setRows((current) => current.filter((_, i) => i !== index))}>Xóa dòng {index + 1}</button>
              </fieldset>)}</div>
              <div className="inline">
                <button type="button" disabled={Boolean(busy)} onClick={() => setRows((current) => [...current, toRow({ ngayThu: current.at(-1)?.ngayThu || 1, thuTuTrongNgay: Number(current.at(-1)?.thuTuTrongNgay || 0) + 1 })])}>Thêm dòng</button>
                <button disabled={!canEdit}>{busy === 'save' ? 'Đang lưu…' : 'Lưu lịch trình'}</button>
                <button type="button" disabled={Boolean(busy)} onClick={() => { setEditing(false); setRows((schedule.lichTrinh || []).map(toRow)); setError(''); }}>Hủy chỉnh sửa</button>
              </div>
            </form> : <ol>{(schedule.lichTrinh || []).map((detail) => <li key={detail.maLichTrinh}>
              <b>Ngày {detail.ngayThu} · Mục {detail.thuTuTrongNgay}: {detail.mota || detail.tenDiaDanh || detail.tenSanPham}</b>
              <p>{detail.maDthamQuan || '—'} · {detail.maSanPham || 'Không kèm sản phẩm'} · SL {detail.soLuong} — {money(detail.thanhTien)}</p>
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
      </section>}
      {!selected && !loading && items.length > 0 && <p className="muted">Chọn một yêu cầu để xem và xử lý.</p>}
    </div>
  );
}
