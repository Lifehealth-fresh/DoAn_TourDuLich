import {useEffect,useState} from 'react';
import * as api from './api';
import {Notice} from './components';
const instant=v=>v?new Date(/(?:Z|[+-]\d\d:\d\d)$/i.test(v)?v:v+'Z'):null;
const stamp=v=>instant(v)?.toLocaleString('vi-VN')||'Chưa xác định';
const inputDate=v=>{const d=instant(v);return d?new Date(d.getTime()-d.getTimezoneOffset()*60000).toISOString().slice(0,16):'';};
const day=v=>v?new Date(v).toLocaleDateString('vi-VN'):'—';
const status={ChoXacNhan:'Chờ xác nhận',DaXacNhan:'Đã xác nhận',DaThanhToan:'Đã thanh toán',HoanThanh:'Hoàn thành'};
export default function DepartureGuests({tourId,defaultCapacity,disabled=false}) {
  const empty=()=>({maKhoiHanh:'',ngayKhoiHanh:'',ngayKetThuc:'',diaDiem:'',soCho:defaultCapacity});
  const [dates,setDates]=useState([]),[roster,setRoster]=useState(null),[profile,setProfile]=useState(null);
  const [form,setForm]=useState(empty),[editing,setEditing]=useState(''),[busy,setBusy]=useState(false),[error,setError]=useState('');
  const [refresh,setRefresh]=useState(0),[departureId,setDepartureId]=useState(''),[bookingId,setBookingId]=useState('');
  useEffect(()=>{if(!editing)setForm(current=>({...current,soCho:defaultCapacity}));},[defaultCapacity]);
  useEffect(()=>{let active=true;setError('');setBusy(true);
    Promise.all([api.departures(tourId),departureId?api.departureGuests(departureId):Promise.resolve(null)])
      .then(([d,r])=>{if(active){setDates(d.data);setRoster(r?.data||null);if(bookingId&&!r?.data.bookings.some(b=>b.maBooking===bookingId)){setBookingId('');setProfile(null);}}})
      .catch(e=>{if(active)setError(api.errorMessage(e));}).finally(()=>{if(active)setBusy(false);});
    return()=>{active=false;};
  },[tourId,defaultCapacity,departureId,refresh]);
  useEffect(()=>{let active=true;setProfile(null);if(bookingId)api.guestProfile(bookingId).then(r=>{if(active)setProfile(r.data);}).catch(e=>{if(active)setError(api.errorMessage(e));});return()=>{active=false;};},[bookingId,refresh]);
  const save=async e=>{e.preventDefault();if(busy||disabled)return;setBusy(true);setError('');
    try{
      const payload={...form,maTour:tourId,soCho:form.soCho===''?null:Number(form.soCho),
        ngayKhoiHanh:form.ngayKhoiHanh?new Date(form.ngayKhoiHanh).toISOString():null,
        ngayKetThuc:form.ngayKetThuc?new Date(form.ngayKetThuc).toISOString():null};
      if(payload.ngayKetThuc&&payload.ngayKetThuc<payload.ngayKhoiHanh)throw Error('Ngày kết thúc phải sau ngày khởi hành.');
      if(editing)await api.updateDeparture(editing,payload);else await api.createDeparture(payload);
      setEditing('');setForm(empty());setRefresh(x=>x+1);
    }catch(e){setError(e.response?api.errorMessage(e):e.message);}finally{setBusy(false);}
  };
  const blocked=busy||disabled;
  return <section className="panel" style={{marginBottom:24,overflowWrap:'anywhere'}}>
    <header className="panel-head"><h2>Lịch khởi hành và khách</h2><button disabled={blocked} onClick={()=>setRefresh(x=>x+1)}>Tải lại chỗ và khách</button></header>
    <Notice error={error}/>
    {!dates.length&&!busy&&<p>Chưa có lịch khởi hành.</p>}
    <div className="table">{dates.map(d=><div className="row" key={d.maKhoiHanh} style={{display:'flex',gap:12,flexWrap:'wrap'}}>
      <button disabled={blocked} onClick={()=>{setBookingId('');setProfile(null);setRoster(null);setDepartureId(d.maKhoiHanh);setRefresh(x=>x+1);}}>{stamp(d.ngayKhoiHanh)}</button>
      <span>Sức chứa {d.sucChua} · Đã đặt {d.daDat} · Còn trống {d.conTrong}</span>
      <b>{d.soTaiKhoan} tài khoản · {d.daDat}/{d.sucChua} chỗ</b>
      <button disabled={blocked} onClick={()=>{setEditing(d.maKhoiHanh);setForm({...d,ngayKhoiHanh:inputDate(d.ngayKhoiHanh),ngayKetThuc:inputDate(d.ngayKetThuc),soCho:d.soCho??defaultCapacity});}}>Sửa lịch / số chỗ</button>
    </div>)}</div>
    <form onSubmit={save}><fieldset disabled={blocked} style={{border:0,padding:0}}>
      <h3>{editing?'Sửa lịch '+editing:'Thêm lịch khởi hành'}</h3>
      <div style={{display:'grid',gridTemplateColumns:'repeat(auto-fit,minmax(180px,1fr))',gap:12}}>
        <label>Mã lịch<input required maxLength={20} readOnly={!!editing} value={form.maKhoiHanh} onChange={e=>setForm({...form,maKhoiHanh:e.target.value})}/></label>
        <label>Khởi hành<input type="datetime-local" required value={form.ngayKhoiHanh} onChange={e=>setForm({...form,ngayKhoiHanh:e.target.value})}/></label>
        <label>Kết thúc<input type="datetime-local" value={form.ngayKetThuc} onChange={e=>setForm({...form,ngayKetThuc:e.target.value})}/></label>
        <label>Địa điểm<input maxLength={100} value={form.diaDiem||''} onChange={e=>setForm({...form,diaDiem:e.target.value})}/></label>
        <label>Số chỗ<input type="number" min="0" max="2147483647" step="1" value={form.soCho} onChange={e=>setForm({...form,soCho:e.target.value})}/><small>Để trống: theo tour ({defaultCapacity}).</small></label>
      </div><button>Lưu lịch</button>{editing&&<button type="button" onClick={()=>{setEditing('');setForm(empty());}}>Thêm lịch khác</button>}
    </fieldset></form>
    {roster&&<><h3>{stamp(roster.ngayKhoiHanh)} · {roster.soTaiKhoan} tài khoản · {roster.daDat}/{roster.sucChua} chỗ</h3>
      <div style={{overflowX:'auto'}}><table style={{width:'100%',textAlign:'left'}}><thead><tr><th>SĐT</th><th>Họ tên</th><th>Số chỗ</th><th>Mã vé</th><th>Trạng thái</th></tr></thead>
        <tbody>{roster.bookings.map(b=><tr key={b.maBooking} tabIndex={0} style={{cursor:'pointer'}}
          onClick={()=>!blocked&&setBookingId(b.maBooking)} onKeyDown={e=>{if(e.key==='Enter'&&!blocked)setBookingId(b.maBooking);}}>
          <td>{b.soDienThoai||'—'}</td><td>{b.hoTen||'Chưa có hồ sơ'}</td><td>{b.soCho} = {b.slnguoiLon} NL + {b.sltreEm} TE</td>
          <td><button disabled={blocked} onClick={()=>setBookingId(b.maBooking)}>{b.maBooking}</button></td><td>{status[b.trangThai]||b.trangThai}</td>
        </tr>)}</tbody></table></div>{!roster.bookings.length&&<p>Không có vé còn giữ chỗ.</p>}
    </>}
    {profile&&<aside className="panel"><h3>Hồ sơ khách — {profile.maBooking}</h3>
      {!profile.maKhachHang?<p>Vé/tài khoản chưa có hồ sơ.</p>:<>
        <p>{profile.ho} {profile.ten} · {profile.soDienThoai} · {profile.email||'Chưa có email'}</p>
        <p>Ngày sinh: {day(profile.ngaySinh)} · Quốc tịch: {profile.quocTich||'—'}</p><h4>Giấy tờ</h4>
        {!profile.giayTo.length&&<p>Chưa có giấy tờ.</p>}
        {profile.giayTo.map((g,i)=><div key={i}><b>{g.loaiGiayTo} · {g.soTrenGiayTo}</b><p>Cấp: {day(g.ngayCap)} · Hết hạn: {day(g.ngayHetHan)} · Nơi cấp: {g.noiCap}</p></div>)}
      </>}
    </aside>}
  </section>;
}
