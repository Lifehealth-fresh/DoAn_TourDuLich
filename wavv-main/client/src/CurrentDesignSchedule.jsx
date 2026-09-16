import {useState} from 'react';
const money=v=>Number(v||0).toLocaleString('vi-VN')+' đ';
const dayLabel=(start,day)=>{
  if(!start||!day) return `Ngày ${day||''}`;
  const [y,m,d]=String(start).slice(0,10).split('-').map(Number);
  if(!y||!m||!d) return `Ngày ${day}`;
  const dt=new Date(y,m-1,d+Number(day)-1);
  return `Ngày ${String(dt.getDate()).padStart(2,'0')}/${String(dt.getMonth()+1).padStart(2,'0')}/${dt.getFullYear()}`;
};
export default function CurrentDesignSchedule({schedule,busy,onRespond}) {

  const [reason,setReason]=useState('');
  const canRespond=schedule.trangThai==='ChoKhachXacNhan';
  const lines=[...(schedule.lichTrinh||[])];
  const days=[...new Set(lines.map(l=>l.ngayThu))];
  return <section className="design-form">
    <h2>Lịch trình hiện tại đã lưu</h2><h3>{schedule.tenTour}</h3>
    <p>Giá tour hiện tại: <b>{money(schedule.giaTour)}</b></p>
    {schedule.soNgay != null && <p>{schedule.soNgay} ngày{schedule.soDem != null ? ` / ${schedule.soDem} đêm` : ''}{schedule.spillSangHomSau ? ` · trả phòng sáng ${schedule.ngayTraPhong || ''}` : ''}</p>}
    <p>Đây là lịch trình Admin/Sale đã lưu, không phải đề xuất ban đầu.</p>
    {days.map(day=>{
      const first=lines.find(l=>l.ngayThu===day);
      return <section key={day} className="day-card">
        <header><b>{first?.ngayLich?`Ngày ${first.ngayLich}`:dayLabel(schedule.ngayDuKienDi,day)}</b></header>
        <ol>{lines.filter(l=>l.ngayThu===day).map(l=><li key={l.maLichTrinh}>
          <b>{l.gioBatDau ? `${l.gioBatDau} · ` : `Mục ${l.thuTuTrongNgay}: `}{l.laKhachSan ? `Khách sạn · ${l.tenDoiTac||''} · ${l.tenSanPham}` : (l.tenDiaDanh||l.tenSanPham||'Hoạt động')}</b>
          <p>{l.mota}</p>
          <p>{l.laKhachSan ? `${money(l.donGia)} / đêm` : (l.tenSanPham||'Không kèm sản phẩm')} · SL {l.soLuong} × {money(l.donGia)} = {money(l.thanhTien)}</p>
        </li>)}</ol>
      </section>;
    })}
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
