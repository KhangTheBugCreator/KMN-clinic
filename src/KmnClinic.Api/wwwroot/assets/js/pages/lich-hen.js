/* Lịch hẹn (M6, lễ tân): lọc theo ngày, trạng thái, bác sĩ; xác nhận, hủy, tiếp nhận. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  let ds = [];

  $('locNgay').value = Ui.thamSo('ngay') || LeTan.homNay();
  if (Ui.thamSo('trangThai')) $('locTrangThai').value = Ui.thamSo('trangThai');
  if (Ui.thamSo('tatCaNgay')) $('tatCaNgay').checked = true;
  $('locNgay').disabled = $('tatCaNgay').checked;

  const bang = DanhSach.bangPhanTrang({
    url: 'le-tan/lich-hen', tbody: $('bang'), phanTrang: $('phanTrang'), soCot: 6, pageSize: 15,
    rong: 'Không có lịch hẹn phù hợp.',
    thamSo: () => ({
      q: $('oTim').value.trim(), ngay: $('tatCaNgay').checked ? '' : $('locNgay').value,
      trangThai: $('locTrangThai').value, maNv: $('locBacSi').value
    }),
    dong: h => LeTan.dongLichHen(h, $('tatCaNgay').checked),
    sauKhiVe: items => { ds = items; }
  });
  LeTan.ganThaoTac($('bang'), () => ds, () => bang.tai());

  Api.get('lich-lam-viec/bac-si').then(bs => {
    $('locBacSi').insertAdjacentHTML('beforeend', bs.map(b => `<option value="${Ui.esc(b.maNv)}">${Ui.esc(b.hoTen)} · ${Ui.esc(b.tenKhoa)}</option>`).join(''));
  }).catch(() => { /* vẫn lọc được theo cách khác */ });

  $('oTim').addEventListener('input', Ui.treo(() => bang.tai(1)));
  ['locNgay', 'locTrangThai', 'locBacSi'].forEach(id => $(id).addEventListener('change', () => bang.tai(1)));
  $('tatCaNgay').addEventListener('change', () => { $('locNgay').disabled = $('tatCaNgay').checked; bang.tai(1); });
  bang.tai(1);
})();
