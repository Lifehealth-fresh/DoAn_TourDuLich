import { useEffect, useState } from 'react';
import * as api from './api';
import { ExpandRecord, Notice } from './components';
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
  const [form, setForm] = useState({ soDienThoai: '', matKhau: '', tenVaiTro: 'Sale' });
  const [grants, setGrants] = useState(emptyGrant);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const canThem = can('TaiKhoan', 'Them');
  const canSua = can('TaiKhoan', 'Sua');

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
      setError(api.errorMessage(err, fallback));
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
    setForm({ soDienThoai: item.soDienThoai, matKhau: '', tenVaiTro: item.tenVaiTro });
    setGrants(mergeGrants(item.quyen));
  };

  const fresh = () => {
    setSelected('');
    setCreating(true);
    setForm({ soDienThoai: '', matKhau: '', tenVaiTro: 'Sale' });
    setGrants(emptyGrant());
    requestAnimationFrame(() => document.getElementById('account-create')?.scrollIntoView({ block: 'nearest', behavior: 'smooth' }));
  };

  const saveNew = (event) => {
    event.preventDefault();
    run(async () => {
      const response = await api.createAccount({
        soDienThoai: form.soDienThoai.trim(),
        matKhau: form.matKhau,
        tenVaiTro: form.tenVaiTro,
        quyen: grants,
      });
      setMessage(`Đã tạo tài khoản ${response.data.soDienThoai}.`);
      setCreating(false);
      setSelected(response.data.maUser);
      setForm({ soDienThoai: response.data.soDienThoai, matKhau: '', tenVaiTro: response.data.tenVaiTro });
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

  const editor = (isCreate) => (
    <form onSubmit={isCreate ? saveNew : saveGrants}>
      <fieldset disabled={busy} className="account-form">
        {isCreate ? (
          <>
            <label>Số điện thoại
              <input required value={form.soDienThoai} maxLength={20}
                onChange={(event) => setForm({ ...form, soDienThoai: event.target.value })} />
            </label>
            <label>Mật khẩu
              <input required type="password" value={form.matKhau}
                onChange={(event) => setForm({ ...form, matKhau: event.target.value })} />
            </label>
          </>
        ) : (
          <p className="muted">SĐT {form.soDienThoai} · mã {selected}</p>
        )}
        <label>Vai trò
          <select value={form.tenVaiTro} disabled={!isCreate && (!canSua || selected === selfId)}
            onChange={(event) => setForm({ ...form, tenVaiTro: event.target.value })}>
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
      {creating && <div id="account-create" className="create-slot record open"><div className="record-body">
        <h2>Tạo tài khoản nhân viên</h2>
        {editor(true)}
      </div></div>}
      <div className="table" aria-label="Danh sách tài khoản">
        {items.map((item) => (
          <ExpandRecord key={item.maUser} open={selected === item.maUser && !creating} summary={
            <div className="row" style={{ gridTemplateColumns: '1fr 1fr 2fr auto', cursor: busy ? 'wait' : 'pointer' }}
              onClick={() => { if (!busy) (selected === item.maUser ? setSelected('') : open(item)); }}>
              <b>{item.soDienThoai}</b>
              <span className="badge">{item.tenVaiTro}</span>
              <span className="muted">{summary(item.quyen)}</span>
              <button type="button" disabled={busy} onClick={(event) => { event.stopPropagation(); open(item); }}>Sửa quyền</button>
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
