export const departureTime = value => value ? Date.parse(/(?:Z|[+-]\d\d:\d\d)$/i.test(value) ? value : value+'Z') : NaN;
export const futureDepartures = (dates,now=Date.now()) => dates.filter(d=>departureTime(d.ngayKhoiHanh)>now);
export const bookingError = (departure,adults,children,now=Date.now()) => {
  if (!departure || !(departureTime(departure.ngayKhoiHanh)>now)) return 'Chưa có lịch khởi hành phù hợp.';
  const a=Number(adults),c=Number(children);
  if(!Number.isInteger(a)||!Number.isInteger(c)||a<0||c<0||a+c<=0||a+c>2147483647)return 'Số hành khách phải là số nguyên hợp lệ, tổng lớn hơn 0.';
  if(!Number.isFinite(Number(departure.conTrong)) || a+c>Number(departure.conTrong))return 'Lịch khởi hành không đủ chỗ trống.';
  return '';
};
