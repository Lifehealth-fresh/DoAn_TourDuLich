import assert from 'node:assert/strict';
import {after, test} from 'node:test';
import React from 'react';
import {renderToStaticMarkup} from 'react-dom/server';
import {createServer} from 'vite';
import {bookingError, futureDepartures, departureTime} from '../src/departureAvailability.mjs';

// SSR component checks need JSX transformation, not browser prebundling or React Fast Refresh.
const server = await createServer({configFile:false, esbuild:{jsx:'automatic'},
  optimizeDeps:{noDiscovery:true,include:[]}, server:{middlewareMode:true}, appType:'custom'});
after(() => server.close());
const {default: DepartureBooking} = await server.ssrLoadModule('/src/DepartureBooking.jsx');
const {default: CurrentDesignSchedule} = await server.ssrLoadModule('/src/CurrentDesignSchedule.jsx');
const render = (component, props) => renderToStaticMarkup(React.createElement(component, props));
const future = {maKhoiHanh:'D1',ngayKhoiHanh:'2099-01-01T00:00:00',sucChua:10,daDat:6,conTrong:4};

test('No future departure: visible explanation and disabled booking button', () => {
  const html = render(DepartureBooking, {dates:[],price:100000});
  assert.match(html, /Chưa có lịch khởi hành/);
  assert.match(html, /<button disabled=""[^>]*>Đặt tour này<\/button>/);
});
test('Expired departures are filtered; timezone-less API dates retain UTC meaning', () => {
  assert.equal(departureTime('2099-01-01T00:00:00'), Date.parse('2099-01-01T00:00:00Z'));
  assert.deepEqual(futureDepartures([future,{...future,maKhoiHanh:'OLD',ngayKhoiHanh:'2000-01-01T00:00:00'}]), [future]);
});
test('Capacity, held and free counts are shown; full departures cannot be selected', () => {
  const html = render(DepartureBooking, {dates:[{...future,daDat:10,conTrong:0}],price:100000});
  assert.match(html, /Sức chứa 10/); assert.match(html,/Đã đặt 10/); assert.match(html,/Còn trống 0/);
  assert.match(html, /<option disabled="" value="D1">/);
  assert.match(html, /<button disabled=""[^>]*>Đặt tour này<\/button>/);
});
test('Adult + child total must fit the selected departure', () => {
  assert.equal(bookingError(future,3,1),'');
  assert.equal(bookingError(future,4,1),'Lịch khởi hành không đủ chỗ trống.');
  for(const [a,c] of [[0,0],[-1,1],[1.5,1],[2147483647,1]])
    assert.notEqual(bookingError(future,a,c),'');
});
test('Unapproved self-designed tour has no booking button', () => {
  const html = render(DepartureBooking, {dates:[future],price:100000,allowed:false});
  assert.doesNotMatch(html,/Đặt tour này/);
  assert.match(html,/chưa được duyệt/);
});
test('Customer reviews saved rows and price, not proposal cards', () => {
  const html = render(CurrentDesignSchedule, {schedule:{tenTour:'Tour đã lưu',trangThai:'ChoKhachXacNhan',giaTour:234567,
    lichTrinh:[{maLichTrinh:'LT1',ngayThu:2,thuTuTrongNgay:1,tenDiaDanh:'Điểm đã sửa',mota:'Nội dung lưu mới',soLuong:1,donGia:234567,thanhTien:234567}]}});
  assert.match(html,/Điểm đã sửa/);assert.match(html,/Nội dung lưu mới/);assert.match(html,/234.567/);
  assert.match(html,/Đồng ý lịch này/);assert.match(html,/Yêu cầu chỉnh lại/);
});
test('Customer response buttons exist only while awaiting customer confirmation', () => {
  for(const trangThai of ['DangThietKe','CanChinhSua','ChoDuyet','DaDuyet']) {
    const html = render(CurrentDesignSchedule,{schedule:{trangThai,giaTour:1,lichTrinh:[]}});
    assert.doesNotMatch(html, /<button[^>]*>Đồng ý lịch này/);
    assert.doesNotMatch(html, /<button[^>]*>Yêu cầu chỉnh lại/);
  }
});
