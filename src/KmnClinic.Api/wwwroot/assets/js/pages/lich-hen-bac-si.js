/* Lịch hẹn của tôi (M10, bác sĩ): khách đã hẹn vào các ca của bác sĩ, chỉ xem. Backend tự lọc theo bác sĩ đăng nhập. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const esc = Ui.esc;

  $('locNgay').value = Ui.thamSo('ngay') || LeTan.homNay();
  if (Ui.thamSo('tatCaNgay')) $('tatCaNgay').checked = true;
  $('locNgay').disabled = $('tatCaNgay').checked;

  const dong = h => `<tr>
      <td class="mono">${esc(h.maLichHen)}</td>
      <td><b>${esc(h.tenKhach)}</b><br><small class="text-muted-kmn">${esc(h.gioiTinh)} · ${BacSi.tuoi(h.ngaySinh)} tuổi</small></td>
      <td>${Ui.fmtNgay(h.ngayKham)}<br><small class="text-muted-kmn">${esc(h.tenCa)} ${LeTan.gio(h)} · ${esc(h.tenPhong)}</small></td>
      <td class="small">${h.trieuChungBanDau ? esc(h.trieuChungBanDau) : '<span class="text-muted-kmn">—</span>'}</td>
      <td>${Ui.badgeTrangThai(h.trangThai)}${h.maHoSo ? `<br><a class="small" href="kham-benh.html?ma=${encodeURIComponent(h.maHoSo)}">Hồ sơ <span class="mono">${esc(h.maHoSo)}</span></a>` : ''}</td></tr>`;

  const bang = DanhSach.bangPhanTrang({
    url: 'bac-si/lich-hen', tbody: $('bang'), phanTrang: $('phanTrang'), soCot: 5, pageSize: 15,
    rong: 'Chưa có khách nào hẹn vào ca của bạn trong ngày này.',
    thamSo: () => ({ q: $('oTim').value.trim(), ngay: $('tatCaNgay').checked ? '' : $('locNgay').value, trangThai: $('locTrangThai').value }),
    dong
  });

  $('oTim').addEventListener('input', Ui.treo(() => bang.tai(1)));
  ['locNgay', 'locTrangThai'].forEach(id => $(id).addEventListener('change', () => bang.tai(1)));
  $('tatCaNgay').addEventListener('change', () => { $('locNgay').disabled = $('tatCaNgay').checked; bang.tai(1); });
  bang.tai(1);
})();
