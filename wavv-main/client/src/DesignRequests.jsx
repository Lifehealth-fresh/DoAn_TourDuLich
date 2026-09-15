import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import * as api from './api';
import CurrentDesignSchedule from './CurrentDesignSchedule.jsx';
import DesignChat from './DesignChat.jsx';

const trim = (value) => String(value ?? '').trim();
const money = (value) => `${Number(value || 0).toLocaleString('vi-VN')} đ`;
const dateText = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : 'Chưa cập nhật';
const itemsOf = (response) => Array.isArray(response.data) ? response.data : response.data?.items || [];
const labels = { Moi: 'Mới gửi', Huy: 'Đã hủy', DangThietKe: 'Đang thiết kế', CanChinhSua: 'Cần chỉnh sửa', ChoKhachXacNhan: 'Chờ bạn xác nhận', ChoDuyet: 'Chờ duyệt', DaDuyet: 'Đã duyệt' };
const statusLabel = (value) => labels[trim(value)] || trim(value) || '—';

export function DesignRequestsPage() {
  const [items, setItems] = useState([]);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [refresh, setRefresh] = useState(0);
  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState('');
  const pageSize = 10;

  useEffect(() => {
    let active = true;
    setLoading(true); setListError('');
    api.designRequests({ page, pageSize }).then((response) => {
      if (!active) return;
      setItems(itemsOf(response));
      setTotal(response.data.totalCount ?? itemsOf(response).length);
    }).catch((err) => { if (active) setListError(api.errorMessage(err, 'Không tải được yêu cầu của bạn.')); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [page, refresh]);

  return (
    <section className="page-section" style={{ overflowWrap: 'anywhere' }}>
      <div className="page-heading"><div><span className="stamp">Hành trình của riêng bạn</span><h1>Tự thiết kế <em>chuyến đi</em></h1></div></div>
      <p>Trò chuyện với trợ lý ANAM: chọn tỉnh xuất phát / đến, giờ đi–về, ngân sách. Hệ thống ghép 3 lịch từ CSDL (không bịa khách sạn).</p>
      <DesignChat />
      <div className="page-heading"><h2>Yêu cầu của tôi</h2><button className="outline-button" disabled={loading} onClick={() => setRefresh((value) => value + 1)}>Tải lại danh sách</button></div>
      {listError && <div className="form-error" role="alert">{listError}</div>}
      {loading ? <p role="status">Đang tải yêu cầu…</p> : !listError && <>
        <div className="booking-list">{items.map((item) => <Link className="booking-row" key={item.maYeuCau} to={`/tu-thiet-ke/${encodeURIComponent(trim(item.maYeuCau))}`}>
          <div><b>{item.maYeuCau}</b><p>{item.diemDenMongMuon || 'Chưa chọn điểm đến'}</p></div>
          <span>Ngày đi: {dateText(item.ngayDuKienDi)}</span><span className="status">{statusLabel(item.trangThai)}</span><span>Xem đề xuất →</span>
        </Link>)}</div>
        {!items.length && <div className="empty-state">Bạn chưa có yêu cầu thiết kế nào.</div>}
        {total > pageSize && <div className="doc-form-actions">
          <button className="outline-button" disabled={page === 1} onClick={() => setPage((value) => value - 1)}>Trang trước</button>
          <span>Trang {page}/{Math.ceil(total / pageSize)}</span>
          <button className="outline-button" disabled={page * pageSize >= total} onClick={() => setPage((value) => value + 1)}>Trang sau</button>
        </div>}
      </>}
    </section>
  );
}

export function DesignRequestDetailPage() {
  const { id } = useParams();
  return <DesignRequestDetail key={id} id={id} />;
}

function DesignRequestDetail({ id }) {
  const [request, setRequest] = useState(null);
  const [proposals, setProposals] = useState([]);
  const [schedule,setSchedule] = useState(null);
  const [refresh, setRefresh] = useState(0);
  const [loading, setLoading] = useState(true);
  const [choosing, setChoosing] = useState('');
  const [loadError, setLoadError] = useState('');
  const [error, setError] = useState('');
  const [ok, setOk] = useState('');

  useEffect(() => {
    let active = true;
    setLoading(true); setLoadError('');
    Promise.all([api.designRequestDetail(id), api.designProposals(id)]).then(async ([detail, plans]) => {
      const current = trim(detail.data.maTourTao) ? (await api.currentDesignSchedule(id)).data : null;
      if (active) {
        setRequest(current ? {...detail.data,trangThai:current.trangThai,lyDo:current.lyDo,nguonLyDo:current.nguonLyDo} : detail.data);
        setProposals(itemsOf(plans)); setSchedule(current);
      }
    }).catch((err) => { if (active) setLoadError(api.errorMessage(err, 'Không tải được chi tiết và đề xuất.')); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [id, refresh]);

  const choose = async (proposalId) => {
    if (choosing || loading) return;
    setChoosing(proposalId); setError(''); setOk('');
    try {
      await api.chooseProposal(id, proposalId);
      setOk('Đã chọn đề xuất. Sale sẽ sửa lịch và gửi bạn xác nhận trước khi Admin duyệt.');
      setLoading(true); setRefresh((value) => value + 1);
    } catch (err) {
      setError(api.errorMessage(err, 'Không chọn được đề xuất.'));
      if (err.response?.status === 409) { setLoading(true); setRefresh((value) => value + 1); }
    } finally { setChoosing(''); }
  };
  const respond = async (lyDo) => {
    if (choosing || loading || request?.trangThai!=='ChoKhachXacNhan') return;
    if (lyDo===null && !window.confirm('Bạn đồng ý với lịch trình và giá hiện tại?')) return;
    setChoosing(lyDo===null?'agree':'revise');setError('');setOk('');
    try {
      if(lyDo===null)await api.agreeDesignSchedule(id);else await api.requestDesignRevision(id,lyDo);
      setOk(lyDo===null?'Đã đồng ý lịch. Yêu cầu đang chờ Admin duyệt.':'Đã gửi yêu cầu chỉnh lại cho Admin/Sale.');
      setLoading(true);setRefresh(x=>x+1);
    } catch(err) { setError(api.errorMessage(err));if(err.response?.status===409){setLoading(true);setRefresh(x=>x+1);} }
    finally {setChoosing('');}
  };
  const canChoose = trim(request?.trangThai) === 'Moi' && !trim(request?.maTourTao);

  return (
    <section className="page-section" style={{ overflowWrap: 'anywhere' }}>
      <Link className="text-button" to="/tu-thiet-ke">← Yêu cầu của tôi</Link>
      <div className="page-heading"><div><span className="stamp">Hành trình may đo</span><h1>Chi tiết <em>yêu cầu</em></h1><p>Mã yêu cầu: <b>{id}</b></p></div>
        <button className="outline-button" disabled={loading || Boolean(choosing)} onClick={() => { setError(''); setRefresh((value) => value + 1); }}>Tải lại lịch và đề xuất</button>
      </div>
      {ok && <div className="success-message" role="status">{ok}</div>}
      {error && <div className="form-error" role="alert">{error}</div>}
      {loadError && <div className="form-error" role="alert">{loadError}</div>}
      {loading ? <p role="status">Đang tải chi tiết…</p> : !loadError && request && <>
        <article className="design-form">
          <h2>{request.diemDenMongMuon || 'Chưa chọn điểm đến'}</h2><span className="status">{statusLabel(request.trangThai)}</span>
          <dl className="detail-summary">
            <dt>Ngày dự kiến đi</dt><dd>{dateText(request.ngayDuKienDi)}</dd>
            <dt>Thời gian</dt><dd>{request.soNgay ?? '—'} ngày</dd>
            <dt>Hành khách</dt><dd>{request.soNguoiLon} người lớn · {request.soTreEm} trẻ em</dd>
            <dt>Ngân sách</dt><dd>{request.nganSachDuKien == null ? 'Chưa cập nhật' : money(request.nganSachDuKien)}</dd>
            <dt>Sở thích / ghi chú</dt><dd>{request.soThichGhiChu || 'Không có'}</dd>
            {trim(request.maTourTao) && <><dt>Tour đã tạo</dt><dd>{trim(request.maTourTao)}</dd></>}
          </dl>
          {request.lyDo && <p className="notice">{request.nguonLyDo==='KhachHang'?'Bạn đã gửi yêu cầu chỉnh: ':'Lý do Admin cần chỉnh / từ chối: '}{request.lyDo}</p>}
          {trim(request.trangThai) === 'DaDuyet' && trim(request.maTourTao) && <>
            <p>Tour đã được duyệt. Xem lịch trình, giá và lịch khởi hành cuối cùng trước khi đặt.</p>
            <Link className="primary-button" to={`/tour/${encodeURIComponent(trim(request.maTourTao))}`}>Đặt tour này</Link>
          </>}
        </article>
        {schedule&&<CurrentDesignSchedule schedule={schedule} busy={Boolean(choosing)} onRespond={respond}/>}
        <h2>Đề xuất lịch trình ban đầu</h2>
        <p>Sale có thể điều chỉnh theo thỏa thuận. Chọn đề xuất chưa phải là đặt tour hay thanh toán.</p>
        {!proposals.length && <div className="empty-state">Hệ thống chưa ghép được điểm phù hợp — Sale sẽ xử lý.</div>}
        <div className="proposal-grid">{proposals.map((proposal) => {
          const state = trim(proposal.trangThai);
          const details = [...(proposal.chiTiets || [])].sort((a, b) => a.ngayThu - b.ngayThu || a.thuTuTrongNgay - b.thuTuTrongNgay);
          const days = [...new Set(details.map((detail) => detail.ngayThu))];
          return <article className={`proposal-card${state === 'DaChon' ? ' chosen' : ''}`} key={proposal.maDeXuat}>
            <span className="proposal-label">Phương án {proposal.thuTuPhuongAn} · {({ DeXuat: 'Đề xuất', DaChon: 'Đã chọn', KhongChon: 'Không chọn' })[state] || state}</span>
            <h2>{proposal.tenPhuongAn}</h2><b>{money(proposal.tongTienDuKien)}</b><p>{proposal.ghiChu}</p>
            <div className="proposal-days">{days.map((day) => <section className="day-card" key={day}>
              <header><b>Ngày {day}</b></header>
              <ol>{details.filter((detail) => detail.ngayThu === day).map((detail) => <li key={detail.maChiTiet}>
                <strong>{detail.gioBatDau ? `${detail.gioBatDau} · ` : ''}{detail.laKhachSan ? `Khách sạn · ${detail.tenDoiTac || ''} · ${detail.tenSanPham}` : (detail.mota || trim(detail.maDthamQuan) || 'Điểm tham quan')}</strong>
                <p>{detail.laKhachSan
                  ? `${money(detail.donGia)} / đêm · SL ${detail.soLuong} — ${money(detail.thanhTien)}`
                  : (trim(detail.maSanPham) ? `Dịch vụ ${trim(detail.tenSanPham || detail.maSanPham)} · SL ${detail.soLuong} × ${money(detail.donGia)}` : 'Chưa kèm dịch vụ đối tác') + ` — ${money(detail.thanhTien)}`}</p>
              </li>)}</ol>
            </section>)}</div>
            {canChoose && state === 'DeXuat' && <button className="primary-button full" disabled={Boolean(choosing)} onClick={() => choose(proposal.maDeXuat)}>
              {choosing === proposal.maDeXuat ? 'Đang chọn…' : 'Chọn đề xuất'}
            </button>}
          </article>;
        })}</div>
      </>}
    </section>
  );
}
