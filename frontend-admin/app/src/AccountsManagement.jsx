import { useEffect, useState } from 'react';
import * as api from './api';
import { ExpandRecord, Notice, OpenPanel } from './components';
import { MODULES, useAuth } from './context';

const emptyGrant = () => MODULES.map((module) => ({
  chucNang: module.ma, tenChucNang: module.ten, them: false, sua: false, xoa: false, toanQuyen: false,
}));

const mergeGrants = (incoming) => {
  const byId = Object.fromEntries((incoming || []).map((item) => [item.chucNang, item]));
  return MODULES.map((module) => {
    const row = byId[module.ma] || {};
    const toanQuyen = Boolean(row.toanQuyen);
    return {
      chucNang: module.ma,
      tenChucNang: module.ten,
      them: toanQuyen || Boolean(row.them),
      sua: toanQuyen || Boolean(row.sua),
      xoa: toanQuyen || Boolean(row.xoa),
      toanQuyen,
    };
  });
};

const summary = (grants) => {
  const parts = mergeGrants(grants)
    .filter((row) => row.them || row.sua || row.xoa || row.toanQuyen)
    .map((row) => {
      if (row.toanQuyen) return `${row.tenChucNang}: toàn quyền`;
      const flags = [row.them && 'thêm', row.sua && 'sửa', row.xoa && 'xóa'].filter(Boolean);
      return `${row.tenChucNang}: ${flags.join(', ')}`;
    });
  return parts.length ? parts.join(' · ') : 'Chưa gán quyền';
};

function GrantGrid({ value, onChange, locked }) {
  const setFlag = (chucNang, key, checked) => {
    onChange(value.map((row) => {
      if (row.chucNang !== chucNang) return row;
      if (key === 'toanQuyen') {
        return { ...row, toanQuyen: checked, them: checked, sua: checked, xoa: checked };
      }
      const next = { ...row, [key]: checked };
      if (!checked) next.toanQuyen = false;
      if (next.them && next.sua && next.xoa) next.toanQuyen = true;
      return next;
    }));
  };

  return (
    <div className="perm-grid" role="table" aria-label="Phân quyền theo chức năng">
      <div className="perm-head" role="row">
        <span>Chức năng</span><span>Thêm</span><span>Sửa</span><span>Xóa</span><span>Toàn quyền</span>
      </div>
      {value.map((row) => (
        <label className="perm-row" role="row" key={row.chucNang}>
          <span>{row.tenChucNang}</span>
          {['them', 'sua', 'xoa', 'toanQuyen'].map((key) => (
            <input
              key={key}
              type="checkbox"
              aria-label={`${row.tenChucNang} ${key}`}
              checked={Boolean(row[key])}
              disabled={locked || (key !== 'toanQuyen' && row.toanQuyen)}
              onChange={(event) => setFlag(row.chucNang, key, event.target.checked)}
            />
          ))}
        </label>
      ))}
    </div>
  );
}

export default function AccountsManagement() {
  const { can, user } = useAuth();
  const selfId = user?.MaUser || user?.maUser;
  const [items, setItems] = useState([]);
  const [selected, setSelected] = useState('');
  const [creating, setCreating] = useState(false);
  const [form, setForm] = useState({ soDienThoai: '', matKhau: '', matKhauXacNhan: '', ho: '', ten: '', soCccd: '', chucVu: '', tenVaiTro: 'Sale' });
  const [grants, setGrants] = useState(emptyGrant);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const canThem = can('TaiKhoan', 'Them');
  const canSua = can('TaiKhoan', 'Sua');
  const canXoa = can('TaiKhoan', 'Xoa');

  const load = async () => {
    const response = await api.accounts();
    setItems(Array.isArray(response.data) ? response.data : []);
  };

  const run = async (action, fallback) => {
    if (busy) return;
    setBusy(true);
    setError('');
    setMessage('');
    try {
      await action();
    } catch (err) {
      setError(err?.response ? api.errorMessage(err, fallback) : (err.message || fallback));
    } finally {
      setBusy(false);
    }
  };

  useEffect(() => {
    run(load, 'Không tải được danh sách tài khoản.');
  }, []);

  const open = (item) => {
    setCreating(false);
    setSelected(item.maUser);
    setForm({
      soDienThoai: item.soDienThoai, matKhau: '', matKhauXacNhan: '',
      ho: item.ho || '', ten: item.ten || '', soCccd: item.soCccd || '',
      chucVu: item.chucVu || item.tenVaiTro, tenVaiTro: item.tenVaiTro,
    });
    setGrants(mergeGrants(item.quyen));
  };

  const fresh = () => {
    setSelected('');
    setCreating(true);
    setForm({ soDienThoai: '', matKhau: '', matKhauXacNhan: '', ho: '', ten: '', soCccd: '', chucVu: '', tenVaiTro: 'Sale' });
    setGrants(emptyGrant());
    requestAnimationFrame(() => document.getElementById('account-create')?.scrollIntoView({ block: 'nearest', behavior: 'smooth' }));
  };

  const saveNew = (event) => {
    event.preventDefault();
    run(async () => {
      const phone = form.soDienThoai.trim();
      if (!/^0\d{9}$/.test(phone)) {
        throw new Error('Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.');
      }
      if (!form.matKhau || form.matKhau.length < 8 || !/[A-Za-z]/.test(form.matKhau) || !/\d/.test(form.matKhau)) {
        throw new Error('Mật khẩu phải có ít nhất 8 ký tự, gồm cả chữ và số.');
      }
      if (form.matKhau !== form.matKhauXacNhan) {
        throw new Error('Xác nhận mật khẩu không khớp.');
      }
      if (!form.ho.trim() || !form.ten.trim()) {
        throw new Error('Họ và tên không được để trống.');
      }
      if (!/^\d{9}$|^\d{12}$/.test(form.soCccd.trim())) {
        throw new Error('Số CCCD phải gồm 9 hoặc 12 chữ số.');
      }
      const response = await api.createAccount({
        soDienThoai: phone,
        matKhau: form.matKhau,
        matKhauXacNhan: form.matKhauXacNhan,
        ho: form.ho.trim(),
        ten: form.ten.trim(),
        soCccd: form.soCccd.trim(),
        chucVu: form.chucVu.trim() || form.tenVaiTro,
        tenVaiTro: form.tenVaiTro,
        quyen: grants,
      });
      setMessage(`Đã tạo tài khoản ${response.data.soDienThoai}.`);
      setCreating(false);
      setSelected(response.data.maUser);
      setForm({
        soDienThoai: response.data.soDienThoai, matKhau: '', matKhauXacNhan: '',
        ho: response.data.ho || '', ten: response.data.ten || '', soCccd: response.data.soCccd || '',
        chucVu: response.data.chucVu || '', tenVaiTro: response.data.tenVaiTro,
      });
      setGrants(mergeGrants(response.data.quyen));
      await load();
    }, 'Không tạo được tài khoản.');
  };

  const saveGrants = (event) => {
    event.preventDefault();
    if (!selected) return;
    run(async () => {
      if (form.tenVaiTro) {
        const current = items.find((item) => item.maUser === selected);
        if (current && current.tenVaiTro !== form.tenVaiTro) {
          await api.changeAccountRole(selected, form.tenVaiTro);
        }
      }
      const response = await api.updateAccountGrants(selected, { quyen: grants });
      setMessage('Đã lưu phân quyền.');
      setGrants(mergeGrants(response.data.quyen));
      await load();
    }, 'Không lưu được phân quyền.');
  };

  const remove = (item) => {
    if (item.maUser === selfId) return;
    if (!window.confirm(`Xóa tài khoản ${item.soDienThoai}? Người này sẽ không đăng nhập được.`)) return;
    run(async () => {
      await api.deleteAccount(item.maUser);
      setMessage(`Đã xóa ${item.soDienThoai}.`);
      if (selected === item.maUser) { setSelected(''); setCreating(false); }
      await load();
    }, 'Không xóa được tài khoản.');
  };

  const editor = (isCreate) => (
    <form onSubmit={isCreate ? saveNew : saveGrants}>
      <fieldset disabled={busy} className="account-form">
        {isCreate ? (
          <>
            <div className="grid-2">
              <label>Họ
                <input required value={form.ho} onChange={(event) => setForm({ ...form, ho: event.target.value })} />
              </label>
              <label>Tên
                <input required value={form.ten} onChange={(event) => setForm({ ...form, ten: event.target.value })} />
              </label>
            </div>
            <label>Số điện thoại
              <input required value={form.soDienThoai} maxLength={10} placeholder="0xxxxxxxxx"
                onChange={(event) => setForm({ ...form, soDienThoai: event.target.value })} />
            </label>
            <label>Số CCCD
              <input required value={form.soCccd} maxLength={12} placeholder="9 hoặc 12 chữ số"
                onChange={(event) => setForm({ ...form, soCccd: event.target.value })} />
            </label>
            <label>Mật khẩu
              <input required type="password" minLength={8} placeholder="Tối thiểu 8 ký tự, gồm chữ và số"
                value={form.matKhau}
                onChange={(event) => setForm({ ...form, matKhau: event.target.value })} />
            </label>
            <label>Xác nhận mật khẩu
              <input required type="password" minLength={8} placeholder="Nhập lại mật khẩu"
                value={form.matKhauXacNhan}
                onChange={(event) => setForm({ ...form, matKhauXacNhan: event.target.value })} />
            </label>
          </>
        ) : (
          <p className="muted">{[form.ho, form.ten].filter(Boolean).join(' ') || 'Chưa có hồ sơ'} · SĐT {form.soDienThoai} · CCCD {form.soCccd || '—'} · mã {selected}</p>
        )}
        <label>Vai trò / chức vụ
          <select value={form.tenVaiTro} disabled={!isCreate && (!canSua || selected === selfId)}
            onChange={(event) => setForm({ ...form, tenVaiTro: event.target.value, chucVu: event.target.value })}>
            <option value="Sale">Sale (nhân viên admin)</option>
            <option value="Admin">Admin (toàn quyền)</option>
          </select>
        </label>
        <p className="muted">
          Không tích ô nào = tài khoản không dùng được chức năng đó. Ví dụ bỏ «Thêm» ở quản lý tour thì bấm thêm tour sẽ báo «Bạn bị hạn chế quyền».
          {form.tenVaiTro === 'Admin' ? ' Vai trò Admin luôn có toàn quyền, ô chọn chỉ lưu để hiển thị.' : ''}
        </p>
        <GrantGrid value={grants} onChange={setGrants} locked={!isCreate && !canSua} />
        {isCreate
          ? canThem && <button type="submit" disabled={busy}>Tạo tài khoản</button>
          : canSua && <button type="submit" disabled={busy}>Lưu phân quyền</button>}
      </fieldset>
    </form>
  );

  return (
    <div>
      <h1>Quản lý tài khoản</h1>
      <p className="muted">Danh sách tài khoản phía admin, tạo nhân viên mới và gán Thêm / Sửa / Xóa / Toàn quyền theo từng chức năng.</p>
      <Notice error={error} />
      {message && <div className="notice ok" role="status">{message}</div>}
      <div className="inline">
        {canThem && <button type="button" disabled={busy} onClick={fresh}>Thêm tài khoản</button>}
        <button type="button" disabled={busy} onClick={() => run(load, 'Không tải được danh sách tài khoản.')}>Tải lại</button>
      </div>
      {creating && <OpenPanel id="account-create" onClose={() => setCreating(false)}>
        <h2>Tạo tài khoản nhân viên</h2>
        {editor(true)}
      </OpenPanel>}
      <div className="table" aria-label="Danh sách tài khoản">
        {items.map((item) => (
          <ExpandRecord key={item.maUser} open={selected === item.maUser && !creating} onClose={() => setSelected('')} summary={
            <div className="row" style={{ gridTemplateColumns: '1.2fr 1fr auto 2fr auto auto', cursor: busy ? 'wait' : 'pointer' }}
              onClick={() => { if (!busy) (selected === item.maUser ? setSelected('') : open(item)); }}>
              <b>{[item.ho, item.ten].filter(Boolean).join(' ') || item.soDienThoai}</b>
              <span>{item.soDienThoai}</span>
              <span className="badge">{item.tenVaiTro}</span>
              <span className="muted">{summary(item.quyen)}</span>
              {canSua && <button type="button" disabled={busy} onClick={(event) => { event.stopPropagation(); open(item); }}>Sửa quyền</button>}
              {canXoa && item.maUser !== selfId && <button type="button" className="danger" disabled={busy} onClick={(event) => { event.stopPropagation(); remove(item); }}>Xóa</button>}
            </div>
          }>
            {editor(false)}
          </ExpandRecord>
        ))}
        {!items.length && !busy && <p className="muted">Chưa có tài khoản quản trị.</p>}
      </div>
    </div>
  );
}
