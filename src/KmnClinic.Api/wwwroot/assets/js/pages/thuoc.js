/* Thuốc (M1): quản trị viên thêm/sửa; bác sĩ và thu ngân chỉ xem. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const laQuanTri = Auth.nguoiDung()?.vaiTro === 'QuanTriVien';
  const NGUONG = 20; // hiển thị; backend dùng PhongKham:NguongThuocSapHet
  let dsHienTai = [];
  const so = n => Number(n).toLocaleString('vi-VN');

  if (laQuanTri) $('btnThem').classList.remove('d-none');
  else document.querySelectorAll('.cot-thao-tac').forEach(th => th.classList.add('d-none'));
  $('nguong').textContent = `(tồn ≤ ${NGUONG})`;
  if (Ui.thamSo('sapHet')) $('locSapHet').checked = true;

  const bang = DanhSach.bangPhanTrang({
    url: 'thuoc', tbody: $('bang'), phanTrang: $('phanTrang'), soCot: laQuanTri ? 8 : 7, rong: 'Không có thuốc phù hợp.',
    thamSo: () => ({ q: $('oTim').value.trim(), sapHet: $('locSapHet').checked }),
    sauKhiVe: items => { dsHienTai = items; },
    dong: t => `<tr>
      <td class="mono">${Ui.esc(t.maThuoc)}</td>
      <td class="fw-semibold text-navy">${Ui.esc(t.tenThuoc)}${t.moTa ? `<br><small class="text-muted-kmn fw-normal">${Ui.esc(t.moTa)}</small>` : ''}</td>
      <td class="small">${Ui.esc(t.nsx)}</td>
      <td class="text-end">${so(t.hamLuong)}</td><td>${Ui.esc(t.donViTinh)}</td>
      <td class="text-end">${Ui.fmtTien(t.donGiaThuoc)}</td>
      <td class="text-end">${t.sapHet ? `<span class="badge-st st-huy text-danger"><i class="bi bi-exclamation-triangle"></i> ${so(t.soLuongTon)}</span>` : so(t.soLuongTon)}</td>
      ${laQuanTri ? `<td class="text-end"><button class="btn btn-sm btn-soft" data-sua="${Ui.esc(t.maThuoc)}"><i class="bi bi-pencil"></i> Sửa</button></td>` : ''}
      </tr>`
  });

  function moForm(t) {
    DanhSach.moForm({
      tieuDe: t ? `Sửa thuốc ${t.maThuoc}` : 'Thêm thuốc', rong: true,
      noiDung: `<div class="row g-3">
        <div class="col-md-6"><label class="form-label" for="tenThuoc">Tên thuốc</label><input id="tenThuoc" name="tenThuoc" class="form-control" maxlength="100" required></div>
        <div class="col-md-6"><label class="form-label" for="nsx">Nhà sản xuất</label><input id="nsx" name="nsx" class="form-control" maxlength="200" required></div>
        <div class="col-md-4"><label class="form-label" for="hamLuong">Hàm lượng</label><input id="hamLuong" name="hamLuong" type="number" step="0.01" min="0.01" class="form-control" required></div>
        <div class="col-md-4"><label class="form-label" for="donViTinh">Đơn vị tính</label><input id="donViTinh" name="donViTinh" class="form-control" maxlength="20" placeholder="Viên, chai, gói..." required></div>
        <div class="col-md-4"><label class="form-label" for="donGiaThuoc">Đơn giá (đ)</label><input id="donGiaThuoc" name="donGiaThuoc" type="number" min="1" step="1" class="form-control" required></div>
        <div class="col-md-4"><label class="form-label" for="soLuongTon">Số lượng tồn</label><input id="soLuongTon" name="soLuongTon" type="number" min="0" step="1" class="form-control" required></div>
        <div class="col-md-8"><label class="form-label" for="moTa">Mô tả <small class="text-muted-kmn">(không bắt buộc)</small></label><input id="moTa" name="moTa" class="form-control" maxlength="255"></div>
      </div>
      ${t ? `<div id="xacNhanTon" class="alert alert-warning small mt-3 mb-0 d-none"><div class="form-check mb-0">
        <input class="form-check-input" type="checkbox" id="dongYTon" name="dongYTon">
        <label class="form-check-label" for="dongYTon">Tôi xác nhận đổi tồn kho trực tiếp từ <b>${so(t.soLuongTon)}</b> thành <b id="tonMoi"></b> ${Ui.esc(t.donViTinh)}. Bình thường kho chỉ thay đổi khi thanh toán hóa đơn.</label></div></div>` : ''}
      ${t ? '<div class="form-text mt-2"><i class="bi bi-info-circle me-1"></i>Đổi đơn giá không ảnh hưởng đơn thuốc đã kê (đơn giá được lưu lúc kê). Kho tự trừ khi thu ngân xác nhận thanh toán.</div>' : ''}`,
      khiMo: f => {
        if (!t) return;
        ['tenThuoc', 'nsx', 'hamLuong', 'donViTinh', 'donGiaThuoc', 'soLuongTon'].forEach(k => { f[k].value = t[k]; });
        f.moTa.value = t.moTa || '';
        // Q8: đổi tồn kho trực tiếp phải tick xác nhận
        f.soLuongTon.addEventListener('input', () => {
          const doi = f.soLuongTon.value !== '' && +f.soLuongTon.value !== t.soLuongTon;
          f.querySelector('#xacNhanTon').classList.toggle('d-none', !doi);
          f.querySelector('#tonMoi').textContent = doi ? so(f.soLuongTon.value) : '';
          if (!doi) f.dongYTon.checked = false;
        });
      },
      thuThap: f => {
        const body = {
          tenThuoc: f.tenThuoc.value.trim(), nsx: f.nsx.value.trim(), hamLuong: f.hamLuong.value === '' ? null : +f.hamLuong.value,
          donViTinh: f.donViTinh.value.trim(), donGiaThuoc: f.donGiaThuoc.value === '' ? null : +f.donGiaThuoc.value,
          soLuongTon: f.soLuongTon.value === '' ? null : +f.soLuongTon.value, moTa: f.moTa.value.trim() || null
        };
        if (!body.tenThuoc) return 'Vui lòng nhập tên thuốc.';
        if (!body.nsx) return 'Vui lòng nhập nhà sản xuất.';
        if (!(body.hamLuong > 0)) return 'Hàm lượng phải lớn hơn 0.';
        if (!body.donViTinh) return 'Vui lòng nhập đơn vị tính.';
        if (!(body.donGiaThuoc > 0) || !Number.isInteger(body.donGiaThuoc)) return 'Đơn giá phải là số nguyên lớn hơn 0.';
        if (body.soLuongTon === null || body.soLuongTon < 0 || !Number.isInteger(body.soLuongTon)) return 'Số lượng tồn phải là số nguyên không âm.';
        if (t && body.soLuongTon !== t.soLuongTon && !f.dongYTon.checked) return 'Vui lòng tick xác nhận đổi tồn kho.';
        return body;
      },
      gui: async body => {
        await (t ? Api.put('thuoc/' + encodeURIComponent(t.maThuoc), body) : Api.post('thuoc', body));
        Ui.toast(t ? 'Đã cập nhật thuốc.' : 'Đã thêm thuốc.');
        bang.tai(t ? undefined : 1);
      }
    });
  }

  $('btnThem').addEventListener('click', () => moForm(null));
  $('oTim').addEventListener('input', Ui.treo(() => bang.tai(1)));
  $('locSapHet').addEventListener('change', () => bang.tai(1));
  $('bang').addEventListener('click', e => {
    const b = e.target.closest('[data-sua]');
    if (b) moForm(dsHienTai.find(t => t.maThuoc === b.dataset.sua));
  });
  bang.tai(1);
})();
