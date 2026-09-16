import axios from 'axios';

const TOKEN_KEY = 'wavv_token';
const REFRESH_KEY = 'wavv_refresh';
const USER_KEY = 'wavv_user';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://localhost:7290',
  headers: { 'Content-Type': 'application/json' },
});

const isAuthUrl = (url = '') =>
  /\/api\/Auth\/(?:login|register|refresh|logout)\/?(?:[?#]|$)/i.test(url);

export const newIdempotencyKey = () =>
  (typeof crypto !== 'undefined' && crypto.randomUUID)
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(16).slice(2)}`;

export const phoneError = (phone) => {
  const value = String(phone || '').trim();
  if (!/^0\d{9}$/.test(value)) return 'Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.';
  return '';
};

export const passwordError = (password) => {
  const value = String(password || '');
  if (value.length < 8) return 'Mật khẩu phải có ít nhất 8 ký tự.';
  if (!/[A-Za-z]/.test(value) || !/\d/.test(value)) return 'Mật khẩu phải gồm cả chữ và số.';
  return '';
};

const persistSession = (data) => {
  if (data?.token) localStorage.setItem(TOKEN_KEY, data.token);
  if (data?.refreshToken) localStorage.setItem(REFRESH_KEY, data.refreshToken);
};

const clearSession = () => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_KEY);
  localStorage.removeItem(USER_KEY);
};

let refreshing = null;

api.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY);
  if (token) config.headers.Authorization = `Bearer ${token}`;
  if (typeof FormData !== 'undefined' && config.data instanceof FormData) {
    if (typeof config.headers?.delete === 'function') config.headers.delete('Content-Type');
    else delete config.headers['Content-Type'];
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config || {};
    if (error.response?.status === 401 && !original._retry && !isAuthUrl(original.url || '')) {
      const refreshToken = localStorage.getItem(REFRESH_KEY);
      if (refreshToken) {
        original._retry = true;
        try {
          if (!refreshing) {
            refreshing = api.post('/api/Auth/refresh', { refreshToken })
              .then((res) => {
                persistSession(res.data);
                return res.data.token;
              })
              .finally(() => { refreshing = null; });
          }
          const token = await refreshing;
          original.headers = original.headers || {};
          original.headers.Authorization = `Bearer ${token}`;
          return api(original);
        } catch {
          clearSession();
        }
      } else {
        clearSession();
      }
      if (!['/dang-nhap', '/dang-ky', '/ho-so-moi'].includes(window.location.pathname)) {
        window.location.assign('/dang-nhap');
      }
    }
    return Promise.reject(error);
  },
);

export const errorMessage = (error, fallback = 'Có lỗi xảy ra. Vui lòng thử lại.') => {
  const data = error?.response?.data;
  if (typeof data === 'string' && data.trim()) return data;
  if (data?.message) return data.message;
  const fieldErrors = data?.errors;
  if (fieldErrors && typeof fieldErrors === 'object') {
    const parts = Object.values(fieldErrors).flat().filter(Boolean);
    if (parts.length) {
      const joined = parts.join(' ');
      if (/file field is required/i.test(joined) || /the file field/i.test(joined))
        return 'Không nhận được file. Chọn lại ảnh/video rồi gửi.';
      return joined;
    }
  }
  if (data?.title && data.title !== 'One or more validation errors occurred.') return data.title;
  if (error?.response?.status === 429) return 'Quá nhiều lần thử. Vui lòng đợi rồi thử lại.';
  if (error?.message === 'Network Error') return 'Không kết nối được máy chủ. Kiểm tra API đang chạy.';
  return fallback;
};

export const persistAuth = persistSession;
export const clearAuth = clearSession;

export const login = (data) => api.post('/api/Auth/login', data);
export const register = (data) => api.post('/api/Auth/register', data);
export const logoutSession = (refreshToken) => api.post('/api/Auth/logout', { refreshToken });
export const tours = () => api.get('/api/Tour', { params: { pageSize: 100 } });
export const tourDetail = (id) => api.get(`/api/Tour/${encodeURIComponent(id)}`);
export const departures = (id) => api.get(`/api/Tour/${encodeURIComponent(id)}/lich-khoi-hanh`);
export const itinerary = (id) => api.get(`/api/LichTrinh/tour/${encodeURIComponent(id)}`);
export const tourPhotos = (id) => api.get(`/api/Tour/${encodeURIComponent(id)}/anh`);
export const reviews = (id) => api.get(`/api/DanhGia/tour/${encodeURIComponent(id)}`);
export const createTourReview = (data) => api.post('/api/DanhGia/tour', data);
export const updateTourReview = (id, data) => api.put(`/api/DanhGia/tour/${encodeURIComponent(id)}`, data);
export const uploadReviewMedia = (file) => {
  const data = new FormData();
  data.append('file', file);
  return api.post('/api/DanhGia/media/upload', data);
};
export const savedTours = () => api.get('/api/DanhSachYeuThich/cua-toi');
export const saveTour = (data) => api.post('/api/DanhSachYeuThich', data);
export const removeSavedTour = (id) => api.delete(`/api/DanhSachYeuThich/${encodeURIComponent(id)}`);
export const recommendations = () => api.get('/api/AiGoiY/cua-toi');
export const generateRecommendations = (data = {}) => api.post('/api/AiGoiY/sinh-goi-y', { SoLuong: 5, Alpha: 0.5, ...data });
export const designRequests = (params) => api.get('/api/YeuCauThietKe/cua-toi', { params });
export const designRequestDetail = (id) => api.get(`/api/YeuCauThietKe/${encodeURIComponent(id)}`);
export const createDesignRequest = (data) => api.post('/api/YeuCauThietKe', data);
export const designProposals = (id) => api.get(`/api/YeuCauThietKe/${encodeURIComponent(id)}/de-xuat`);
export const chooseProposal = (requestId, proposalId) => api.put(`/api/YeuCauThietKe/${encodeURIComponent(requestId)}/chon-de-xuat/${encodeURIComponent(proposalId)}`);
export const bookings = (params) => api.get('/api/DatDichVu/cua-toi', { params });
export const bookingDetail = (id) => api.get(`/api/DatDichVu/${encodeURIComponent(id)}`);
export const createBooking = (data) => api.post('/api/DatDichVu', data);
export const cancelBooking = (id) => api.put(`/api/DatDichVu/${encodeURIComponent(id)}/huy`);
export const bookingPayments = (id) => api.get(`/api/ThanhToan/theo-booking/${encodeURIComponent(id)}`);
export const paymentSummary = (id) => api.get(`/api/ThanhToan/theo-booking/${encodeURIComponent(id)}/tong-hop`);
export const createPayment = (data, config = {}) => api.post('/api/ThanhToan', data, {
  ...config,
  headers: {
    'Idempotency-Key': config?.headers?.['Idempotency-Key'] || newIdempotencyKey(),
    ...config.headers,
  },
});
export const createGatewayPayment = (data, config = {}) => api.post('/api/ThanhToan/tao-phien-cong', data, {
  ...config,
  headers: {
    'Idempotency-Key': config?.headers?.['Idempotency-Key'] || newIdempotencyKey(),
    ...config.headers,
  },
});
export const bookingContract = (id) => api.get(`/api/HopDong/theo-booking/${encodeURIComponent(id)}`);
export const promotions = () => api.get('/api/KhuyenMai');
export const applyPromotion = (data) => api.post('/api/KhuyenMai/ap-dung', data);
export const logBehavior = (data) => api.post('/api/HanhViKhachHang', data);
export const profile = () => api.get('/api/KhachHang');
export const createProfile = (data) => api.post('/api/KhachHang', data);
export const updateProfile = (id, data) => api.put(`/api/KhachHang/${encodeURIComponent(id)}`, data);
export const addDocument = (id, data) => api.post(`/api/KhachHang/${encodeURIComponent(id)}/giay-to`, data);
export const updateDocument = (id, docId, data) => api.put(`/api/KhachHang/${encodeURIComponent(id)}/giay-to/${encodeURIComponent(docId)}`, data);
export const deleteDocument = (id, docId) => api.delete(`/api/KhachHang/${encodeURIComponent(id)}/giay-to/${encodeURIComponent(docId)}`);
export const uploadDocumentImage = (id, docId, file, mat = 'Truoc') => {
  const data = new FormData();
  data.append('file', file);
  return api.post(`/api/KhachHang/${encodeURIComponent(id)}/giay-to/${encodeURIComponent(docId)}/anh?mat=${encodeURIComponent(mat)}`, data);
};
export const paperImageBlob = (id, docId, mat = 'Truoc') =>
  api.get(`/api/KhachHang/${encodeURIComponent(id)}/giay-to/${encodeURIComponent(docId)}/anh?mat=${encodeURIComponent(mat)}`, { responseType: 'blob' });

export const connectSupportHub = (onEvent) => {
  const Hub = window.signalR?.HubConnectionBuilder;
  const token = () => localStorage.getItem(TOKEN_KEY);
  if (!Hub || !token()) return () => {};
  const HttpTransportType = window.signalR.HttpTransportType || {};
  const transports = (HttpTransportType.WebSockets || 1)
    | (HttpTransportType.ServerSentEvents || 2)
    | (HttpTransportType.LongPolling || 4);
  const connection = new window.signalR.HubConnectionBuilder()
    .withUrl(`${api.defaults.baseURL}/hubs/hotro`, {
      accessTokenFactory: () => token() || '',
      transport: transports,
      withCredentials: true,
    })
    .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
    .build();
  connection.on('hotro', onEvent);
  connection.start().catch(() => {});
  return () => { connection.stop().catch(() => {}); };
};

export default api;
export const currentDesignSchedule=id=>api.get('/api/YeuCauThietKe/'+encodeURIComponent(id)+'/lich-hien-tai');
export const agreeDesignSchedule=id=>api.put('/api/YeuCauThietKe/'+encodeURIComponent(id)+'/dong-y-lich');
export const requestDesignRevision=(id,lyDo)=>api.put('/api/YeuCauThietKe/'+encodeURIComponent(id)+'/yeu-cau-chinh-sua',{lyDo});
export const provinces = (q) => api.get('/api/TinhThanh', { params: q ? { q } : {} });
export const mySupport = (danhDauDoc = false) => api.get('/api/HoTro/cua-toi', { params: { danhDauDoc } });
export const sendSupport = (noiDung) => api.post('/api/HoTro/cua-toi', { noiDung });
export const startSupportSession = () => api.post('/api/HoTro/cua-toi/phien-moi');
