import featured1 from './assets/featured/featured1.jpg';
import featured2 from './assets/featured/featured2.jpg';
import featured3 from './assets/featured/featured3.jpg';
import featured4 from './assets/featured/featured4.jpg';
import featured5 from './assets/featured/featured5.jpg';
import featured6 from './assets/featured/featured6.jpg';

export const tours = [
  { id: 'TOUR001', name: 'Đà Nẵng - Hội An 4 ngày', destination: 'Đà Nẵng', region: 'Miền Trung', price: 4800000, duration: 4, image: featured1, tags: ['Biển', 'Văn hóa'], rating: 4.8, description: 'Khám phá biển Mỹ Khê, phố cổ Hội An và những món ăn miền Trung.' },
  { id: 'TOUR002', name: 'Hạ Long - Ninh Bình 3 ngày', destination: 'Quảng Ninh', region: 'Miền Bắc', price: 3900000, duration: 3, image: featured2, tags: ['Thiên nhiên', 'Gia đình'], rating: 4.7, description: 'Du ngoạn vịnh Hạ Long và ngắm cảnh Tràng An.' },
  { id: 'TOUR003', name: 'Đà Lạt nghỉ dưỡng 3 ngày', destination: 'Đà Lạt', region: 'Tây Nguyên', price: 3200000, duration: 3, image: featured3, tags: ['Nghỉ dưỡng', 'Ẩm thực'], rating: 4.6, description: 'Tận hưởng khí hậu mát lành, cà phê và những cung đường hoa.' },
  { id: 'TOUR004', name: 'Phú Quốc biển xanh 4 ngày', destination: 'Phú Quốc', region: 'Miền Nam', price: 6200000, duration: 4, image: featured4, tags: ['Biển', 'Nghỉ dưỡng'], rating: 4.9, description: 'Kỳ nghỉ biển trọn vẹn với hoàng hôn, đảo nhỏ và hải sản.' },
  { id: 'TOUR005', name: 'Sapa - Fansipan 3 ngày', destination: 'Sapa', region: 'Miền Bắc', price: 4100000, duration: 3, image: featured5, tags: ['Leo núi', 'Bản làng'], rating: 4.5, description: 'Chạm nóc nhà Đông Dương và tìm hiểu văn hóa bản địa.' },
  { id: 'TOUR006', name: 'Huế - Quảng Bình di sản 5 ngày', destination: 'Huế', region: 'Miền Trung', price: 5700000, duration: 5, image: featured6, tags: ['Di sản', 'Thiên nhiên'], rating: 4.7, description: 'Hành trình qua kinh thành Huế và động Phong Nha.' },
];
export const departures = { TOUR001: ['2026-09-12', '2026-10-03'], TOUR002: ['2026-09-18', '2026-10-10'], TOUR003: ['2026-09-25'], TOUR004: ['2026-10-01', '2026-11-05'], TOUR005: ['2026-09-20'], TOUR006: ['2026-10-15'] };
export const reviews = { TOUR001: [{ name: 'Nguyễn Minh', stars: 5, text: 'Lịch trình hợp lý, hướng dẫn viên nhiệt tình.', date: '12/06/2026' }, { name: 'Trần An', stars: 4, text: 'Phố cổ Hội An rất đẹp, dịch vụ tốt.', date: '04/05/2026' }], TOUR004: [{ name: 'Lê Hà', stars: 5, text: 'Bãi biển và khách sạn đều tuyệt vời.', date: '20/05/2026' }] };
export const proposals = { REQ001: [{ id: 'PLAN001', name: 'Tiết kiệm', price: 7800000, note: 'Tối ưu chi phí, ưu tiên trải nghiệm địa phương.', days: ['Ngày 1 · Đà Nẵng - Sơn Trà', 'Ngày 2 · Hội An - phố cổ', 'Ngày 3 · Bà Nà Hills', 'Ngày 4 · Tự do và trở về'] }, { id: 'PLAN002', name: 'Cân bằng', price: 10200000, note: 'Cân bằng thời gian nghỉ dưỡng và tham quan.', days: ['Ngày 1 · Đón sân bay - biển Mỹ Khê', 'Ngày 2 · Bà Nà Hills', 'Ngày 3 · Hội An - trải nghiệm ẩm thực', 'Ngày 4 · Ngũ Hành Sơn - trở về'] }, { id: 'PLAN003', name: 'Cao cấp', price: 15800000, note: 'Dịch vụ riêng, khách sạn cao cấp và lịch trình linh hoạt.', days: ['Ngày 1 · Xe riêng - resort biển', 'Ngày 2 · Du thuyền riêng Cù Lao Chàm', 'Ngày 3 · Hội An về đêm', 'Ngày 4 · Spa - tiễn sân bay'] }] };
