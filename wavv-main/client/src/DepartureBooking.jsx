import {useState} from 'react';
import {futureDepartures,departureTime,bookingError} from './departureAvailability.mjs';
export default function DepartureBooking({dates,price,allowed=true,busy,onBook}) {
  const [id,setId]=useState(''),[adults,setAdults]=useState(1),[children,setChildren]=useState(0);
  const future=futureDepartures(dates);
  const selected=id||future.find(d=>d.conTrong>0)?.maKhoiHanh||'';
  const departure=future.find(d=>d.maKhoiHanh===selected);
  const error=bookingError(departure,adults,children);
  return <aside className="booking-box sticky">
    <p className="muted">Giá từ</p><h3 className="book-price">{Number(price).toLocaleString('vi-VN')} đ<small>/ khách</small></h3>
    <h3>Lịch khởi hành</h3>
    {future.map(d=><p key={d.maKhoiHanh}>{new Date(departureTime(d.ngayKhoiHanh)).toLocaleString('vi-VN')} · Sức chứa {d.sucChua} · Đã đặt {d.daDat} · Còn trống {d.conTrong}</p>)}
    {!future.length&&<p className="muted">Chưa có lịch khởi hành</p>}
    {allowed?<>
      <label>Chọn lịch<select value={selected} disabled={busy||!future.length} onChange={e=>setId(e.target.value)}>
        <option disabled value="">Chọn lịch còn chỗ</option>
        {future.map(d=><option disabled={d.conTrong<=0} key={d.maKhoiHanh} value={d.maKhoiHanh}>
          {new Date(departureTime(d.ngayKhoiHanh)).toLocaleDateString('vi-VN')} · Còn {d.conTrong}/{d.sucChua} chỗ
        </option>)}
      </select></label>
      <div className="people-fields"><label>Người lớn<input type="number" min="0" step="1" disabled={busy} value={adults} onChange={e=>setAdults(e.target.value)}/></label>
        <label>Trẻ em<input type="number" min="0" step="1" disabled={busy} value={children} onChange={e=>setChildren(e.target.value)}/></label></div>
      <div className="total-row"><span>{+adults + +children} khách</span><b>{(price*(+adults + +children)).toLocaleString('vi-VN')} đ</b></div>
      {!!future.length&&error&&<p role="alert" className="form-error">{error}</p>}
      <button disabled={busy||!!error} className="primary-button full" onClick={()=>{if(!bookingError(departure,adults,children))onBook(departure,+adults,+children);}}>
        {busy?'Đang giữ chỗ...':'Đặt tour này'}
      </button><p className="muted">Có thể đặt cọc 30% sau khi giữ chỗ.</p>
    </>:<p>Tour tự thiết kế chưa được duyệt. Chưa thể đặt tour.</p>}
  </aside>;
}
