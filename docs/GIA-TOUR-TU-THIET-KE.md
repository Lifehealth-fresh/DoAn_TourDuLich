# Giá tour tự thiết kế

Backend là nguồn chân lý duy nhất. Công thức hiện hành là:

`thành tiền dòng = max(0, giá niêm yết sản phẩm) × max(0, số lượng)`

`giá tour = tổng thành tiền các dòng lịch trình`

`DeXuatLichTrinhService` và thao tác Sale sửa lịch trình đều gọi
`TuThietKeTourPricing`; frontend chỉ gửi mã sản phẩm/điểm và số lượng, rồi hiển
thị giá backend trả về. Chưa có trường hoa hồng đối tác hoặc khuyến mãi cho tour
tự thiết kế, vì vậy hai khoản này hiện bằng 0. Khi bổ sung, công thức phải được
mở rộng duy nhất tại helper này.
