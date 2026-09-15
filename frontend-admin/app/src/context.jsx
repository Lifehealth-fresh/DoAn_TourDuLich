import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { login as loginApi, me as meApi, logoutSession, clearAuth } from './api';

const C = createContext(null);
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

const decode = (token) => {
  try {
    return JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
  } catch {
    return {};
  }
};

export const roleOf = (user) => user?.role || user?.[ROLE_CLAIM] || '';

export const MODULES = [
  { ma: 'TongQuan', ten: 'Tổng quan' },
  { ma: 'Tour', ten: 'Quản lý tour' },
  { ma: 'Booking', ten: 'Quản lý booking' },
  { ma: 'UuDai', ten: 'Quản lý ưu đãi' },
  { ma: 'DiemThamQuan', ten: 'Điểm tham quan' },
  { ma: 'DoiTac', ten: 'Đối tác' },
  { ma: 'ThietKe', ten: 'Thiết kế' },
  { ma: 'DanhGia', ten: 'Đánh giá nhận xét' },
  { ma: 'KhachHang', ten: 'Quản lý khách hàng' },
  { ma: 'TaiKhoan', ten: 'Tài khoản' },
];

const NAV = [
  { module: 'TongQuan', to: '/', label: 'Tổng quan' },
  { module: 'Tour', to: '/tours', label: 'Quản lý tour' },
  { module: 'Booking', to: '/booking', label: 'Booking' },
  { module: 'UuDai', to: '/uu-dai', label: 'Ưu đãi' },
  { module: 'DiemThamQuan', to: '/diem-tham-quan', label: 'Điểm tham quan' },
  { module: 'DoiTac', to: '/doi-tac', label: 'Đối tác' },
  { module: 'ThietKe', to: '/thiet-ke', label: 'Thiết kế' },
  { module: 'DanhGia', to: '/danh-gia', label: 'Đánh giá nhận xét' },
  { module: 'KhachHang', to: '/khach-hang', label: 'Khách hàng' },
  { module: 'TaiKhoan', to: '/tai-khoan', label: 'Tài khoản' },
];

const readQuyen = () => {
  try {
    return JSON.parse(localStorage.getItem('admin_quyen') || '[]');
  } catch {
    return [];
  }
};

export function canGrant(grants, role, module, action) {
  if (role === 'Admin') return true;
  const list = Array.isArray(grants) ? grants : [];
  const row = list.find((item) => item.chucNang === module);
  if (!row) {
    return Boolean(!list.length && role === 'Sale' && module !== 'TaiKhoan');
  }
  if (row.toanQuyen) return true;
  if (action === 'Xem') return Boolean(row.them || row.sua || row.xoa);
  if (action === 'Them') return Boolean(row.them);
  if (action === 'Sua') return Boolean(row.sua);
  if (action === 'Xoa') return Boolean(row.xoa);
  return false;
}

export function firstAllowedPath(grants, role) {
  return NAV.find((item) => canGrant(grants, role, item.module, 'Xem'))?.to || '/';
}

export function AuthProvider({ children }) {
  const [token, setToken] = useState(localStorage.getItem('admin_token'));
  const [quyen, setQuyen] = useState(readQuyen);
  const user = token ? decode(token) : null;
  const role = roleOf(user);

  const persist = (nextToken, nextQuyen, refreshToken) => {
    localStorage.setItem('admin_token', nextToken);
    localStorage.setItem('admin_quyen', JSON.stringify(nextQuyen || []));
    if (refreshToken) localStorage.setItem('admin_refresh', refreshToken);
    setToken(nextToken);
    setQuyen(nextQuyen || []);
  };

  const login = async (data) => {
    const phone = String(data.SoDienThoai || '').trim();
    const password = String(data.MatKhau || '');
    if (!/^0\d{9}$/.test(phone)) {
      throw new Error('Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.');
    }
    if (password.length < 8) {
      throw new Error('Mật khẩu phải có ít nhất 8 ký tự.');
    }
    const response = (await loginApi({ ...data, SoDienThoai: phone })).data;
    const claims = decode(response.token);
    const nextRole = roleOf(claims);
    if (!['Admin', 'Sale'].includes(nextRole)) {
      try { await logoutSession(response.refreshToken); } catch { /* không giữ phiên khách trên trang vận hành */ }
      throw new Error('Tài khoản này không có quyền quản trị.');
    }
    persist(response.token, response.quyen || [], response.refreshToken);
    return { ...response, home: firstAllowedPath(response.quyen || [], nextRole) };
  };

  const logout = () => {
    const refreshToken = localStorage.getItem('admin_refresh');
    logoutSession(refreshToken).catch(() => {});
    clearAuth();
    localStorage.removeItem('admin_token');
    localStorage.removeItem('admin_quyen');
    localStorage.removeItem('admin_refresh');
    setToken(null);
    setQuyen([]);
  };

  useEffect(() => {
    if (!token) return;
    meApi().then((response) => {
      if (Array.isArray(response.data?.quyen)) {
        localStorage.setItem('admin_quyen', JSON.stringify(response.data.quyen));
        setQuyen(response.data.quyen);
      }
    }).catch(() => {});
  }, [token]);

  const can = (module, action) => canGrant(quyen, role, module, action);
  const navItems = NAV.filter((item) => can(item.module, 'Xem'));

  return (
    <C.Provider value={useMemo(() => ({
      token, user, quyen, login, logout, can, navItems, role,
      isStaff: Boolean(token && ['Admin', 'Sale'].includes(role)),
      isAdmin: role === 'Admin',
    }), [token, user, quyen, role])}>
      {children}
    </C.Provider>
  );
}

export const useAuth = () => useContext(C);
