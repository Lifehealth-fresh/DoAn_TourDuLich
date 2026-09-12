import {useState} from 'react';
const money=v=>Number(v||0).toLocaleString('vi-VN')+' đ';
export default function CurrentDesignSchedule({schedule,busy,onRespond}) {
  const [reason,setReason]=useState('');
  const canRespond=schedule.trangThai==='ChoKhachXacNhan';
  return <section className="design-form">
    <h2>Lịch trình hiện tại đã lưu</h2><h3>{schedule.tenTour}</h3>
    <p>Giá tour hiện tại: <b>{money(schedule.giaTour)}</b></p>
    <p>Đây là lịch trình Admin/Sale đã lưu, không phải đề xuất ban đầu.</p>
    <ol>{(schedule.lichTrinh||[]).map(l=><li key={l.maLichTrinh}>
      <b>Ngày {l.ngayThu} · Mục {l.thuTuTrongNgay}: {l.tenDiaDanh||l.tenSanPham||'Hoạt động'}</b>
      <p>{l.mota}</p><p>{l.tenSanPham||'Không kèm sản phẩm'} · SL {l.soLuong} × {money(l.donGia)} = {money(l.thanhTien)}</p>
    </li>)}</ol>
    {canRespond&&<>
      <button disabled={busy||!schedule.lichTrinh?.length} className="primary-button" onClick={()=>onRespond(null)}>Đồng ý lịch này</button>
      <form onSubmit={e=>{e.preventDefault();if(reason.trim()&&!busy)onRespond(reason.trim());}}>
        <label>Lý do yêu cầu chỉnh lại<textarea required value={reason} disabled={busy} onChange={e=>setReason(e.target.value)}/></label>
        <button className="outline-button" disabled={busy||!reason.trim()}>Yêu cầu chỉnh lại</button>
      </form>
    </>}
    {schedule.trangThai==='ChoDuyet'&&<p>Bạn đã đồng ý lịch này. Đang chờ Admin duyệt.</p>}
    {['DangThietKe','CanChinhSua'].includes(schedule.trangThai)&&<p>Admin/Sale đang chỉnh sửa. Bạn có thể xác nhận sau khi họ gửi duyệt.</p>}
  </section>;
}
