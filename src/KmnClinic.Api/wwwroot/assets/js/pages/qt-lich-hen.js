/* Lịch hẹn (M10, quản trị viên): xem lịch hẹn toàn phòng khám, chỉ đọc. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const esc = Ui.esc;

  $('locNgay').value = Ui.thamSo('ngay') || LeTan.homNay();
  if (Ui.thamSo('trangThai')) $('locTrangThai').value = Ui.thamSo('trangThai');
  if (Ui.thamSo('tatCaNgay')) $('tatCaNgay').checked = true;
  $('locNgay').disabled = $('tatCaNgay').checked;

  const dong = h => `<tr>
      <td class="mono">${esc(h.maLichHen)}</td>
      <td><b>${esc(h.tenKhach)}</b><br><small class="text-muted-kmn">${esc(h.sdtKhach)} · <span class="mono">${esc(h.maKh)}</span></small></td>
      <td>${esc(h.tenBacSi)}<br><small class="text-muted-kmn">${Ui.fmtNgay(h.ngayKham)} · ${esc(h.tenCa)} ${LeTan.gio(h)} · ${esc(h.tenPhong)} · ${esc(h.tenKhoa)}</small></td>
      <td class="small">${h.trieuChungBanDau ? esc(h.trieuChungBanDau) : '<span class="text-muted-kmn">—</span>'}</td>
      <td>${Ui.badgeTrangThai(h.trangThai)}${h.tenNvXacNhan ? `<br><small class="text-muted-kmn">LT: ${esc(h.tenNvXacNhan)}</small>` : ''}
        ${h.maHoSo ? `<br><a class="small" href="qt-ho-so.html?ma=${encodeURIComponent(h.maHoSo)}">Hồ sơ <span class="mono">${esc(h.maHoSo)}</span></a>` : ''}</td>
      <td class="small text-nowrap">${Ui.fmtNgayGio(h.ngayGioDat)}</td></tr>`;

  const bang = DanhSach.bangPhanTrang({
    url: 'quan-tri/lich-hen', tbody: $('bang'), phanTrang: $('phanTrang'), soCot: 6, pageSize: 15,
    rong: 'Không có lịch hẹn phù hợp.',
    thamSo: () => ({
      q: $('oTim').value.trim(), ngay: $('tatCaNgay').checked ? '' : $('locNgay').value,
      trangThai: $('locTrangThai').value, maNv: $('locBacSi').value
    }),
    dong
  });

  Api.get('lich-lam-viec/bac-si').then(bs => {
    $('locBacSi').insertAdjacentHTML('beforeend', bs.map(b => `<option value="${esc(b.maNv)}">${esc(b.hoTen)} · ${esc(b.tenKhoa)}</option>`).join(''));
  }).catch(() => { /* vẫn lọc được theo cách khác */ });

  $('oTim').addEventListener('input', Ui.treo(() => bang.tai(1)));
  ['locNgay', 'locTrangThai', 'locBacSi'].forEach(id => $(id).addEventListener('change', () => bang.tai(1)));
  $('tatCaNgay').addEventListener('change', () => { $('locNgay').disabled = $('tatCaNgay').checked; bang.tai(1); });
  bang.tai(1);
})();
