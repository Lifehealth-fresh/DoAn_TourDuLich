import { useEffect, useMemo, useState } from 'react';
import {
  BrowserRouter, Link, Navigate, Outlet, Route, Routes,
  useLocation, useNavigate, useParams, useSearchParams,
} from 'react-router-dom';
import {
  FaBars, FaCalendarAlt, FaEnvelope, FaIdCard, FaMapMarkerAlt,
  FaPassport, FaPhoneAlt, FaPlane, FaSearch, FaStar, FaTimes, FaUserCircle,
} from 'react-icons/fa';
import * as api from './api';
import imgHaLong from './assets/vietnam/halong.jpg';
import imgHoiAn from './assets/vietnam/hoian.jpg';
import imgHaNoi from './assets/vietnam/hanoi.jpg';
import imgDaNang from './assets/vietnam/danang.jpg';
import imgSapa from './assets/vietnam/sapa.jpg';
import imgNinhBinh from './assets/vietnam/ninhbinh.jpg';
import imgHue from './assets/vietnam/hue.jpg';
import imgNhaTrang from './assets/vietnam/nhatrang.jpg';
import imgPhuQuoc from './assets/vietnam/phuquoc.jpg';
import imgDalat from './assets/vietnam/dalat.jpg';
import './App.css';

const fallbackImage = imgHaLong;

const money = (value) => `${Number(value || 0).toLocaleString('vi-VN')} đ`;
const read = (key) => { try { return JSON.parse(localStorage.getItem(key) || 'null'); } catch { return null; } };
const write = (key, value) => localStorage.setItem(key, JSON.stringify(value));
const token = () => localStorage.getItem('wavv_token');
const trim = (value) => (value == null ? '' : String(value).trim());
const itemsOf = (response) => (Array.isArray(response?.data) ? response.data : (response?.data?.items || []));
const dateText = (value) => (value ? new Date(value).toLocaleDateString('vi-VN') : 'Chưa cập nhật');
const isoDate = (value) => {
  if (!value) return '';
  const text = String(value);
  if (/^\d{4}-\d{2}-\d{2}/.test(text)) return text.slice(0, 10);
  const next = new Date(value);
  return Number.isNaN(next.getTime()) ? '' : next.toISOString().slice(0, 10);
};
const yearsOld = (value) => {
  if (!value) return null;
  const birth = new Date(value);
  if (Number.isNaN(birth.getTime())) return null;
  const now = new Date();
  let age = now.getFullYear() - birth.getFullYear();
  const month = now.getMonth() - birth.getMonth();
  if (month < 0 || (month === 0 && now.getDate() < birth.getDate())) age -= 1;
  return age;
};

const STATUS = {
  ChoXacNhan: 'Chờ xác nhận',
  DaXacNhan: 'Đã xác nhận',
  DaThanhToan: 'Đã thanh toán',
  HoanThanh: 'Hoàn thành',
  DaHuy: 'Đã hủy',
  Moi: 'Mới gửi',
  DangThietKe: 'Đang thiết kế',
  ChoDuyet: 'Chờ duyệt',
  DaDuyet: 'Đã duyệt',
  CanChinhSua: 'Cần chỉnh sửa',
};

const statusLabel = (value) => STATUS[trim(value)] || trim(value) || '—';

const guessRegion = (name = '') => {
  const text = name.toLowerCase();
  if (/hà nội|ha noi|hạ long|ha long|sa pa|sapa|fansipan|ninh bình|ninh binh|tràng an|trang an/.test(text)) return 'Miền Bắc';
  if (/đà nẵng|da nang|hội an|hoi an|huế|hue|sông hương|nha trang/.test(text)) return 'Miền Trung';
  if (/phú quốc|phu quoc|cần thơ|can tho|đà lạt|da lat|chợ nổi|miền tây/.test(text)) return 'Miền Nam';
  return 'Việt Nam';
};

const TOUR_IMAGES = {
  TOUR001: [imgHaLong, imgHaNoi, imgSapa],
  TOUR002: [imgHoiAn, imgDaNang, imgHue],
  TOUR003: [imgSapa, imgHaNoi, imgNinhBinh],
  TOUR004: [imgNinhBinh, imgHaNoi, imgSapa],
  TOUR005: [imgHue, imgHoiAn, imgDaNang],
  TOUR006: [imgNhaTrang, imgDaNang, imgPhuQuoc],
  TOUR007: [imgPhuQuoc, imgNhaTrang, imgHoiAn],
  TOUR008: [imgNinhBinh, imgHoiAn, imgDaNang],
  TOUR009: [imgDalat, imgSapa, imgNinhBinh],
};

const TOUR_EXTRA = {
  TOUR001: { tags: ['Di sản', 'Du thuyền', 'Hà Nội'], highlights: ['Ngủ đêm trên Vịnh Hạ Long', 'Phố cổ và ẩm thực đêm', 'Bình minh giữa vịnh'], includes: ['Du thuyền/khách sạn', 'Xe đưa đón', 'Bữa chính', 'Vé thắng cảnh', 'Hướng dẫn viên'], excludes: ['Vé máy bay', 'Chi tiêu cá nhân'] },
  TOUR002: { tags: ['Phố cổ', 'Biển', 'Ẩm thực'], highlights: ['Đèn lồng Hội An', 'Biển Mỹ Khê', 'Lớp nấu ăn'], includes: ['Khách sạn', 'Xe', 'Bữa chính', 'Vé điểm đến'], excludes: ['Vé máy bay'] },
  TOUR003: { tags: ['Núi', 'Sương', 'Tây Bắc'], highlights: ['Cáp treo Fansipan', 'Ruộng bậc thang', 'Bản Cát Cát'], includes: ['Khách sạn', 'Xe giường nằm', 'Vé cáp treo', 'Bữa chính'], excludes: ['Áo ấm', 'Đồ trekking thuê'] },
  TOUR004: { tags: ['Sông', 'Hang động', 'Gia đình'], highlights: ['Thuyền Tràng An', 'Tam Cốc mùa lúa', 'Chùa Bái Đính'], includes: ['Khách sạn', 'Thuyền', 'Vé danh thắng', 'Bữa chính'], excludes: ['Vé máy bay'] },
  TOUR005: { tags: ['Cố đô', 'Văn hóa', 'Chậm'], highlights: ['Đại Nội Huế', 'Thuyền sông Hương', 'Ẩm thực cung đình'], includes: ['Khách sạn', 'Vé Đại Nội', 'Thuyền chiều', 'Bữa chính'], excludes: ['Vé máy bay'] },
  TOUR006: { tags: ['Biển', 'Đảo', 'Gia đình'], highlights: ['Tắm biển Nha Trang', 'VinWonders Hòn Tre', 'Hải sản đêm'], includes: ['Khách sạn gần biển', 'Vé VinWonders', 'Xe', 'Bữa chính'], excludes: ['Đồ lặn thuê'] },
  TOUR007: { tags: ['Đảo', 'Hoàng hôn', 'Nghỉ dưỡng'], highlights: ['Bãi Sao', 'Sunset Sanato', 'Chợ đêm Dương Đông'], includes: ['Khách sạn', 'Xe sân bay', 'Xe tham quan', 'Bữa chính'], excludes: ['Vé máy bay'] },
  TOUR008: { tags: ['Sông nước', 'Miền Tây', 'Chợ nổi'], highlights: ['Chợ nổi Cái Răng', 'Vườn trái Phong Điền', 'Đờn ca tài tử'], includes: ['Khách sạn', 'Thuyền', 'Vườn trái', 'Bữa chính'], excludes: ['Vé máy bay'] },
  TOUR009: { tags: ['Cao nguyên', 'Hoa', 'Se lạnh'], highlights: ['Hồ Xuân Hương', 'Đồi chè Cầu Đất', 'Đêm sương Đà Lạt'], includes: ['Khách sạn trung tâm', 'Xe', 'Điểm check-in', 'Bữa chính'], excludes: ['Vé máy bay'] },
};

const pickImage = (item) => {
  const remote = item.avatarUrl || item.imageUrl || item.url;
  if (remote) return remote;
  const id = trim(item.maTour);
  return TOUR_IMAGES[id]?.[0] || fallbackImage;
};

const normalizeTour = (item) => {
  const id = trim(item.maTour);
  const extra = TOUR_EXTRA[id] || { tags: [], highlights: [], includes: [], excludes: [] };
  return {
    ...item,
    id,
    name: item.tenTour || 'Tour chưa đặt tên',
    destination: item.diemDen || item.tenTour || 'Việt Nam',
    region: item.khuVuc || item.tenKhuVuc || guessRegion(item.tenTour),
    price: item.giaTour || 0,
    duration: item.thoiGian || 1,
    image: pickImage(item),
    gallery: TOUR_IMAGES[id] || [pickImage(item)],
    rating: item.diemTrungBinh || 0,
    description: item.mota || item.moTa || 'Hành trình được chọn lọc, vừa vặn với nhịp sống của bạn.',
    terms: item.dieuKhoan || '',
    ...extra,
  };
};

function Brand() {
  return (
    <Link className="brand" to="/">
      <span className="brand-mark" aria-hidden="true">
        <svg viewBox="0 0 36 36">
          <circle cx="18" cy="18" r="16" fill="#c49a3c" stroke="#1c1612" strokeWidth="1.6" />
          <path d="M18 6 L20.2 15.2 L30 18 L20.2 20.8 L18 30 L15.8 20.8 L6 18 L15.8 15.2 Z" fill="#1c1612" />
        </svg>
      </span>
      <span className="brand-copy">
        <b>ANAM</b>
        <small>Lữ hành may đo</small>
      </span>
    </Link>
  );
}

function Layout() {
  const [open, setOpen] = useState(false);
  const user = read('wavv_user');
  const navigate = useNavigate();
  const logout = () => {
    localStorage.removeItem('wavv_user');
    localStorage.removeItem('wavv_token');
    navigate('/');
  };

  return (
    <div>
      <header className="site-header">
        <Brand />
        <button className="mobile-menu" onClick={() => setOpen(!open)}>{open ? <FaTimes /> : <FaBars />}</button>
        <nav className={open ? 'nav open' : 'nav'} onClick={() => setOpen(false)}>
          <Link to="/tours">Khám phá</Link>
          <Link to="/tim-tour">Tìm đúng gu</Link>
          {user && <Link to="/goi-y">Gợi ý AI</Link>}
          <Link to="/uu-dai">Ưu đãi</Link>
          {user ? (
            <>
              <Link to="/booking">Chuyến đi của tôi</Link>
              <Link to="/ho-so"><FaUserCircle /> {user.name}</Link>
              <button className="text-button" onClick={logout}>Đăng xuất</button>
            </>
          ) : (
            <>
              <Link className="outline-button" to="/dang-nhap">Đăng nhập</Link>
              <Link className="primary-button" to="/dang-ky">Đăng ký</Link>
            </>
          )}
        </nav>
      </header>
      <main><Outlet /></main>
      <footer className="footer">
        <div>
          <Brand />
          <p>Tour có sẵn và gợi ý đúng gu — không còn một khuôn cho tất cả.</p>
        </div>
        <div>
          <h4>Hành trình</h4>
          <Link to="/tours">Tour có sẵn</Link>
          <Link to="/tim-tour">Tìm đúng gu</Link>
          <Link to="/goi-y">Gợi ý AI</Link>
        </div>
        <div>
          <h4>Liên hệ</h4>
          <span>support@anam.travel</span>
          <span>1900 6868</span>
          <span>Đà Nẵng, Việt Nam</span>
        </div>
      </footer>
    </div>
  );
}

function Home() {
  const [items, setItems] = useState([]);
  const [error, setError] = useState('');

  useEffect(() => {
    api.tours()
      .then((r) => setItems(itemsOf(r).slice(0, 6).map(normalizeTour)))
      .catch((e) => setError(api.errorMessage(e, 'Không tải được tour nổi bật.')));
  }, []);

  return (
    <>
      <section className="hero">
        <div>
          <p className="stamp">Est. 2026 · May đo chuyến đi</p>
          <h1>Đi cho đúng<br /><em>gu của mình.</em></h1>
          <p className="lede">ANAM không bán tour một khuôn. Chọn lịch có sẵn, trả lời vài câu hỏi để tìm đúng gu, hoặc để AI gợi ý từ những gì bạn đã xem và đặt.</p>
          <div className="hero-actions">
            <Link className="primary-button large" to="/tours">Khám phá tour</Link>
            <Link className="outline-button large" to="/tim-tour">Tìm đúng gu</Link>
          </div>
        </div>
        <div className="poster-stack">
          <div className="poster one"><img src={imgHaNoi} alt="Hà Nội" /><span className="poster-label">Hà Nội</span></div>
          <div className="poster two"><img src={imgHoiAn} alt="Hội An" /><span className="poster-label">Hội An</span></div>
          <div className="poster three"><img src={imgHaLong} alt="Hạ Long" /><span className="poster-label">Hạ Long</span></div>
        </div>
      </section>

      <section className="strip">
        <p>01 / 03 · Không chỉ là chuyến đi. <b>Là câu chuyện bạn mang về.</b></p>
        <Link to="/uu-dai">Xem ưu đãi mùa này →</Link>
      </section>

      <section className="section">
        <div className="section-title">
          <div>
            <p className="stamp">Bản đồ cảm hứng</p>
            <h2>Ba miền,<br /><em>ba sắc màu.</em></h2>
          </div>
        </div>
        <div className="dest-grid">
          <Link className="dest-card" to="/tours?region=Miền Bắc"><img src={imgHaNoi} alt="Miền Bắc" /><span>Miền Bắc</span></Link>
          <Link className="dest-card" to="/tours?region=Miền Trung"><img src={imgHoiAn} alt="Miền Trung" /><span>Miền Trung</span></Link>
          <Link className="dest-card" to="/tours?region=Miền Nam"><img src={imgPhuQuoc} alt="Miền Nam" /><span>Miền Nam</span></Link>
        </div>
      </section>

      <section className="section">
        <div className="section-title">
          <div>
            <p className="stamp">Được yêu thích</p>
            <h2>Hành trình<br /><em>đáng nhớ.</em></h2>
          </div>
          <Link className="outline-button" to="/tours">Xem tất cả</Link>
        </div>
        {error && <div className="form-error">{error}</div>}
        {items.length ? <TourGrid items={items} /> : !error && <p>Đang tải tour...</p>}
      </section>

      <section className="section">
        <div className="section-title">
          <div>
            <p className="stamp">Cách ANAM làm việc</p>
            <h2>Ba bước,<br /><em>một chuyến đi vừa vặn.</em></h2>
          </div>
        </div>
        <div className="steps">
          <article className="step-card"><b>01</b><h3>Chọn hoặc lọc theo gu</h3><p>Lấy tour có sẵn, hoặc trả lời vài câu hỏi về miền, số ngày, ngân sách.</p></article>
          <article className="step-card"><b>02</b><h3>AI học từ bạn</h3><p>Xem, tìm, đặt — hệ thống ghi nhận và gợi ý tour ngày càng đúng hơn.</p></article>
          <article className="step-card"><b>03</b><h3>Đặt chỗ & thanh toán</h3><p>Giữ chỗ, đặt cọc 30% hoặc trả hết — rõ từng khoản.</p></article>
        </div>
      </section>

      <section className="banner">
        <div>
          <p className="stamp">Cá nhân hóa</p>
          <h2>Bạn muốn đi đâu?<br /><em>Hãy để chúng tôi lắng nghe.</em></h2>
          <p>Chọn miền, số ngày và ngân sách. ANAM lọc ngay những hành trình vừa vặn — không cần chờ nhân viên thiết kế.</p>
          <Link className="outline-button" to="/tim-tour">Tìm tour đúng gu</Link>
        </div>
      </section>
    </>
  );
}

function TourCard({ tour }) {
  return (
    <article className="tour-card">
      <div className="tour-image">
        <img src={tour.image} alt={tour.name} />
        <span className="tour-tag">{tour.duration} ngày</span>
        <span className="region-chip">{tour.region}</span>
        {tour.reason && <span className="reason-tag">{tour.reason}</span>}
      </div>
      <div className="tour-info">
        <div className="tour-location"><FaMapMarkerAlt /> {tour.destination}</div>
        <h3>{tour.name}</h3>
        <p>{tour.description}</p>
        {tour.tags?.length > 0 && (
          <div className="tag-row">{tour.tags.map((tag) => <span key={tag}>{tag}</span>)}</div>
        )}
        <div className="tour-bottom">
          <span className="price-from">từ <b>{money(tour.price)}</b></span>
          <span><FaStar /> {tour.rating || 'Mới'}</span>
        </div>
        <Link className="card-link" to={`/tour/${tour.id}`}>Xem hành trình →</Link>
      </div>
    </article>
  );
}

function TourGrid({ items }) {
  return <div className="tour-grid">{items.map((tour) => <TourCard key={tour.id} tour={tour} />)}</div>;
}

function ToursPage() {
  const [params, setParams] = useSearchParams();
  const [items, setItems] = useState([]);
  const [query, setQuery] = useState('');
  const [region, setRegion] = useState(params.get('region') || 'Tất cả');
  const [days, setDays] = useState('Tất cả');
  const [max, setMax] = useState(20000000);
  const [error, setError] = useState('');

  useEffect(() => {
    api.tours()
      .then((r) => setItems(itemsOf(r).map(normalizeTour)))
      .catch((e) => setError(api.errorMessage(e)));
  }, []);

  const changeRegion = (value) => {
    setRegion(value);
    const next = new URLSearchParams(params);
    if (value === 'Tất cả') next.delete('region');
    else next.set('region', value);
    setParams(next);
  };

  const result = useMemo(() => items.filter((tour) =>
    `${tour.name} ${tour.destination} ${tour.description}`.toLowerCase().includes(query.toLowerCase())
    && (region === 'Tất cả' || tour.region === region)
    && (days === 'Tất cả' || Number(tour.duration) === Number(days))
    && Number(tour.price) <= Number(max)
  ), [items, query, region, days, max]);

  return (
    <section className="page-section">
      <div className="page-heading">
        <div>
          <p className="stamp">Bộ sưu tập hành trình</p>
          <h1>Chọn nơi bạn<br /><em>muốn thuộc về.</em></h1>
        </div>
        <p className="heading-note">9 hành trình may đo — từ vịnh đá đến đảo ngọc, cố đô đến cao nguyên.</p>
      </div>
      <div className="filter-bar">
        <div className="search-box">
          <FaSearch />
          <input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Tìm Sa Pa, Phú Quốc, Huế..." />
        </div>
        <select value={region} onChange={(e) => changeRegion(e.target.value)}>
          <option>Tất cả</option>
          <option>Miền Bắc</option>
          <option>Miền Trung</option>
          <option>Miền Nam</option>
        </select>
        <select value={days} onChange={(e) => setDays(e.target.value)}>
          <option>Tất cả</option>
          <option value="3">3 ngày</option>
          <option value="4">4 ngày</option>
        </select>
        <label className="price-filter">
          Ngân sách
          <input type="range" min="1000000" max="8000000" step="200000" value={max} onChange={(e) => setMax(e.target.value)} />
          <span>{money(max)}</span>
        </label>
      </div>
      {error && <div className="form-error">{error}</div>}
      <p className="result-count">{result.length} hành trình phù hợp</p>
      {result.length ? <TourGrid items={result} /> : !error && <div className="empty-state"><h2>Chưa có tour khớp bộ lọc.</h2></div>}
    </section>
  );
}

function groupDays(plan) {
  const map = new Map();
  plan.forEach((item) => {
    const day = item.ngayThu || 1;
    if (!map.has(day)) map.set(day, []);
    map.get(day).push(item);
  });
  return [...map.entries()].sort((a, b) => a[0] - b[0]);
}

function TourDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [tour, setTour] = useState(null);
  const [related, setRelated] = useState([]);
  const [dates, setDates] = useState([]);
  const [plan, setPlan] = useState([]);
  const [reviewData, setReviewData] = useState({});
  const [photos, setPhotos] = useState([]);
  const [activePhoto, setActivePhoto] = useState('');
  const [date, setDate] = useState('');
  const [adults, setAdults] = useState(1);
  const [children, setChildren] = useState(0);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let alive = true;
    Promise.all([api.tourDetail(id), api.departures(id), api.itinerary(id), api.tourPhotos(id), api.reviews(id), api.tours()])
      .then(([t, d, i, p, r, all]) => {
        if (!alive) return;
        const next = normalizeTour(t.data);
        const shots = itemsOf(p);
        const local = next.gallery || [next.image];
        setTour(next);
        setDates(itemsOf(d));
        setDate(String(itemsOf(d)[0]?.maKhoiHanh || ''));
        setPlan(i.data?.lichTrinh || []);
        setPhotos(shots.length ? shots : local.map((url) => ({ url })));
        setActivePhoto(shots[0]?.url || shots[0]?.imageUrl || local[0]);
        setReviewData(r.data || {});
        setRelated(itemsOf(all).map(normalizeTour).filter((x) => x.id !== next.id && x.region === next.region).slice(0, 3));
      })
      .catch((e) => setError(api.errorMessage(e, 'Không thể tải thông tin tour.')));
    api.logBehavior({ MaTour: id, HanhDong: 'Xem' }).catch(() => {});
    return () => { alive = false; };
  }, [id]);

  if (error && !tour) {
    return <section className="page-section"><div className="form-error">{error}</div><Link className="back-link" to="/tours">← Quay lại</Link></section>;
  }
  if (!tour) return <section className="page-section">Đang tải tour...</section>;

  const book = async () => {
    if (!token()) {
      navigate('/dang-nhap', { state: { from: `/tour/${id}` } });
      return;
    }
    setBusy(true);
    try {
      const r = await api.createBooking({
        MaTour: id,
        MaKhoiHanh: date,
        SlnguoiLon: Number(adults),
        SltreEm: Number(children),
      });
      const created = r.data || {};
      const bookingId = created.maBooking || created.MaBooking;
      if (!bookingId) {
        setError('Đặt chỗ thành công nhưng không nhận được mã vé. Mở mục Chuyến đi của tôi.');
        navigate('/booking');
        return;
      }
      navigate(`/booking/${bookingId}`, { state: { booking: { ...created, tenTour: tour.name } } });
    } catch (e) {
      setError(api.errorMessage(e));
    } finally {
      setBusy(false);
    }
  };

  const list = reviewData.danhGias || [];
  const gallery = photos.length ? photos : [{ url: tour.image }];
  const days = groupDays(plan);
  const people = +adults + +children;

  return (
    <section className="page-section detail-page">
      <Link className="back-link" to="/tours">← Quay lại danh sách</Link>
      {error && <div className="form-error">{error}</div>}

      <div className="detail-hero">
        <img className="detail-image" src={activePhoto || tour.image} alt={tour.name} />
        <div className="detail-thumbs">
          {gallery.slice(0, 5).map((p, n) => {
            const src = p.url || p.imageUrl || tour.image;
            return <img key={n} className={src === activePhoto ? 'active' : ''} src={src} alt="" onClick={() => setActivePhoto(src)} />;
          })}
        </div>
      </div>

      <div className="detail-grid">
        <div className="detail-copy">
          <p className="stamp">{tour.region} · {tour.duration} ngày {tour.duration - 1} đêm</p>
          <h1>{tour.name}</h1>
          <div className="rating-line">
            <FaStar /> {reviewData.diemTrungBinh || tour.rating || 'Mới'}
            <span>·</span> {reviewData.tongDanhGia || list.length} đánh giá
            <span>·</span> tối đa {tour.slkhach || 25} khách
          </div>
          <p className="lede">{tour.description}</p>
          {tour.tags?.length > 0 && <div className="tag-row">{tour.tags.map((tag) => <span key={tag}>{tag}</span>)}</div>}

          {tour.highlights?.length > 0 && (
            <div className="highlight-box">
              <h3>Điểm nhấn</h3>
              <ul>{tour.highlights.map((item) => <li key={item}>{item}</li>)}</ul>
            </div>
          )}

          <h3>Lịch trình từng ngày</h3>
          <div className="itinerary-days">
            {days.length ? days.map(([day, items]) => (
              <article className="day-card" key={day}>
                <header><b>Ngày {day}</b><span>{items[0]?.tenDiaDanh || 'Hành trình'}</span></header>
                <ul>
                  {items.map((item, n) => (
                    <li key={item.maLichTrinh || n}>
                      <strong>{item.tenDiaDanh || item.tenSanPham || 'Hoạt động'}</strong>
                      <p>{item.mota || 'Thời gian tự do theo lịch đoàn.'}</p>
                    </li>
                  ))}
                </ul>
              </article>
            )) : <p className="muted">Lịch trình đang được cập nhật.</p>}
          </div>

          <div className="include-grid">
            <div>
              <h3>Đã gồm</h3>
              <ul>{(tour.includes || []).map((item) => <li key={item}>{item}</li>)}</ul>
            </div>
            <div>
              <h3>Chưa gồm</h3>
              <ul>{(tour.excludes || []).map((item) => <li key={item}>{item}</li>)}</ul>
            </div>
          </div>
          {tour.terms && <p className="terms">{tour.terms}</p>}
        </div>

        <aside className="booking-box sticky">
          <p className="muted">Giá từ</p>
          <h3 className="book-price">{money(tour.price)}<small>/ khách</small></h3>
          {dates.length ? (
            <label>Lịch khởi hành
              <select value={date} onChange={(e) => setDate(e.target.value)}>
                {dates.map((item) => (
                  <option key={item.maKhoiHanh} value={item.maKhoiHanh}>
                    {dateText(item.ngayKhoiHanh)} → {dateText(item.ngayKetThuc)} · {item.diaDiem || ''}
                  </option>
                ))}
              </select>
            </label>
          ) : <p className="muted">Chưa có lịch khởi hành.</p>}
          <div className="people-fields">
            <label>Người lớn<input type="number" min="1" value={adults} onChange={(e) => setAdults(e.target.value)} /></label>
            <label>Trẻ em<input type="number" min="0" value={children} onChange={(e) => setChildren(e.target.value)} /></label>
          </div>
          <div className="total-row"><span>{people} khách</span><b>{money(tour.price * people)}</b></div>
          <button disabled={!date || busy} className="primary-button full" onClick={book}>
            {busy ? 'Đang giữ chỗ...' : 'Đặt tour này'}
          </button>
          <p className="muted">Có thể đặt cọc 30% sau khi giữ chỗ.</p>
        </aside>
      </div>

      <section className="reviews-section">
        <p className="stamp">Cảm nhận</p>
        <h2>Người đi trước<br /><em>nói gì?</em></h2>
        {list.length ? (
          <div className="review-grid">
            {list.map((review, n) => (
              <div className="review-card" key={review.maDanhGiaTour || n}>
                <div className="stars">{'★'.repeat(review.saoDanhGia || 0)}</div>
                <p>“{review.nhanXet || 'Chuyến đi đáng nhớ.'}”</p>
                <b>Khách ANAM</b>
                <small>{dateText(review.thoiGian)}</small>
              </div>
            ))}
          </div>
        ) : <p className="muted">Chưa có đánh giá cho tour này.</p>}
      </section>

      {related.length > 0 && (
        <section className="reviews-section">
          <p className="stamp">Cùng miền</p>
          <h2>Hành trình<br /><em>gần giống.</em></h2>
          <TourGrid items={related} />
        </section>
      )}
    </section>
  );
}

function AuthPage({ register = false }) {
  const navigate = useNavigate();
  const location = useLocation();
  const [form, setForm] = useState({ phone: '', password: '' });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async (e) => {
    e.preventDefault();
    if (!form.phone || !form.password) return setError('Vui lòng nhập số điện thoại và mật khẩu.');
    setBusy(true);
    try {
      if (register) {
        await api.register({ SoDienThoai: form.phone, MatKhau: form.password });
        navigate('/dang-nhap', { state: { message: 'Đăng ký thành công, hãy đăng nhập.' } });
      } else {
        const { data } = await api.login({ SoDienThoai: form.phone, MatKhau: form.password });
        localStorage.setItem('wavv_token', data.token);
        write('wavv_user', {
          name: form.phone,
          phone: form.phone,
          maUser: data.maUser,
          maVaiTro: data.maVaiTro,
        });
        navigate(location.state?.from || '/');
      }
    } catch (e2) {
      setError(api.errorMessage(e2, 'Đăng nhập/đăng ký thất bại.'));
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="auth-page">
      <div className="auth-art">
        <p className="stamp">Chương tiếp theo của bạn</p>
        <h1>Mỗi chuyến đi<br /><em>một câu chuyện.</em></h1>
      </div>
      <form className="auth-form" onSubmit={submit}>
        <h2>{register ? 'Tạo tài khoản' : 'Chào mừng trở lại'}</h2>
        <p>{register ? 'Bắt đầu lưu những hành trình của riêng bạn.' : location.state?.message || 'Đăng nhập để giữ chỗ và xem gợi ý.'}</p>
        {error && <div className="form-error">{error}</div>}
        <input placeholder="Số điện thoại" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
        <input type="password" placeholder="Mật khẩu" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
        <button disabled={busy} className="primary-button full">{busy ? 'Đang xử lý...' : register ? 'Đăng ký' : 'Đăng nhập'}</button>
        <span className="auth-switch">
          {register ? 'Đã có tài khoản?' : 'Chưa có tài khoản?'}{' '}
          <Link to={register ? '/dang-nhap' : '/dang-ky'}>{register ? 'Đăng nhập' : 'Đăng ký'}</Link>
        </span>
      </form>
    </section>
  );
}

function Protected({ children }) {
  const location = useLocation();
  return token() ? children : <Navigate to="/dang-nhap" replace state={{ from: location.pathname + location.search }} />;
}

function RecommendationPage() {
  const [items, setItems] = useState([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const load = async (generate) => {
    setBusy(true);
    setError('');
    try {
      const r = generate ? await api.generateRecommendations() : await api.recommendations();
      const detailed = await Promise.all(itemsOf(r).map((x) =>
        api.tourDetail(x.maTour).then((t) => ({ ...normalizeTour(t.data), reason: x.lyDo }))
      ));
      setItems(detailed);
    } catch (e) {
      setError(api.errorMessage(e, 'Dịch vụ gợi ý tạm thời chưa sẵn sàng.'));
    } finally {
      setBusy(false);
    }
  };

  useEffect(() => { load(false); }, []);

  return (
    <section className="page-section">
      <div className="page-heading">
        <div>
          <p className="stamp">Dành riêng cho bạn</p>
          <h1>Đi đâu cũng được,<br /><em>miễn là đúng gu.</em></h1>
        </div>
      </div>
      <div className="ai-banner">
        <div>
          <h2>ANAM Curator</h2>
          <p>Gợi ý học từ những gì bạn xem, tìm và đặt. Càng dùng càng đúng.</p>
        </div>
        <button className="primary-button" onClick={() => load(true)}>{busy ? 'Đang tải...' : 'Làm mới gợi ý'}</button>
      </div>
      {error && <div className="form-error">{error}</div>}
      {items.length ? <TourGrid items={items} /> : !busy && <div className="empty-state"><h2>Chưa có gợi ý.</h2><p>Xem vài tour rồi bấm làm mới — AI sẽ bắt đầu hiểu bạn.</p></div>}
    </section>
  );
}

function FindTourPage() {
  const [tours, setTours] = useState([]);
  const [error, setError] = useState('');
  const [pref, setPref] = useState({ region: 'Tất cả', vibe: '', days: 'Tất cả', budget: 6000000 });

  useEffect(() => {
    api.tours()
      .then((r) => setTours(itemsOf(r).map(normalizeTour)))
      .catch((e) => setError(api.errorMessage(e)));
  }, []);

  const result = useMemo(() => tours.filter((tour) => {
    if (pref.region !== 'Tất cả' && tour.region !== pref.region) return false;
    if (pref.days !== 'Tất cả' && Number(tour.duration) !== Number(pref.days)) return false;
    if (Number(tour.price) > Number(pref.budget)) return false;
    if (pref.vibe) {
      const keys = { bien: ['Biển', 'Đảo', 'Nghỉ dưỡng'], nui: ['Núi', 'Sương', 'Tây Bắc', 'Cao nguyên'], vanhoa: ['Văn hóa', 'Cố đô', 'Phố cổ', 'Di sản', 'Chợ nổi'], giadinh: ['Gia đình', 'Hang động', 'Sông'] }[pref.vibe] || [];
      const hay = `${(tour.tags || []).join(' ')} ${tour.name} ${tour.description}`.toLowerCase();
      if (!keys.some((k) => hay.includes(k.toLowerCase()))) return false;
    }
    return true;
  }), [tours, pref]);

  const Choice = ({ name, value, children }) => (
    <button type="button" className={pref[name] === value ? 'choice on' : 'choice'} onClick={() => setPref({ ...pref, [name]: value })}>{children}</button>
  );

  return (
    <section className="page-section">
      <div className="page-heading">
        <div>
          <p className="stamp">Bộ lọc cảm hứng</p>
          <h1>Tìm tour<br /><em>đúng gu.</em></h1>
        </div>
        <p className="heading-note">Bốn lựa chọn. Kết quả hiện ngay — không chờ nhân viên thiết kế.</p>
      </div>
      <div className="quiz">
        <div>
          <p className="stamp">01 · Miền nào?</p>
          <div className="choice-row">
            <Choice name="region" value="Tất cả">Linh hoạt</Choice>
            <Choice name="region" value="Miền Bắc">Miền Bắc</Choice>
            <Choice name="region" value="Miền Trung">Miền Trung</Choice>
            <Choice name="region" value="Miền Nam">Miền Nam</Choice>
          </div>
        </div>
        <div>
          <p className="stamp">02 · Cảm hứng</p>
          <div className="choice-row">
            <Choice name="vibe" value="">Mọi kiểu</Choice>
            <Choice name="vibe" value="bien">Biển / đảo</Choice>
            <Choice name="vibe" value="nui">Núi / sương</Choice>
            <Choice name="vibe" value="vanhoa">Văn hóa</Choice>
            <Choice name="vibe" value="giadinh">Gia đình</Choice>
          </div>
        </div>
        <div>
          <p className="stamp">03 · Số ngày</p>
          <div className="choice-row">
            <Choice name="days" value="Tất cả">Linh hoạt</Choice>
            <Choice name="days" value="3">3 ngày</Choice>
            <Choice name="days" value="4">4 ngày</Choice>
          </div>
        </div>
        <label className="price-filter">
          Ngân sách tối đa
          <input type="range" min="2000000" max="8000000" step="200000" value={pref.budget} onChange={(e) => setPref({ ...pref, budget: e.target.value })} />
          <span>{money(pref.budget)}</span>
        </label>
      </div>
      {error && <div className="form-error">{error}</div>}
      <p className="result-count">{result.length} hành trình khớp gu của bạn</p>
      {result.length ? <TourGrid items={result} /> : !error && (
        <div className="empty-state">
          <h2>Chưa có tour khớp.</h2>
          <p>Nới ngân sách hoặc chọn “Linh hoạt” để xem thêm.</p>
          <Link className="primary-button" to="/tours">Xem tất cả tour</Link>
        </div>
      )}
    </section>
  );
}

function BookingsPage() {
  const [items, setItems] = useState([]);
  const [error, setError] = useState('');

  useEffect(() => {
    api.bookings().then((r) => setItems(itemsOf(r))).catch((e) => setError(api.errorMessage(e)));
  }, []);

  return (
    <section className="page-section">
      <div className="page-heading">
        <div>
          <p className="stamp">Chuyến đi của tôi</p>
          <h1>Những tấm vé<br /><em>đã giữ chỗ.</em></h1>
        </div>
      </div>
      {error && <div className="form-error">{error}</div>}
      {items.length ? (
        <div className="booking-list">
          {items.map((item) => (
            <Link className="booking-row" to={`/booking/${item.maBooking}`} key={item.maBooking}>
              <div>
                <span>{item.maBooking}</span>
                <h3>{item.tenTour}</h3>
                <p>{dateText(item.ngayDat)} · {item.slnguoiLon} người lớn, {item.sltreEm} trẻ em</p>
              </div>
              <div>
                <b>{money(item.thanhTien)}</b>
                <span className="status">{statusLabel(item.trangThai)}</span>
              </div>
            </Link>
          ))}
        </div>
      ) : !error && (
        <div className="empty-state">
          <h2>Chưa có hành trình nào.</h2>
          <Link className="primary-button" to="/tours">Khám phá tour</Link>
        </div>
      )}
    </section>
  );
}

function BookingDetailPage() {
  const { id } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const [item, setItem] = useState(location.state?.booking || null);
  const [summary, setSummary] = useState({});
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(!location.state?.booking);
  const [paymentMethod, setPaymentMethod] = useState('VNPay');
  const returnParams = new URLSearchParams(location.search);
  const gatewayReturnStatus = returnParams.get('status') ||
    (returnParams.get('paid') === '1' ? 'success' : '');

  const load = async () => {
    if (!id) return;
    if (!token()) {
      navigate('/dang-nhap', { replace: true, state: { from: `/booking/${id}` } });
      return;
    }
    setLoading(!item);
    setError('');
    try {
      const bookingRes = await api.bookingDetail(id);
      setItem(bookingRes.data);
      try {
        const sumRes = await api.paymentSummary(id);
        setSummary(sumRes.data || {});
      } catch (sumError) {
        setSummary({});
        setError(api.errorMessage(sumError, 'Không tải được số dư thanh toán.'));
      }
    } catch (e) {
      if (e?.response?.status === 401) {
        navigate('/dang-nhap', { replace: true, state: { from: `/booking/${id}` } });
        return;
      }
      if (!item) setError(api.errorMessage(e, 'Không tìm thấy vé này. Hãy đăng nhập đúng tài khoản đã đặt.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [id, location.search]);

  const pay = async (kind, amount) => {
    setError('');
    try {
      const payload = {
        MaBooking: id,
        SoTien: Number(amount),
        PhuongThuc: paymentMethod,
        LoaiThanhToan: kind,
      };
      const requestConfig = {
        headers: { 'Idempotency-Key': `${id}-${kind}-${paymentMethod}-${Date.now()}` },
      };
      if (paymentMethod === 'VNPay' || paymentMethod === 'MoMo') {
        const response = await api.createGatewayPayment(payload, requestConfig);
        window.location.assign(response.data.payUrl);
        return;
      }
      await api.createPayment(payload, requestConfig);
      await load();
    } catch (e2) {
      setError(api.errorMessage(e2, 'Thanh toán chưa thành công.'));
    }
  };

  const cancel = async () => {
    try {
      await api.cancelBooking(id);
      await load();
    } catch (e) {
      setError(api.errorMessage(e, 'Không hủy được booking.'));
    }
  };

  if (loading && !item) {
    return (
      <section className="page-section">
        <div className="empty-state">
          <h2>Đang mở vé của bạn…</h2>
          <p>Mã vé {id}</p>
        </div>
      </section>
    );
  }

  if (!item) {
    return (
      <section className="page-section">
        <div className="empty-state">
          <h2>Chưa xem được vé</h2>
          {error && <div className="form-error">{error}</div>}
          <p>Đăng nhập đúng tài khoản khách đã đặt chỗ, rồi mở lại trang này.</p>
          <div className="hero-actions" style={{ justifyContent: 'center' }}>
            <Link className="primary-button" to="/dang-nhap" state={{ from: `/booking/${id}` }}>Đăng nhập</Link>
            <Link className="outline-button" to="/booking">Chuyến đi của tôi</Link>
          </div>
        </div>
      </section>
    );
  }

  const remain = Number(summary.conLai ?? item.thanhTien ?? item.tongTien ?? 0);
  const total = Number(summary.tongTien ?? item.thanhTien ?? item.tongTien ?? 0);
  const paid = Number(summary.daThanhToan ?? 0);
  const deposit = remain > 0 ? Math.max(1, Math.min(Math.round(total * 0.3), remain)) : 0;

  return (
    <section className="page-section booking-detail">
      <Link className="back-link" to="/booking">← Chuyến đi của tôi</Link>
      <div className="success-message">Đặt chỗ thành công. Bạn có thể đặt cọc 30% hoặc thanh toán hết.</div>
      {error && <div className="form-error">{error}</div>}
      <div className="booking-detail-grid">
        <div className="booking-hero">
          <p className="stamp">Vé {item.maBooking || id}</p>
          <h1>{item.tenTour || 'Hành trình đã giữ chỗ'}</h1>
          <div className="detail-summary">
            <span>Khởi hành</span><b>{dateText(item.ngayKhoiHanh)}</b>
            <span>Kết thúc</span><b>{dateText(item.ngayKetThuc)}</b>
            <span>Điểm tập trung</span><b>{item.diaDiem || '—'}</b>
            <span>Số khách</span><b>{item.slnguoiLon ?? 1} người lớn · {item.sltreEm ?? 0} trẻ em</b>
            <span>Trạng thái</span><b className="status">{statusLabel(item.trangThai)}</b>
          </div>
          {statusLabel(item.trangThai) !== 'Đã hủy' && (
            <button className="outline-button" onClick={cancel}>Hủy booking</button>
          )}
        </div>
        <div className="payment-card">
          <p className="stamp">Thanh toán</p>
          {gatewayReturnStatus === 'success' && (
            <div className="success-message">
              Cổng đã báo giao dịch thành công. Hệ thống đang đối chiếu IPN và đã tải lại số dư.
            </div>
          )}
          {gatewayReturnStatus === 'failed' && (
            <div className="form-error">Giao dịch tại cổng chưa thành công.</div>
          )}
          {gatewayReturnStatus === 'invalid' && (
            <div className="form-error">Không xác minh được chữ ký trả về từ cổng thanh toán.</div>
          )}
          <div className="total-row"><span>Tổng tiền</span><b>{money(total)}</b></div>
          <div className="total-row"><span>Đã thanh toán</span><b>{money(paid)}</b></div>
          <div className="total-row remain"><span>Còn lại</span><b>{money(remain)}</b></div>
          {remain > 0 && (
            <div className="pay-actions">
              <label>
                Phương thức thanh toán
                <select value={paymentMethod} onChange={(event) => setPaymentMethod(event.target.value)}>
                  <option value="VNPay">VNPay sandbox</option>
                  <option value="MoMo">MoMo sandbox</option>
                  <option value="ChuyenKhoan">Chuyển khoản</option>
                  <option value="TienMat">Tiền mặt</option>
                </select>
              </label>
              <button className="primary-button full" onClick={() => pay('DatCoc', deposit)}>
                Đặt cọc 30% · {money(deposit)}
              </button>
              <button className="outline-button full" onClick={() => pay('ThanhToanDu', remain)}>
                Thanh toán hết
              </button>
            </div>
          )}
          {remain <= 0 && <div className="success-message">Đã thanh toán đủ.</div>}
        </div>
      </div>
    </section>
  );
}

const emptyDoc = { loaiGiayTo: 'CCCD', soTrenGiayTo: '', ngayCap: '', ngayHetHan: '', noiCap: '' };

function ProfilePage() {
  const user = read('wavv_user') || {};
  const [item, setItem] = useState(null);
  const [docs, setDocs] = useState([]);
  const [trips, setTrips] = useState([]);
  const [form, setForm] = useState({
    ho: '',
    ten: '',
    danhXung: 'Anh',
    gioiTinh: 'Nam',
    ngaySinh: '',
    email: '',
    country: 'Việt Nam',
    hoGiayTo: '',
    tenGiayTo: '',
  });
  const [docForm, setDocForm] = useState(emptyDoc);
  const [editingDoc, setEditingDoc] = useState('');
  const [saved, setSaved] = useState(false);
  const [docSaved, setDocSaved] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [docBusy, setDocBusy] = useState(false);

  const setField = (key) => (e) => setForm((prev) => ({ ...prev, [key]: e.target.value }));
  const setDocField = (key) => (e) => setDocForm((prev) => ({ ...prev, [key]: e.target.value }));

  useEffect(() => {
    let alive = true;
    Promise.allSettled([api.profile(), api.bookings()]).then(([profileRes, bookingRes]) => {
      if (!alive) return;
      if (profileRes.status === 'fulfilled') {
        const p = Array.isArray(profileRes.value.data) ? profileRes.value.data[0] : profileRes.value.data;
        if (p) {
          setItem(p);
          setDocs(p.giayTos || []);
          setForm({
            ho: p.ho || '',
            ten: p.ten || '',
            danhXung: trim(p.danhXung) || 'Anh',
            gioiTinh: trim(p.gioiTinh) || 'Nam',
            ngaySinh: isoDate(p.ngaySinh),
            email: p.email || '',
            country: p.quocTich || 'Việt Nam',
            hoGiayTo: p.hoGiayTo || p.ho || '',
            tenGiayTo: p.tenGiayTo || p.ten || '',
          });
        }
      } else {
        setError(api.errorMessage(profileRes.reason));
      }
      if (bookingRes.status === 'fulfilled') setTrips(itemsOf(bookingRes.value));
    });
    return () => { alive = false; };
  }, []);

  const fullName = `${form.ho} ${form.ten}`.trim() || user.name || 'Hành khách ANAM';
  const age = yearsOld(form.ngaySinh);
  const phone = trim(item?.soDienThoai) || user.phone || '—';
  const filled = [
    form.ho, form.ten, form.danhXung, form.gioiTinh, form.ngaySinh,
    form.email, form.country, form.hoGiayTo, form.tenGiayTo, docs.length,
  ].filter(Boolean).length;
  const completeness = Math.round((filled / 10) * 100);
  const doneTrips = trips.filter((t) => /HoanThanh|DaThanhToan/i.test(trim(t.trangThai))).length;
  const nextTrips = trips.filter((t) => /ChoXacNhan|DaXacNhan/i.test(trim(t.trangThai))).length;
  const rank = doneTrips >= 3 ? 'Nhà thám hiểm' : trips.length >= 1 ? 'Hành khách thân thiết' : 'Tân hành khách';

  const payload = () => ({
    Ho: form.ho.trim(),
    Ten: form.ten.trim(),
    HoGiayTo: form.hoGiayTo.trim() || form.ho.trim(),
    TenGiayTo: form.tenGiayTo.trim() || form.ten.trim(),
    QuocTich: form.country.trim() || 'Việt Nam',
    DanhXung: form.danhXung,
    GioiTinh: form.gioiTinh,
    NgaySinh: form.ngaySinh || null,
    Email: form.email.trim(),
  });

  const save = async (e) => {
    e.preventDefault();
    if (!form.ho.trim() || !form.ten.trim()) {
      setError('Họ và tên không được để trống.');
      return;
    }
    setBusy(true);
    setError('');
    setSaved(false);
    try {
      const res = item
        ? await api.updateProfile(item.maKhachHang, payload())
        : await api.createProfile(payload());
      const next = res.data || {};
      setItem({ ...next, giayTos: docs });
      write('wavv_user', { ...user, name: `${form.ho} ${form.ten}`.trim() });
      setSaved(true);
    } catch (e2) {
      setError(api.errorMessage(e2));
    } finally {
      setBusy(false);
    }
  };

  const docPayload = () => ({
    LoaiGiayTo: docForm.loaiGiayTo,
    SoTrenGiayTo: docForm.soTrenGiayTo.trim(),
    NgayCap: docForm.ngayCap,
    NgayHetHan: docForm.ngayHetHan,
    NoiCap: docForm.noiCap.trim(),
  });

  const saveDoc = async (e) => {
    e.preventDefault();
    if (!item?.maKhachHang) {
      setError('Hãy lưu thông tin hành khách trước khi thêm giấy tờ.');
      return;
    }
    setDocBusy(true);
    setError('');
    setDocSaved('');
    try {
      if (editingDoc) {
        const res = await api.updateDocument(item.maKhachHang, editingDoc, docPayload());
        setDocs((list) => list.map((d) => (d.maGiayTo === editingDoc ? res.data : d)));
        setDocSaved('Đã cập nhật giấy tờ.');
      } else {
        const res = await api.addDocument(item.maKhachHang, docPayload());
        setDocs((list) => [...list, res.data]);
        setDocSaved('Đã thêm giấy tờ mới.');
      }
      setDocForm(emptyDoc);
      setEditingDoc('');
    } catch (e2) {
      setError(api.errorMessage(e2));
    } finally {
      setDocBusy(false);
    }
  };

  const editDoc = (doc) => {
    setEditingDoc(doc.maGiayTo);
    setDocForm({
      loaiGiayTo: doc.loaiGiayTo || 'CCCD',
      soTrenGiayTo: doc.soTrenGiayTo || '',
      ngayCap: isoDate(doc.ngayCap),
      ngayHetHan: isoDate(doc.ngayHetHan),
      noiCap: doc.noiCap || '',
    });
    setDocSaved('');
  };

  const removeDoc = async (doc) => {
    if (!item?.maKhachHang || !window.confirm(`Xóa ${doc.loaiGiayTo} ${doc.soTrenGiayTo}?`)) return;
    try {
      await api.deleteDocument(item.maKhachHang, doc.maGiayTo);
      setDocs((list) => list.filter((d) => d.maGiayTo !== doc.maGiayTo));
      if (editingDoc === doc.maGiayTo) {
        setEditingDoc('');
        setDocForm(emptyDoc);
      }
    } catch (e2) {
      setError(api.errorMessage(e2));
    }
  };

  const docTone = (doc) => {
    if (!doc?.ngayHetHan) return { label: 'Chưa rõ hạn', tone: 'muted' };
    const days = Math.round((new Date(doc.ngayHetHan) - new Date()) / 86400000);
    if (days < 0) return { label: 'Đã hết hạn', tone: 'bad' };
    if (days < 180) return { label: `Còn ${days} ngày`, tone: 'warn' };
    return { label: `Hết hạn ${dateText(doc.ngayHetHan)}`, tone: 'ok' };
  };

  return (
    <section className="page-section profile-page">
      <div className="page-heading">
        <div>
          <p className="stamp">Hộ chiếu ANAM</p>
          <h1>Hồ sơ<br /><em>hành khách.</em></h1>
          <p className="lede">Thông tin này dùng khi giữ chỗ, làm hợp đồng và kiểm tra giấy tờ xuất hành. Điền càng đủ, đặt tour càng nhanh.</p>
        </div>
        <p className="heading-note">Hoàn thiện {completeness}%</p>
      </div>

      {error && <div className="form-error">{error}</div>}

      <div className="profile-grid">
        <aside className="passport-card">
          <div className="passport-head">
            <span>ANAM · LỮ HÀNH MAY ĐO</span>
            <b>PASSPORT</b>
          </div>
          <div className="avatar">{(form.ten || form.ho || 'K').charAt(0)}</div>
          <p className="passport-title">{form.danhXung} · {form.gioiTinh}{age != null ? ` · ${age} tuổi` : ''}</p>
          <h3>{fullName}</h3>
          <p className="passport-rank">{rank}</p>
          <ul className="passport-meta">
            <li><FaPhoneAlt /> {phone}</li>
            <li><FaEnvelope /> {form.email || 'Chưa có email'}</li>
            <li><FaMapMarkerAlt /> {form.country || 'Chưa rõ quốc tịch'}</li>
            <li><FaCalendarAlt /> Sinh {form.ngaySinh ? dateText(form.ngaySinh) : 'chưa cập nhật'}</li>
            <li><FaIdCard /> Mã {item?.maKhachHang || 'sẽ tạo khi lưu'}</li>
          </ul>
          <div className="completeness">
            <div className="completeness-bar"><i style={{ width: `${completeness}%` }} /></div>
            <small>{completeness < 100 ? 'Còn thiếu vài mục — giấy tờ giúp đặt tour quốc tế.' : 'Hồ sơ đã đủ để xuất hành.'}</small>
          </div>
          <div className="passport-links">
            <Link to="/booking"><FaPlane /> Chuyến đi của tôi</Link>
            <Link to="/goi-y">Gợi ý AI</Link>
            <Link to="/tim-tour">Tìm đúng gu</Link>
          </div>
        </aside>

        <div className="profile-main">
          <div className="profile-stats">
            <article><b>{trips.length}</b><span>Tour đã đặt</span></article>
            <article><b>{doneTrips}</b><span>Đã hoàn tất</span></article>
            <article><b>{nextTrips}</b><span>Sắp khởi hành</span></article>
            <article><b>{docs.length}</b><span>Giấy tờ</span></article>
          </div>

          <form className="profile-form" onSubmit={save}>
            <header className="profile-block-head">
              <div>
                <p className="proposal-label">01 · Cá nhân</p>
                <h2>Thông tin hành khách</h2>
              </div>
              {saved && <div className="success-message">Đã lưu hồ sơ.</div>}
            </header>

            <div className="profile-fields">
              <label>Danh xưng
                <select value={form.danhXung} onChange={setField('danhXung')}>
                  <option>Anh</option>
                  <option>Chị</option>
                  <option>Ông</option>
                  <option>Bà</option>
                </select>
              </label>
              <label>Giới tính
                <select value={form.gioiTinh} onChange={setField('gioiTinh')}>
                  <option>Nam</option>
                  <option>Nữ</option>
                  <option>Khác</option>
                </select>
              </label>
              <label>Họ
                <input maxLength={20} value={form.ho} onChange={setField('ho')} placeholder="Nguyễn" required />
              </label>
              <label>Tên
                <input maxLength={50} value={form.ten} onChange={setField('ten')} placeholder="An" required />
              </label>
              <label>Ngày sinh
                <input type="date" value={form.ngaySinh} onChange={setField('ngaySinh')} />
              </label>
              <label>Quốc tịch
                <input list="nations" maxLength={50} value={form.country} onChange={setField('country')} placeholder="Việt Nam" />
                <datalist id="nations">
                  <option value="Việt Nam" />
                  <option value="Hoa Kỳ" />
                  <option value="Nhật Bản" />
                  <option value="Hàn Quốc" />
                  <option value="Pháp" />
                  <option value="Úc" />
                  <option value="Singapore" />
                </datalist>
              </label>
              <label className="span-2">Email
                <input type="email" maxLength={100} value={form.email} onChange={setField('email')} placeholder="ban@email.com" />
              </label>
              <label className="span-2">Số điện thoại tài khoản
                <input value={phone} readOnly />
              </label>
            </div>

            <header className="profile-block-head">
              <div>
                <p className="proposal-label">02 · Xuất hành</p>
                <h2>Tên trên giấy tờ</h2>
                <p>Nên khớp CCCD hoặc hộ chiếu — dùng khi làm hợp đồng và check-in.</p>
              </div>
            </header>
            <div className="profile-fields">
              <label>Họ trên giấy tờ
                <input maxLength={20} value={form.hoGiayTo} onChange={setField('hoGiayTo')} placeholder="NGUYEN" />
              </label>
              <label>Tên trên giấy tờ
                <input maxLength={50} value={form.tenGiayTo} onChange={setField('tenGiayTo')} placeholder="AN" />
              </label>
            </div>
            <button className="primary-button" disabled={busy}>{busy ? 'Đang lưu...' : item ? 'Lưu thay đổi' : 'Tạo hồ sơ'}</button>
          </form>

          <div className="profile-form docs-panel">
            <header className="profile-block-head">
              <div>
                <p className="proposal-label">03 · Giấy tờ</p>
                <h2>CCCD & hộ chiếu</h2>
                <p>Lưu số giấy tờ để Sale xác minh nhanh khi bạn đặt chỗ.</p>
              </div>
              {docSaved && <div className="success-message">{docSaved}</div>}
            </header>

            {docs.length ? (
              <div className="doc-grid">
                {docs.map((doc) => {
                  const tone = docTone(doc);
                  return (
                    <article className="doc-card" key={doc.maGiayTo}>
                      <header>
                        <FaPassport />
                        <div>
                          <b>{doc.loaiGiayTo}</b>
                          <span className={`doc-tone ${tone.tone}`}>{tone.label}</span>
                        </div>
                      </header>
                      <p className="doc-number">{doc.soTrenGiayTo}</p>
                      <p>Cấp {dateText(doc.ngayCap)} · {doc.noiCap}</p>
                      <div className="doc-actions">
                        <button type="button" className="text-button" onClick={() => editDoc(doc)}>Sửa</button>
                        <button type="button" className="text-button" onClick={() => removeDoc(doc)}>Xóa</button>
                      </div>
                    </article>
                  );
                })}
              </div>
            ) : (
              <p className="muted">Chưa có giấy tờ. Thêm CCCD hoặc hộ chiếu bên dưới.</p>
            )}

            <form className="doc-form" onSubmit={saveDoc}>
              <h3>{editingDoc ? 'Sửa giấy tờ' : 'Thêm giấy tờ'}</h3>
              <div className="profile-fields">
                <label>Loại
                  <select value={docForm.loaiGiayTo} onChange={setDocField('loaiGiayTo')}>
                    <option>CCCD</option>
                    <option>Hộ chiếu</option>
                    <option>CMND</option>
                  </select>
                </label>
                <label>Số giấy tờ
                  <input maxLength={50} value={docForm.soTrenGiayTo} onChange={setDocField('soTrenGiayTo')} placeholder="079098000001" required />
                </label>
                <label>Ngày cấp
                  <input type="date" value={docForm.ngayCap} onChange={setDocField('ngayCap')} required />
                </label>
                <label>Ngày hết hạn
                  <input type="date" value={docForm.ngayHetHan} onChange={setDocField('ngayHetHan')} required />
                </label>
                <label className="span-2">Nơi cấp
                  <input maxLength={50} value={docForm.noiCap} onChange={setDocField('noiCap')} placeholder="Cục Cảnh sát QLHC" required />
                </label>
              </div>
              <div className="doc-form-actions">
                <button className="primary-button" disabled={docBusy}>{docBusy ? 'Đang lưu...' : editingDoc ? 'Cập nhật giấy tờ' : 'Thêm giấy tờ'}</button>
                {editingDoc && (
                  <button type="button" className="outline-button" onClick={() => { setEditingDoc(''); setDocForm(emptyDoc); }}>
                    Hủy sửa
                  </button>
                )}
              </div>
            </form>
          </div>

          <div className="profile-form trip-panel">
            <header className="profile-block-head">
              <div>
                <p className="proposal-label">04 · Nhật ký</p>
                <h2>Chuyến đi gần đây</h2>
              </div>
              <Link className="card-link" to="/booking">Xem tất cả</Link>
            </header>
            {trips.length ? (
              <div className="profile-trips">
                {trips.slice(0, 3).map((trip) => (
                  <Link className="profile-trip" to={`/booking/${trip.maBooking}`} key={trip.maBooking}>
                    <div>
                      <b>{trip.tenTour || trip.maBooking}</b>
                      <p>{dateText(trip.ngayDat)} · {trip.slnguoiLon} người lớn{trip.sltreEm ? `, ${trip.sltreEm} trẻ em` : ''}</p>
                    </div>
                    <span className="status">{statusLabel(trip.trangThai)}</span>
                  </Link>
                ))}
              </div>
            ) : (
              <div className="empty-inline">
                <p>Chưa có vé nào. Bắt đầu từ một hành trình vừa vặn.</p>
                <Link className="outline-button" to="/tours">Khám phá tour</Link>
              </div>
            )}
          </div>
        </div>
      </div>
    </section>
  );
}

function PromotionsPage() {
  const [items, setItems] = useState([]);
  const [error, setError] = useState('');
  const [copied, setCopied] = useState('');

  useEffect(() => {
    api.promotions().then((r) => setItems(itemsOf(r))).catch((e) => setError(api.errorMessage(e)));
  }, []);

  return (
    <section className="page-section">
      <div className="page-heading">
        <div>
          <p className="stamp">Đặc quyền ANAM</p>
          <h1>Ưu đãi<br /><em>cho chuyến đi.</em></h1>
        </div>
      </div>
      {error && <div className="form-error">{error}</div>}
      {copied && <div className="success-message">Đã sao chép mã {copied}</div>}
      <div className="promo-grid">
        {items.map((item, n) => (
          <div className={n % 2 ? 'promo-card' : 'promo-card alt'} key={item.maKm}>
            <span>{item.maCode}</span>
            <h2>{item.tenKm}</h2>
            <p>Giảm {item.giamGia}{item.donVi === '%' ? '%' : ' đ'} · đến {dateText(item.ngayKt)}</p>
            <button className="outline-button" onClick={() => { navigator.clipboard?.writeText(item.maCode); setCopied(item.maCode); }}>
              Sao chép mã
            </button>
          </div>
        ))}
      </div>
      {!items.length && !error && <div className="empty-state"><h2>Chưa có ưu đãi.</h2></div>}
    </section>
  );
}

function NotFound() {
  return (
    <section className="page-section">
      <div className="empty-state">
        <h2>Trang này không tồn tại.</h2>
        <Link className="primary-button" to="/">Về trang chủ</Link>
      </div>
    </section>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<Home />} />
          <Route path="/tours" element={<ToursPage />} />
          <Route path="/tour/:id" element={<TourDetail />} />
          <Route path="/dang-nhap" element={<AuthPage />} />
          <Route path="/dang-ky" element={<AuthPage register />} />
          <Route path="/goi-y" element={<Protected><RecommendationPage /></Protected>} />
          <Route path="/tim-tour" element={<FindTourPage />} />
          <Route path="/tu-thiet-ke" element={<Navigate to="/tim-tour" replace />} />
          <Route path="/tu-thiet-ke/de-xuat" element={<Navigate to="/tim-tour" replace />} />
          <Route path="/booking" element={<Protected><BookingsPage /></Protected>} />
          <Route path="/booking/:id" element={<Protected><BookingDetailPage /></Protected>} />
          <Route path="/ho-so" element={<Protected><ProfilePage /></Protected>} />
          <Route path="/uu-dai" element={<PromotionsPage />} />
          <Route path="*" element={<NotFound />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
