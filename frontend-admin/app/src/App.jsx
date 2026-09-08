import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context';
import { Layout, Protected } from './components';
import { Login, Dashboard, TourAdminPage, BookingManagement } from './pages';

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
            </Route>
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
