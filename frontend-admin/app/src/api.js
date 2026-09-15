import axios from 'axios';
const api=axios.create({baseURL:import.meta.env.VITE_API_BASE_URL||'https://localhost:7290',headers:{'Content-Type':'application/json'}});
const isAuthUrl=(url='')=>/\/api\/Auth\/(?:login|refresh|logout)\/?(?:[?#]|$)/i.test(url);
let refreshing=null;
const persistSession=(data)=>{if(data?.token)localStorage.setItem('admin_token',data.token);if(data?.refreshToken)localStorage.setItem('admin_refresh',data.refreshToken);if(Array.isArray(data?.quyen))localStorage.setItem('admin_quyen',JSON.stringify(data.quyen));};
const clearSession=()=>{localStorage.removeItem('admin_token');localStorage.removeItem('admin_refresh');localStorage.removeItem('admin_quyen');};
api.interceptors.request.use(c=>{const t=localStorage.getItem('admin_token');if(t)c.headers.Authorization=`Bearer ${t}`;if(typeof FormData!=='undefined'&&c.data instanceof FormData){delete c.headers['Content-Type'];}return c;});
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config || {};
    if (error.response?.status === 401 && !original._retry && !isAuthUrl(original.url || '')) {
      const refreshToken = localStorage.getItem('admin_refresh');
      if (refreshToken) {
        original._retry = true;
        try {
          if (!refreshing) {
            refreshing = api.post('/api/Auth/refresh', { refreshToken })
              .then((res) => { persistSession(res.data); return res.data.token; })
              .finally(() => { refreshing = null; });
          }
          original.headers = original.headers || {};
          original.headers.Authorization = `Bearer ${await refreshing}`;
          return api(original);
        } catch {
          clearSession();
        }
      } else {
        clearSession();
      }
      if (window.location.pathname !== '/dang-nhap') window.location.assign('/dang-nhap');
    }
    return Promise.reject(error);
  },
);
export const persistAuth=persistSession;
export const clearAuth=clearSession;
export const errorMessage=(e,f='Có lỗi xảy ra.')=>e?.response?.data?.message||e?.response?.data?.title||(e?.response?.status===429?'Quá nhiều lần thử. Vui lòng đợi rồi thử lại.':e?.response?.status===403?'Bạn không có quyền thực hiện thao tác này.':e?.response?.status===401?'Phiên đăng nhập đã hết hạn.':f);
export const login=data=>api.post('/api/Auth/login',data);
export const logoutSession=(refreshToken)=>api.post('/api/Auth/logout',{refreshToken});
export const tours=(params)=>api.get('/api/Tour',{params:{pageSize:100,...params}}); export const createTour=d=>api.post('/api/Tour',d); export const updateTour=(id,d)=>api.put(`/api/Tour/${encodeURIComponent(id)}`,d); export const deleteTour=id=>api.delete(`/api/Tour/${encodeURIComponent(id)}`);
export const tourDetail=id=>api.get(`/api/Tour/${encodeURIComponent(id)}`);
export const tourSchedule=id=>api.get(`/api/LichTrinh/tour/${encodeURIComponent(id)}`);
export const createTourSchedule=data=>api.post('/api/LichTrinh',data);
export const deleteTourSchedule=id=>api.delete(`/api/LichTrinh/${encodeURIComponent(id)}`);
export const provinces=(q)=>api.get('/api/TinhThanh',{params:q?{q}:{}});
export const sightseeingPlaces=()=>api.get('/api/DiemThamQuan');
export const createSightseeing=data=>api.post('/api/DiemThamQuan',data);
export const updateSightseeing=(id,data)=>api.put(`/api/DiemThamQuan/${encodeURIComponent(id)}`,data);
export const deleteSightseeing=id=>api.delete(`/api/DiemThamQuan/${encodeURIComponent(id)}`);
export const regions=()=>api.get('/api/KhuVuc');
export const partners=(params)=>api.get('/api/DoiTac',{params});
export const createPartner=data=>api.post('/api/DoiTac',data);
export const updatePartner=(id,data)=>api.put(`/api/DoiTac/${encodeURIComponent(id)}`,data);
export const deletePartner=id=>api.delete(`/api/DoiTac/${encodeURIComponent(id)}`);
export const partnerProducts=(params)=>api.get('/api/SanPhamDoiTac',{params});
export const createPartnerProduct=data=>api.post('/api/SanPhamDoiTac',data);
export const deletePartnerProduct=id=>api.delete(`/api/SanPhamDoiTac/${encodeURIComponent(id)}`);
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
export const promotions=(params)=>api.get('/api/KhuyenMai',{params}); export const reviews=id=>api.get(`/api/DanhGia/tour/${id}`);
export const promotionDetail=id=>api.get(`/api/KhuyenMai/${encodeURIComponent(id)}`);
export const createPromotion=data=>api.post('/api/KhuyenMai',data);
export const updatePromotion=(id,data)=>api.put(`/api/KhuyenMai/${encodeURIComponent(id)}`,data);
export const deletePromotion=id=>api.delete(`/api/KhuyenMai/${encodeURIComponent(id)}`);
export const tourMedia=id=>api.get(`/api/AnhTour/theo-tour/${id}`); export const uploadTourMedia=(tourId,file,thuTu=0,isAvatar=false)=>{const data=new FormData();data.append('file',file);data.append('thuTu',String(thuTu));data.append('isAvatar',String(isAvatar));return api.post(`/api/AnhTour/theo-tour/${encodeURIComponent(tourId)}/upload`,data)}; export const replaceTourMedia=(mediaId,file,thuTu=0,isAvatar=false)=>{const data=new FormData();data.append('file',file);data.append('thuTu',String(thuTu));data.append('isAvatar',String(isAvatar));return api.put(`/api/AnhTour/${encodeURIComponent(mediaId)}/upload`,data)}; export const deleteMedia=id=>api.delete(`/api/AnhTour/${id}`);
export const setTourCover=id=>api.put(`/api/AnhTour/${encodeURIComponent(id)}/dai-dien`);
export default api;
export const departures=id=>api.get('/api/Tour/'+encodeURIComponent(id)+'/lich-khoi-hanh');
export const createDeparture=data=>api.post('/api/LichKhoiHanh',data);
export const updateDeparture=(id,data)=>api.put('/api/LichKhoiHanh/'+encodeURIComponent(id),data);
export const departureGuests=id=>api.get('/api/LichKhoiHanh/'+encodeURIComponent(id)+'/khach');
export const guestProfile=id=>api.get('/api/DatDichVu/'+encodeURIComponent(id)+'/ho-so-khach');
export const updateGuestProfile=(id,data)=>api.put('/api/DatDichVu/'+encodeURIComponent(id)+'/ho-so-khach',data);
export const addGuestDocument=(id,data)=>api.post('/api/DatDichVu/'+encodeURIComponent(id)+'/ho-so-khach/giay-to',data);
export const updateGuestDocument=(id,docId,data)=>api.put('/api/DatDichVu/'+encodeURIComponent(id)+'/ho-so-khach/giay-to/'+encodeURIComponent(docId),data);
export const deleteGuestDocument=(id,docId)=>api.delete('/api/DatDichVu/'+encodeURIComponent(id)+'/ho-so-khach/giay-to/'+encodeURIComponent(docId));
export const overview=()=>api.get('/api/BaoCao/tong-quan');
export const currentDesignSchedule=id=>api.get('/api/YeuCauThietKe/'+encodeURIComponent(id)+'/lich-hien-tai');
export const me=()=>api.get('/api/Auth/toi');
export const accounts=(params)=>api.get('/api/Admin/tai-khoan',{params});
export const account=id=>api.get('/api/Admin/tai-khoan/'+encodeURIComponent(id));
export const createAccount=data=>api.post('/api/Admin/tai-khoan',data);
export const updateAccountGrants=(id,data)=>api.put('/api/Admin/tai-khoan/'+encodeURIComponent(id)+'/quyen',data);
export const changeAccountRole=(id,tenVaiTro)=>api.put('/api/Admin/tai-khoan/'+encodeURIComponent(id)+'/vai-tro',{tenVaiTro});

