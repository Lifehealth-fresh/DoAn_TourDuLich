import { useEffect, useState } from 'react';
import * as api from './api';

export default function TourReviewForm({ tourId, ownReview, onSaved }) {
  const [eligible, setEligible] = useState(null);
  const [stars, setStars] = useState(ownReview?.saoDanhGia || 0);
  const [hover, setHover] = useState(0);
  const [comment, setComment] = useState(ownReview?.nhanXet || '');
  const [savedId, setSavedId] = useState(ownReview?.maDanhGiaTour || '');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  useEffect(() => {
    setStars(ownReview?.saoDanhGia || 0);
    setComment(ownReview?.nhanXet || '');
    if (ownReview?.maDanhGiaTour) setSavedId(ownReview.maDanhGiaTour);
  }, [ownReview]);
  useEffect(() => {
    let active = true;
    const check = async () => {
      try {
        // A completed booking may be older than the first page of the customer's tickets.
        for (let page = 1; active; page++) {
          const response = await api.bookings({ page, pageSize: 100 });
          if (!active) return;
          const data = response.data;
          const rows = Array.isArray(data) ? data : data.items || [];
          if (rows.some((row) => String(row.maTour || '').trim() === String(tourId).trim() &&
              String(row.trangThai || '').trim() === 'HoanThanh')) { setEligible(true); return; }
          if (!rows.length || Array.isArray(data) || page * (data.pageSize || 100) >= (data.totalCount ?? rows.length)) break;
        }
        if (active) setEligible(false);
      } catch (e) {
        if (active) { setEligible(false); setError(api.errorMessage(e, 'Không kiểm tra được điều kiện đánh giá.')); }
      }
    };
    check();
    return () => { active = false; };
  }, [tourId]);

  const submit = async (event) => {
    event.preventDefault();
    if (busy) return;
    if (!stars) { setError('Hãy chọn từ 1 đến 5 sao.'); return; }
    setBusy(true); setError(''); setMessage('');
    try {
      const data = { saoDanhGia: stars, nhanXet: comment.trim() };
      if (savedId) await api.updateTourReview(savedId, data);
      else {
        const response = await api.createTourReview({ maTour: tourId, ...data });
        // Retain the ID if reloading fails, so retrying never creates a duplicate review.
        setSavedId(response.data.maDanhGiaTour);
      }
      setMessage(savedId ? 'Đã cập nhật đánh giá.' : 'Đã gửi đánh giá. Cảm ơn bạn!');
      await onSaved();
    } catch (e) { setError(api.errorMessage(e, 'Không gửi được đánh giá hoặc tải lại danh sách.')); }
    finally { setBusy(false); }
  };

  return <div className="tour-review-form" style={{ marginTop: 24 }}>
    {error && <div className="form-error" role="alert">{error}</div>}
    {message && <div className="success-message" role="status">{message}</div>}
    {eligible === null ? <p>Đang kiểm tra điều kiện đánh giá…</p> : !eligible
      ? <p className="muted">Đánh giá khi tour đã hoàn thành</p>
      : <form onSubmit={submit} aria-label="Đánh giá tour">
        <h3>{savedId ? 'Đánh giá của bạn' : 'Chia sẻ cảm nhận về tour'}</h3>
        <div role="group" aria-label="Chọn số sao" style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }} onMouseLeave={() => setHover(0)}>
          {[1, 2, 3, 4, 5].map((value) => <button key={value} type="button" disabled={busy}
            aria-label={value + ' sao'} aria-pressed={stars === value}
            onMouseEnter={() => setHover(value)} onFocus={() => setHover(value)} onBlur={() => setHover(0)}
            onClick={() => setStars(value)}
            style={{ fontSize: 32, lineHeight: 1, background: 'transparent', border: 'none', boxShadow: 'none',
              padding: 6, cursor: busy ? 'wait' : 'pointer', color: value <= (hover || stars) ? '#e4a400' : '#b5b5b5' }}>★</button>)}
        </div>
        <p aria-live="polite">{stars ? stars + '/5 sao' : 'Chọn từ 1 đến 5 sao'}</p>
        <label style={{ display: 'grid', gap: 8 }}>Nhận xét
          <textarea aria-label="Nhận xét" rows={4} style={{ width: '100%', maxWidth: '100%' }}
            disabled={busy} value={comment} onChange={(event) => setComment(event.target.value)} />
        </label>
        <button className="primary-button" style={{ marginTop: 12 }} disabled={busy}>
          {busy ? 'Đang gửi…' : savedId ? 'Lưu đánh giá' : 'Gửi đánh giá'}
        </button>
      </form>}
  </div>;
}
