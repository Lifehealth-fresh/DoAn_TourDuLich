import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://localhost:7290',
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('wavv_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export const errorMessage = (error, fallback = 'Có lỗi xảy ra. Vui lòng thử lại.') => {
  const data = error?.response?.data;
  if (typeof data === 'string' && data.trim()) return data;
  if (data?.message) return data.message;
  if (data?.title) return data.title;
  if (error?.message === 'Network Error') return 'Không kết nối được máy chủ. Kiểm tra API đang chạy.';
  return fallback;
};

export const login = (data) => api.post('/api/Auth/login', data);
export const register = (data) => api.post('/api/Auth/register', data);
export const tours = () => api.get('/api/Tour', { params: { pageSize: 50 } });
export const tourDetail = (id) => api.get(`/api/Tour/${encodeURIComponent(id)}`);
export const departures = (id) => api.get(`/api/Tour/${encodeURIComponent(id)}/lich-khoi-hanh`);
export const itinerary = (id) => api.get(`/api/LichTrinh/tour/${encodeURIComponent(id)}`);
export const tourPhotos = (id) => api.get(`/api/Tour/${encodeURIComponent(id)}/anh`);
export const reviews = (id) => api.get(`/api/DanhGia/tour/${encodeURIComponent(id)}`);
export const savedTours = () => api.get('/api/DanhSachYeuThich/cua-toi');
export const saveTour = (data) => api.post('/api/DanhSachYeuThich', data);
export const removeSavedTour = (id) => api.delete(`/api/DanhSachYeuThich/${encodeURIComponent(id)}`);
export const recommendations = () => api.get('/api/AiGoiY/cua-toi');
export const generateRecommendations = (data = {}) => api.post('/api/AiGoiY/sinh-goi-y', { SoLuong: 5, Alpha: 0.5, ...data });
export const designRequests = () => api.get('/api/YeuCauThietKe/cua-toi');
export const createDesignRequest = (data) => api.post('/api/YeuCauThietKe', data);
export const designProposals = (id) => api.get(`/api/YeuCauThietKe/${encodeURIComponent(id)}/de-xuat`);
export const chooseProposal = (requestId, proposalId) => api.put(`/api/YeuCauThietKe/${encodeURIComponent(requestId)}/chon-de-xuat/${encodeURIComponent(proposalId)}`);
export const bookings = () => api.get('/api/DatDichVu/cua-toi');
export const bookingDetail = (id) => api.get(`/api/DatDichVu/${encodeURIComponent(id)}`);
export const createBooking = (data) => api.post('/api/DatDichVu', data);
export const cancelBooking = (id) => api.put(`/api/DatDichVu/${encodeURIComponent(id)}/huy`);
export const bookingPayments = (id) => api.get(`/api/ThanhToan/theo-booking/${encodeURIComponent(id)}`);
export const paymentSummary = (id) => api.get(`/api/ThanhToan/theo-booking/${encodeURIComponent(id)}/tong-hop`);
export const createPayment = (data, config = {}) => api.post('/api/ThanhToan', data, {
  ...config,
  headers: {
    'Idempotency-Key': config?.headers?.['Idempotency-Key'] || `${data?.MaBooking || 'pay'}-${Date.now()}`,
    ...config.headers,
  },
});
export const createGatewayPayment = (data, config = {}) => api.post('/api/ThanhToan/tao-phien-cong', data, {
  ...config,
  headers: {
    'Idempotency-Key': config?.headers?.['Idempotency-Key'] || `${data?.MaBooking || 'gateway'}-${Date.now()}`,
    ...config.headers,
  },
});
export const bookingContract = (id) => api.get(`/api/HopDong/theo-booking/${encodeURIComponent(id)}`);
export const promotions = () => api.get('/api/KhuyenMai');
export const logBehavior = (data) => api.post('/api/HanhViKhachHang', data);
export const profile = () => api.get('/api/KhachHang');
export const createProfile = (data) => api.post('/api/KhachHang', data);
export const updateProfile = (id, data) => api.put(`/api/KhachHang/${encodeURIComponent(id)}`, data);
export const addDocument = (id, data) => api.post(`/api/KhachHang/${encodeURIComponent(id)}/giay-to`, data);
export const updateDocument = (id, docId, data) => api.put(`/api/KhachHang/${encodeURIComponent(id)}/giay-to/${encodeURIComponent(docId)}`, data);
export const deleteDocument = (id, docId) => api.delete(`/api/KhachHang/${encodeURIComponent(id)}/giay-to/${encodeURIComponent(docId)}`);

export default api;
