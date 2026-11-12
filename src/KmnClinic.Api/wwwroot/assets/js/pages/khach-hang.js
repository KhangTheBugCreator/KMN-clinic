/* Khách hàng (M2): quản trị viên và lễ tân thêm, sửa; chỉ quản trị viên xóa. Không có trường mật khẩu. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const laQuanTri = Auth.nguoiDung()?.vaiTro === 'QuanTriVien';
  let dsHienTai = [];
  if (!laQuanTri) document.querySelector('.breadcrumb-item a').textContent = 'Tiếp đón';

  const bang = DanhSach.bangPhanTrang({
    url: 'khach-hang', tbody: $('bang'), phanTrang: $('phanTrang'), soCot: 8, rong: 'Không có khách hàng phù hợp.',
    thamSo: () => ({ q: $('oTim').value.trim(), gioiTinh: $('locGioiTinh').value }),
    sauKhiVe: items => { dsHienTai = items; },
    dong: k => {
      const dangDung = k.soLichHen > 0 || k.soHoSo > 0;
      return `<tr>
      <td class="mono">${Ui.esc(k.maKh)}</td>
      <td><b class="text-navy">${Ui.esc(k.hoTen)}</b><br><small class="text-muted-kmn">${Ui.esc(k.gioiTinh)}</small></td>
      <td>${Ui.esc(k.soDienThoai)}</td>
      <td>${Ui.fmtNgay(k.ngaySinh)}</td>
      <td class="small">${Ui.esc(k.diaChi || '—')}</td>
      <td class="text-center">${k.soLichHen}</td><td class="text-center">${k.soHoSo}</td>
      <td class="text-end text-nowrap">
        <button class="btn btn-sm btn-soft" data-sua="${Ui.esc(k.maKh)}"><i class="bi bi-pencil"></i> Sửa</button>
        ${laQuanTri ? `<button class="btn btn-sm btn-outline-danger" data-xoa="${Ui.esc(k.maKh)}" ${dangDung ? 'disabled title="Khách đã có lịch hẹn hoặc hồ sơ khám"' : ''} aria-label="Xóa"><i class="bi bi-trash"></i></button>` : ''}
      </td></tr>`;
    }
  });

  function moForm(k) {
    DanhSach.moForm({
      tieuDe: k ? `Sửa khách hàng ${k.maKh}` : 'Thêm khách hàng',
      noiDung: `<div class="row g-3">
        <div class="col-12"><label class="form-label" for="hoTen">Họ tên</label><input id="hoTen" name="hoTen" class="form-control" maxlength="50" required></div>
        <div class="col-sm-6"><label class="form-label" for="soDienThoai">Số điện thoại</label><input id="soDienThoai" name="soDienThoai" class="form-control" inputmode="numeric" maxlength="10" placeholder="0xxxxxxxxx" required></div>
        <div class="col-sm-6"><label class="form-label" for="ngaySinh">Ngày sinh</label><input id="ngaySinh" name="ngaySinh" type="date" class="form-control" required></div>
        <div class="col-12"><span class="form-label d-block">Giới tính</span>
          <div class="form-check form-check-inline"><input class="form-check-input" type="radio" name="gioiTinh" id="gtNam" value="Nam"><label class="form-check-label" for="gtNam">Nam</label></div>
          <div class="form-check form-check-inline"><input class="form-check-input" type="radio" name="gioiTinh" id="gtNu" value="Nữ"><label class="form-check-label" for="gtNu">Nữ</label></div></div>
        <div class="col-12"><label class="form-label" for="diaChi">Địa chỉ <small class="text-muted-kmn">(không bắt buộc)</small></label><input id="diaChi" name="diaChi" class="form-control" maxlength="100"></div>
      </div>`,
      khiMo: f => {
        f.ngaySinh.max = new Date().toISOString().slice(0, 10);
        if (!k) return;
        f.hoTen.value = k.hoTen; f.soDienThoai.value = k.soDienThoai; f.ngaySinh.value = k.ngaySinh;
        f.gioiTinh.value = k.gioiTinh; f.diaChi.value = k.diaChi || '';
      },
      thuThap: f => {
        const body = { hoTen: f.hoTen.value.trim(), soDienThoai: f.soDienThoai.value.trim(), ngaySinh: f.ngaySinh.value || null,
          gioiTinh: f.gioiTinh.value, diaChi: f.diaChi.value.trim() || null };
        if (!body.hoTen) return 'Vui lòng nhập họ tên.';
        if (!/^0\d{9}$/.test(body.soDienThoai)) return 'Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.';
        if (!body.ngaySinh) return 'Vui lòng nhập ngày sinh.';
        if (!body.gioiTinh) return 'Vui lòng chọn giới tính.';
        return body;
      },
      gui: async body => {
        await (k ? Api.put('khach-hang/' + encodeURIComponent(k.maKh), body) : Api.post('khach-hang', body));
        Ui.toast(k ? 'Đã cập nhật khách hàng.' : 'Đã thêm khách hàng.');
        bang.tai(k ? undefined : 1);
      }
    });
  }

  $('btnThem').addEventListener('click', () => moForm(null));
  $('oTim').addEventListener('input', Ui.treo(() => bang.tai(1)));
  $('locGioiTinh').addEventListener('change', () => bang.tai(1));
  $('bang').addEventListener('click', e => {
    const sua = e.target.closest('[data-sua]'), xoa = e.target.closest('[data-xoa]');
    if (sua) moForm(dsHienTai.find(k => k.maKh === sua.dataset.sua));
    if (xoa) {
      const k = dsHienTai.find(x => x.maKh === xoa.dataset.xoa);
      DanhSach.xoa({ tieuDe: `Xóa khách hàng ${k.hoTen}?`, noiDung: 'Thao tác này không hoàn tác được.',
        url: 'khach-hang/' + encodeURIComponent(k.maKh), sauKhiXoa: () => bang.tai() });
    }
  });
  bang.tai(1);
})();
