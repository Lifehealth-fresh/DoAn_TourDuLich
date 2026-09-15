import { useEffect, useState } from 'react';
import * as api from './api';

export default function TourReviewForm({ tourId, ownReview, onSaved, internal = false }) {
  const [eligible, setEligible] = useState(null);
  const [stars, setStars] = useState(ownReview?.saoDanhGia || 0);
  const [hover, setHover] = useState(0);
  const [comment, setComment] = useState(ownReview?.nhanXet || '');
  const [savedId, setSavedId] = useState(ownReview?.maDanhGiaTour || '');
  const [media, setMedia] = useState(ownReview?.media || []);
  const [files, setFiles] = useState([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const canEdit = !savedId || ownReview?.coTheSua !== false;
  const deadline = ownReview?.hanSua;

  useEffect(() => {
    setStars(ownReview?.saoDanhGia || 0);
    setComment(ownReview?.nhanXet || '');
    setMedia(ownReview?.media || []);
    if (ownReview?.maDanhGiaTour) setSavedId(ownReview.maDanhGiaTour);
  }, [ownReview]);

  useEffect(() => {
    let active = true;
    const check = async () => {
      try {
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
    if (busy || !canEdit) return;
    if (!stars) { setError('Hãy chọn từ 1 đến 5 sao.'); return; }
    setBusy(true); setError(''); setMessage('');
    try {
      const uploaded = [...media];
      for (const file of files) {
        const r = await api.uploadReviewMedia(file);
        uploaded.push({ url: r.data.url, loaiMedia: r.data.loaiMedia });
      }
      if (uploaded.length > 6) { setError('Tối đa 6 ảnh/video mỗi bài.'); setBusy(false); return; }
      const data = { saoDanhGia: stars, nhanXet: comment.trim(), mediaUrls: uploaded };
      if (savedId) await api.updateTourReview(savedId, data);
      else {
        const response = await api.createTourReview({ maTour: tourId, ...data });
        setSavedId(response.data.maDanhGiaTour);
      }
      setFiles([]);
      setMessage(internal
        ? (savedId ? 'Đã cập nhật nhận xét nội bộ.' : 'Đã gửi nhận xét. Chỉ bộ phận vận hành xem được.')
        : (savedId ? 'Đã cập nhật đánh giá.' : 'Đã gửi đánh giá. Cảm ơn bạn!'));
      await onSaved();
    } catch (e) { setError(api.errorMessage(e, 'Không gửi được đánh giá hoặc tải lại danh sách.')); }
    finally { setBusy(false); }
  };

  const filled = hover || stars;
  return <div className="tour-review-form" style={{ marginTop: 24 }}>
    {error && <div className="form-error" role="alert">{error}</div>}
    {message && <div className="success-message" role="status">{message}</div>}
    {eligible === null ? <p>Đang kiểm tra điều kiện đánh giá…</p> : !eligible
      ? <p className="muted">{internal
        ? 'Gửi nhận xét nội bộ sau khi hoàn thành chuyến đi tự thiết kế.'
        : 'Đánh giá khi tour đã hoàn thành'}</p>
      : <form onSubmit={submit} aria-label={internal ? 'Nhận xét nội bộ' : 'Đánh giá tour'}>
        <h3>{internal
          ? (savedId ? 'Nhận xét nội bộ của bạn' : 'Gửi nhận xét nội bộ')
          : (savedId ? 'Đánh giá của bạn' : 'Chia sẻ cảm nhận về tour')}</h3>
        {internal && <p className="muted">Tour tự thiết kế không đăng công khai. Chỉ admin/sale xem được bài này.</p>}
        {savedId && deadline && <p className="muted">{canEdit
          ? `Có thể sửa đến ${new Date(deadline).toLocaleString('vi-VN')} (5 ngày sau khi gửi).`
          : 'Hết hạn 5 ngày, không sửa được nữa.'}</p>}
        <div role="group" aria-label="Chọn số sao" style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }} onMouseLeave={() => setHover(0)}>
          {[1, 2, 3, 4, 5].map((value) => <button key={value} type="button" disabled={busy || !canEdit}
            aria-label={value + ' sao'} aria-pressed={stars === value}
            onMouseEnter={() => canEdit && setHover(value)} onFocus={() => canEdit && setHover(value)} onBlur={() => setHover(0)}
            onClick={() => canEdit && setStars(value)}
            style={{ fontSize: 32, lineHeight: 1, background: 'transparent', border: 'none', boxShadow: 'none',
              padding: 6, cursor: busy || !canEdit ? 'default' : 'pointer', color: value <= filled ? '#e4a400' : '#d0d0d0' }}>★</button>)}
        </div>
        <p aria-live="polite">{stars ? stars + '/5 sao' : 'Chọn từ 1 đến 5 sao'}</p>
        <label style={{ display: 'grid', gap: 8 }}>Nhận xét
          <textarea aria-label="Nhận xét" rows={4} style={{ width: '100%', maxWidth: '100%' }}
            disabled={busy || !canEdit} value={comment} onChange={(event) => setComment(event.target.value)} />
        </label>
        <label style={{ display: 'grid', gap: 8, marginTop: 12 }}>Ảnh / video (không bắt buộc, tối đa 6)
          <input type="file" accept="image/jpeg,image/png,image/webp,video/mp4,video/webm" multiple
            disabled={busy || !canEdit}
            onChange={(e) => setFiles([...e.target.files].slice(0, 6))} />
        </label>
        {(media.length || files.length) ? (
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginTop: 8 }}>
            {media.map((m, i) => m.loaiMedia === 'Video'
              ? <video key={'m' + i} src={m.url} controls style={{ maxWidth: 180, maxHeight: 110 }} />
              : <img key={'m' + i} src={m.url} alt="" style={{ height: 88 }} />)}
            {files.map((f, i) => <span key={'f' + i} className="muted">{f.name}</span>)}
          </div>
        ) : null}
        {canEdit && <button className="primary-button" style={{ marginTop: 12 }} disabled={busy}>
          {busy ? 'Đang gửi…' : savedId ? 'Lưu' : (internal ? 'Gửi nhận xét nội bộ' : 'Gửi đánh giá')}
        </button>}
      </form>}
  </div>;
}
