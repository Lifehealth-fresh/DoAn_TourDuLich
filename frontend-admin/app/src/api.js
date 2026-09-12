import axios from 'axios';
const api=axios.create({baseURL:import.meta.env.VITE_API_BASE_URL||'https://localhost:7290',headers:{'Content-Type':'application/json'}});
api.interceptors.request.use(c=>{const t=localStorage.getItem('admin_token');if(t)c.headers.Authorization=`Bearer ${t}`;if(typeof FormData!=='undefined'&&c.data instanceof FormData){delete c.headers['Content-Type'];}return c;});
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const isLoginRequest = error.config?.method?.toLowerCase() === 'post' &&
      /\/api\/Auth\/login\/?(?:[?#]|$)/i.test(error.config?.url || '');
    if (error.response?.status === 401 && !isLoginRequest) {
      localStorage.removeItem('admin_token');
      if (window.location.pathname !== '/dang-nhap') {
        window.location.assign('/dang-nhap');
      }
    }
    return Promise.reject(error);
  },
);
export const errorMessage=(e,f='Có lỗi xảy ra.')=>e?.response?.data?.message||e?.response?.data?.title||(e?.response?.status===403?'Bạn không có quyền thực hiện thao tác này.':e?.response?.status===401?'Phiên đăng nhập đã hết hạn.':f);
export const login=data=>api.post('/api/Auth/login',data);
export const tours=(params)=>api.get('/api/Tour',{params:{pageSize:50,...params}}); export const createTour=d=>api.post('/api/Tour',d); export const updateTour=(id,d)=>api.put(`/api/Tour/${encodeURIComponent(id)}`,d); export const deleteTour=id=>api.delete(`/api/Tour/${encodeURIComponent(id)}`);
export const tourDetail=id=>api.get(`/api/Tour/${encodeURIComponent(id)}`);
export const tourSchedule=id=>api.get(`/api/LichTrinh/tour/${encodeURIComponent(id)}`);
export const createTourSchedule=data=>api.post('/api/LichTrinh',data);
export const deleteTourSchedule=id=>api.delete(`/api/LichTrinh/${encodeURIComponent(id)}`);
export const sightseeingPlaces=()=>api.get('/api/DiemThamQuan');
export const designRequests=()=>api.get('/api/YeuCauThietKe/danh-sach');
export const proposals=id=>api.get(`/api/YeuCauThietKe/${encodeURIComponent(id)}/de-xuat`);
export const generate=id=>api.post(`/api/YeuCauThietKe/${encodeURIComponent(id)}/sinh-de-xuat`);
export const reject=(id,{lyDo})=>api.put(`/api/YeuCauThietKe/${encodeURIComponent(id)}/tu-choi-boi-sale`,{lyDoTuChoi:lyDo});
export const submitDesignForApproval=id=>api.put(`/api/YeuCauThietKe/${encodeURIComponent(id)}/gui-duyet`);
export const approve=id=>api.put(`/api/YeuCauThietKe/${encodeURIComponent(id)}/duyet`);
export const designSchedule=tourId=>api.get(`/api/LichTrinh/tour/${encodeURIComponent(tourId)}`);
export const editDesignSchedule=(id,data)=>api.put(`/api/YeuCauThietKe/${encodeURIComponent(id)}/sua-lich-trinh`,data);
export const bookings=(params)=>api.get('/api/DatDichVu/danh-sach',{params:{pageSize:50,...params}});
export const booking=id=>api.get(`/api/DatDichVu/${encodeURIComponent(id)}`);
export const status=(id,s)=>api.put(`/api/DatDichVu/${encodeURIComponent(id)}/trang-thai`,{trangThai:s});
export const confirmRefund=id=>api.put(`/api/DatDichVu/${encodeURIComponent(id)}/xac-nhan-hoan-tien`);
export const promotions=()=>api.get('/api/KhuyenMai'); export const reviews=id=>api.get(`/api/DanhGia/tour/${id}`);
export const tourMedia=id=>api.get(`/api/AnhTour/theo-tour/${id}`); export const uploadTourMedia=(tourId,file,thuTu=0,isAvatar=false)=>{const data=new FormData();data.append('file',file);data.append('thuTu',String(thuTu));data.append('isAvatar',String(isAvatar));return api.post(`/api/AnhTour/theo-tour/${encodeURIComponent(tourId)}/upload`,data)}; export const replaceTourMedia=(mediaId,file,thuTu=0,isAvatar=false)=>{const data=new FormData();data.append('file',file);data.append('thuTu',String(thuTu));data.append('isAvatar',String(isAvatar));return api.put(`/api/AnhTour/${encodeURIComponent(mediaId)}/upload`,data)}; export const deleteMedia=id=>api.delete(`/api/AnhTour/${id}`);
export default api;
export const departures=id=>api.get('/api/Tour/'+encodeURIComponent(id)+'/lich-khoi-hanh');
export const createDeparture=data=>api.post('/api/LichKhoiHanh',data);
export const updateDeparture=(id,data)=>api.put('/api/LichKhoiHanh/'+encodeURIComponent(id),data);
export const departureGuests=id=>api.get('/api/LichKhoiHanh/'+encodeURIComponent(id)+'/khach');
export const guestProfile=id=>api.get('/api/DatDichVu/'+encodeURIComponent(id)+'/ho-so-khach');
export const currentDesignSchedule=id=>api.get('/api/YeuCauThietKe/'+encodeURIComponent(id)+'/lich-hien-tai');
