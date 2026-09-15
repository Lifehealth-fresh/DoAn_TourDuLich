import { useEffect, useState } from 'react';
import * as api from './api';
import { ExpandRecord, Notice } from './components';

const stars = (n) => '★'.repeat(Number(n) || 0) + '☆'.repeat(Math.max(0, 5 - Number(n || 0)));
const pct = (v) => `${Math.round(Number(v || 0) * 1000) / 10}%`;
const dateText = (value) => (value ? new Date(value).toLocaleString('vi-VN') : '—');

export default function ReviewsManagement() {
  const [q, setQ] = useState('');
  const [items, setItems] = useState([]);
  const [open, setOpen] = useState('');
  const [detail, setDetail] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const load = (query) => {
    setBusy(true);
    setError('');
    api.reviewSearch({ q: query || undefined, pageSize: 50 })
      .then((r) => setItems(r.data?.items || []))
      .catch((e) => setError(api.errorMessage(e, 'Không tải được đánh giá.')))
      .finally(() => setBusy(false));
  };

  useEffect(() => { load(''); }, []);

  const openTour = async (maTour) => {
    if (open === maTour) { setOpen(''); return; }
    setOpen(maTour);
    setBusy(true);
    setError('');
    try {
      const r = await api.reviewTour(maTour);
      setDetail(r.data);
    } catch (e) {
      setDetail(null);
      setError(api.errorMessage(e, 'Không tải được bài đánh giá của tour.'));
    } finally { setBusy(false); }
  };

  const submit = (event) => {
    event.preventDefault();
    load(q.trim());
  };

  return (
    <>
      <h1>Đánh giá nhận xét</h1>
      <p className="muted">Chỉ xem. Không thêm, sửa hay xóa bài đánh giá của khách. Tour tự thiết kế là phản hồi nội bộ, không hiện trên website khách.</p>
      <Notice error={error} />
      <form className="toolbar" onSubmit={submit} style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 16 }}>
        <input
          value={q}
          onChange={(e) => setQ(e.target.value)}
          placeholder="Tên tour, mã tour, tên khách, mã khách"
          aria-label="Tìm đánh giá"
          style={{ minWidth: 260, flex: 1 }}
        />
        <button className="primary" disabled={busy} type="submit">{busy ? 'Đang tìm…' : 'Tìm'}</button>
      </form>
      {items.length === 0 ? <p className="muted">Chưa có tour nào khớp.</p> : items.map((row) => (
        <ExpandRecord
          key={row.maTour}
          open={open === row.maTour}
          summary={(
            <button type="button" className="row" onClick={() => openTour(row.maTour)}>
              <b>{row.maTour}</b>
              <span>{row.tenTour}</span>
              <span>{row.diemTrungBinh != null ? `${row.diemTrungBinh}/5` : 'Chưa có điểm'} · {row.soDanhGia} bài</span>
              <em className={'badge ' + (row.noiBo ? 'wait' : 'ok')}>{row.noiBo ? 'Nội bộ' : 'Công khai'}</em>
            </button>
          )}
        >
          {detail?.maTour === row.maTour ? (
            <div className="review-staff-list">
              <p className="muted">Tích cực {pct(row.tyLeTichCuc)} · Tiêu cực {pct(row.tyLeTieuCuc)} · Công khai {row.soCongKhai} · Nội bộ {row.soNoiBo}</p>
              {detail.danhGias?.length ? detail.danhGias.map((item) => (
                <article className="review-staff-card" key={item.maDanhGiaTour}>
                  <header>
                    <strong>{item.tenKhachHang}</strong>
                    <span>{item.maKhachHang || '—'}</span>
                    <time>{dateText(item.thoiGian)}</time>
                    <b className="star-line" aria-label={`${item.saoDanhGia} sao`}>{stars(item.saoDanhGia)}</b>
                    {!item.congKhai && <em className="badge wait">Nội bộ</em>}
                  </header>
                  <p>{item.nhanXet || 'Không có nhận xét.'}</p>
                </article>
              )) : <p className="muted">Tour này chưa có bài đánh giá.</p>}
            </div>
          ) : <p className="muted">Đang mở…</p>}
        </ExpandRecord>
      ))}
    </>
  );
}
