# Admin/Sale app

React/Vite dashboard cho vai trò `Admin` và `Sale`. Chạy tại `http://localhost:5174`:

```bash
npm install
npm run dev
```

Tài khoản khác `Admin`/`Sale` bị chặn ở frontend và backend bằng RBAC.
SĐT đăng nhập 10 số; mật khẩu nhân viên mới tối thiểu 8 ký tự gồm chữ và số.
Access token 2 giờ, trang tự gọi `/api/Auth/refresh`.
Sale/Admin chỉ duyệt tay thanh toán `TienMat` và `ChuyenKhoan`; giao dịch `VNPay`/`MoMo` do IPN hợp lệ xác nhận.
