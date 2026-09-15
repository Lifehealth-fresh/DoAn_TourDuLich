import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context';
import { Layout, Protected, ModuleGate } from './components';
import { Login, Dashboard, TourAdminPage, BookingManagement, DesignRequests } from './pages';
import PromotionsManagement from './PromotionsManagement.jsx';
import SightseeingManagement from './SightseeingManagement.jsx';
import PartnersManagement from './PartnersManagement.jsx';
import AccountsManagement from './AccountsManagement.jsx';
import ReviewsManagement from './ReviewsManagement.jsx';
import CustomersManagement from './CustomersManagement.jsx';
import SupportInbox from './SupportInbox.jsx';

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/dang-nhap" element={<Login />} />
          <Route element={<Protected />}>
            <Route element={<Layout />}>
              <Route path="/" element={<ModuleGate module="TongQuan"><Dashboard /></ModuleGate>} />
              <Route path="/tours" element={<ModuleGate module="Tour"><TourAdminPage /></ModuleGate>} />
              <Route path="/booking" element={<ModuleGate module="Booking"><BookingManagement /></ModuleGate>} />
              <Route path="/uu-dai" element={<ModuleGate module="UuDai"><PromotionsManagement /></ModuleGate>} />
              <Route path="/diem-tham-quan" element={<ModuleGate module="DiemThamQuan"><SightseeingManagement /></ModuleGate>} />
              <Route path="/doi-tac" element={<ModuleGate module="DoiTac"><PartnersManagement /></ModuleGate>} />
              <Route path="/thiet-ke" element={<ModuleGate module="ThietKe"><DesignRequests /></ModuleGate>} />
              <Route path="/danh-gia" element={<ModuleGate module="DanhGia"><ReviewsManagement /></ModuleGate>} />
              <Route path="/khach-hang" element={<ModuleGate module="KhachHang"><CustomersManagement /></ModuleGate>} />
              <Route path="/ho-tro" element={<ModuleGate module="HoTro"><SupportInbox /></ModuleGate>} />
              <Route path="/tai-khoan" element={<ModuleGate module="TaiKhoan"><AccountsManagement /></ModuleGate>} />
            </Route>
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
