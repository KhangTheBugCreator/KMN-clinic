/* Quản lý nhân viên (M2) – quản trị viên. Không có trường mật khẩu khi sửa; ảnh chỉ là đường dẫn HTTPS. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const toi = Auth.nguoiDung()?.ma;
  let dsHienTai = [], dsKhoa = [];
  const MAU_CV = { 'Quản trị viên': 'st-huy', 'Lễ tân': 'st-xacnhan', 'Bác sĩ': 'st-kham', 'Thu ngân': 'st-chua-tt' };

  const bang = DanhSach.bangPhanTrang({
    url: 'nhan-vien', tbody: $('bang'), phanTrang: $('phanTrang'), soCot: 6, rong: 'Không có nhân viên phù hợp.',
    thamSo: () => ({ q: $('oTim').value.trim(), chucVu: $('locChucVu').value, maKhoa: $('locKhoa').value }),
    sauKhiVe: items => {
      dsHienTai = items;
      $('bang').querySelectorAll('img[data-avatar]').forEach(img => renderAvatar(img, img.dataset.avatar));
    },
    dong: n => {
      const khoaXoa = n.soLienKet > 0 ? 'Nhân viên đã có dữ liệu nghiệp vụ' : n.maNv === toi ? 'Đây là bạn' : '';
      return `<tr>
      <td><div class="d-flex align-items-center gap-2">
        <img class="avatar" alt="" data-avatar="${Ui.esc(n.duongDanAnh || '')}" referrerpolicy="no-referrer" style="width:40px;height:40px">
        <span><b class="text-navy">${Ui.esc(n.hoTen)}</b>${n.maNv === toi ? ' <small class="badge text-bg-light">Bạn</small>' : ''}<br><small class="text-muted-kmn mono">${Ui.esc(n.maNv)}</small></span></div></td>
      <td>${Ui.esc(n.soDienThoai)}</td>
      <td><span class="badge-st ${MAU_CV[n.chucVu] || ''}">${Ui.esc(n.chucVu)}</span></td>
      <td>${Ui.esc(n.tenKhoa || '—')}</td>
      <td>${n.ngaySinh ? Ui.fmtNgay(n.ngaySinh) : '<span class="text-muted-kmn">—</span>'}</td>
      <td class="text-end text-nowrap">
        <button class="btn btn-sm btn-soft" data-sua="${Ui.esc(n.maNv)}"><i class="bi bi-pencil"></i> Sửa</button>
        <button class="btn btn-sm btn-outline-danger" data-xoa="${Ui.esc(n.maNv)}" ${khoaXoa ? `disabled title="${khoaXoa}"` : ''} aria-label="Xóa"><i class="bi bi-trash"></i></button>
      </td></tr>`;
    }
  });

  async function taiKhoa() {
    dsKhoa = (await Api.get('khoa?pageSize=100')).items;
    $('locKhoa').insertAdjacentHTML('beforeend', dsKhoa.map(k => `<option value="${Ui.esc(k.maKhoa)}">${Ui.esc(k.tenKhoa)}</option>`).join(''));
  }

  function moForm(n) {
    const khoaCv = n && n.soLienKet > 0;
    DanhSach.moForm({
      tieuDe: n ? `Sửa nhân viên ${n.maNv}` : 'Thêm nhân viên', rong: true,
      noiDung: `<div class="row g-3">
        <div class="col-md-8"><div class="row g-3">
          <div class="col-12"><label class="form-label" for="hoTen">Họ tên</label><input id="hoTen" name="hoTen" class="form-control" maxlength="50" required></div>
          <div class="col-sm-6"><label class="form-label" for="soDienThoai">Số điện thoại</label><input id="soDienThoai" name="soDienThoai" class="form-control" inputmode="numeric" maxlength="10" placeholder="0xxxxxxxxx" required></div>
          <div class="col-sm-6"><label class="form-label" for="ngaySinh">Ngày sinh <small class="text-muted-kmn">(không bắt buộc)</small></label><input id="ngaySinh" name="ngaySinh" type="date" class="form-control"></div>
          <div class="col-sm-6"><label class="form-label" for="chucVu">Chức vụ</label>
            <select id="chucVu" name="chucVu" class="form-select" ${khoaCv ? 'disabled' : ''} required><option value="">Chọn chức vụ</option>
              <option>Quản trị viên</option><option>Lễ tân</option><option>Bác sĩ</option><option>Thu ngân</option></select></div>
          <div class="col-sm-6" id="oKhoa"><label class="form-label" for="maKhoa">Khoa</label>
            <select id="maKhoa" name="maKhoa" class="form-select" ${khoaCv ? 'disabled' : ''}><option value="">Chọn khoa</option>
              ${dsKhoa.map(k => `<option value="${Ui.esc(k.maKhoa)}">${Ui.esc(k.tenKhoa)}</option>`).join('')}</select></div>
          ${khoaCv ? `<div class="col-12 form-text mt-1"><i class="bi bi-lock me-1"></i>Nhân viên đã có ${n.soLienKet} dữ liệu lịch làm việc, lịch hẹn, hồ sơ hoặc hóa đơn nên không đổi chức vụ, khoa được.</div>` : ''}
          ${n ? '' : `<div class="col-sm-6"><label class="form-label" for="matKhau">Mật khẩu</label>
            <input id="matKhau" name="matKhau" type="password" class="form-control" minlength="8" maxlength="100" autocomplete="new-password" required>
            <div class="form-text">Tối thiểu 8 ký tự (RB38). Chỉ được băm và lưu vào database, website không dùng để đăng nhập.</div></div>`}
        </div></div>
        <div class="col-md-4 text-center">
          <img id="xemAnh" class="avatar mb-2" alt="Ảnh xem trước" referrerpolicy="no-referrer" style="width:120px;height:120px">
          <label class="form-label d-block text-start" for="duongDanAnh">Đường dẫn ảnh <small class="text-muted-kmn">(không bắt buộc)</small></label>
          <input id="duongDanAnh" name="duongDanAnh" class="form-control" maxlength="1000" placeholder="https://...">
          <div class="form-text text-start">Chỉ nhận https://. Ảnh lỗi hoặc bỏ trống sẽ hiện ảnh mặc định.</div>
        </div></div>`,
      khiMo: f => {
        const capNhatKhoa = () => {
          const bs = f.chucVu.value === 'Bác sĩ';
          $('oKhoa').classList.toggle('d-none', !bs);
          if (!bs) f.maKhoa.value = '';
        };
        const xemAnh = Ui.treo(() => renderAvatar($('xemAnh'), f.duongDanAnh.value.trim()), 400);
        f.chucVu.addEventListener('change', capNhatKhoa);
        f.duongDanAnh.addEventListener('input', xemAnh);
        if (n) {
          f.hoTen.value = n.hoTen; f.soDienThoai.value = n.soDienThoai; f.ngaySinh.value = n.ngaySinh || '';
          f.chucVu.value = n.chucVu; f.maKhoa.value = n.maKhoa || ''; f.duongDanAnh.value = n.duongDanAnh || '';
        }
        f.ngaySinh.max = new Date().toISOString().slice(0, 10);
        capNhatKhoa();
        renderAvatar($('xemAnh'), f.duongDanAnh.value.trim());
      },
      thuThap: f => {
        const body = {
          hoTen: f.hoTen.value.trim(), soDienThoai: f.soDienThoai.value.trim(), ngaySinh: f.ngaySinh.value || null,
          chucVu: f.chucVu.value, maKhoa: f.maKhoa.value || null, duongDanAnh: f.duongDanAnh.value.trim() || null
        };
        if (!body.hoTen) return 'Vui lòng nhập họ tên.';
        if (!/^0\d{9}$/.test(body.soDienThoai)) return 'Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.';
        if (!body.chucVu) return 'Vui lòng chọn chức vụ.';
        if (body.chucVu === 'Bác sĩ' && !body.maKhoa) return 'Bác sĩ phải thuộc một khoa (RB6).';
        if (body.duongDanAnh && !/^https:\/\//i.test(body.duongDanAnh)) return 'Đường dẫn ảnh phải bắt đầu bằng https://';
        if (!n) {
          body.matKhau = f.matKhau.value;
          if (body.matKhau.length < 8) return 'Mật khẩu tối thiểu 8 ký tự.';
        }
        return body;
      },
      gui: async body => {
        await (n ? Api.put('nhan-vien/' + encodeURIComponent(n.maNv), body) : Api.post('nhan-vien', body));
        Ui.toast(n ? 'Đã cập nhật nhân viên.' : 'Đã thêm nhân viên.');
        bang.tai(n ? undefined : 1);
      }
    });
  }

  $('btnThem').addEventListener('click', () => moForm(null));
  $('oTim').addEventListener('input', Ui.treo(() => bang.tai(1)));
  $('locChucVu').addEventListener('change', () => bang.tai(1));
  $('locKhoa').addEventListener('change', () => bang.tai(1));
  $('bang').addEventListener('click', e => {
    const sua = e.target.closest('[data-sua]'), xoa = e.target.closest('[data-xoa]');
    if (sua) moForm(dsHienTai.find(n => n.maNv === sua.dataset.sua));
    if (xoa) {
      const n = dsHienTai.find(x => x.maNv === xoa.dataset.xoa);
      DanhSach.xoa({ tieuDe: `Xóa nhân viên ${n.hoTen}?`, noiDung: 'Thao tác này không hoàn tác được.',
        url: 'nhan-vien/' + encodeURIComponent(n.maNv), sauKhiXoa: () => bang.tai() });
    }
  });

  taiKhoa().catch(Ui.loi).finally(() => bang.tai(1));
})();
