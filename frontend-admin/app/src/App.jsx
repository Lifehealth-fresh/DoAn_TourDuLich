import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context';
import { Layout, Protected } from './components';
import { Login, Dashboard, TourAdminPage, BookingManagement, DesignRequests } from './pages';
import PromotionsManagement from './PromotionsManagement.jsx';
import SightseeingManagement from './SightseeingManagement.jsx';
import PartnersManagement from './PartnersManagement.jsx';

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/dang-nhap" element={<Login />} />
          <Route element={<Protected />}>
            <Route element={<Layout />}>
              <Route path="/" element={<Dashboard />} />
              <Route path="/tours" element={<TourAdminPage />} />
              <Route path="/booking" element={<BookingManagement />} />
              <Route path="/uu-dai" element={<PromotionsManagement />} />
              <Route path="/diem-tham-quan" element={<SightseeingManagement />} />
              <Route path="/doi-tac" element={<PartnersManagement />} />
              <Route path="/thiet-ke" element={<DesignRequests />} />
            </Route>
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
