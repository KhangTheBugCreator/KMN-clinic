/* Hồ sơ của tôi (M7): lịch sử hồ sơ bác sĩ đã khám, lọc theo ngày, trạng thái; mở xem chi tiết ở trang khám (chỉ xem nếu đã hoàn tất). */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const { esc, thongTinKhach } = BacSi;

  const bang = DanhSach.bangPhanTrang({
    url: 'bac-si/ho-so', tbody: $('bang'), phanTrang: $('phanTrang'), soCot: 8, pageSize: 15,
    rong: 'Không có hồ sơ phù hợp.',
    thamSo: () => ({ q: $('oTim').value.trim(), tuNgay: $('tuNgay').value, denNgay: $('denNgay').value, trangThai: $('locTrangThai').value }),
    dong: h => `<tr>
      <td class="mono">${esc(h.maHoSo)}</td>
      <td class="text-nowrap">${Ui.fmtNgayGio(h.ngayGioKham)}</td>
      <td><b>${esc(h.tenKhach)}</b><br><small class="text-muted-kmn">${thongTinKhach(h)} · ${h.maLichHen ? 'Có hẹn' : 'Vãng lai'}</small></td>
      <td class="small">${h.chanDoanBenh ? esc(h.chanDoanBenh) : '<span class="text-muted-kmn">—</span>'}</td>
      <td class="text-center">${h.soThuoc}</td>
      <td>${Ui.badgeTrangThai(h.trangThai)}</td>
      <td class="text-end text-nowrap">${h.tongTien != null ? Ui.fmtTien(h.tongTien) : '<span class="text-muted-kmn">—</span>'}</td>
      <td class="text-end"><a class="btn btn-sm ${h.maHoaDon ? 'btn-soft' : 'btn-primary'}" href="kham-benh.html?ma=${encodeURIComponent(h.maHoSo)}">${h.maHoaDon ? 'Xem' : 'Khám'}</a></td></tr>`
  });

  $('oTim').addEventListener('input', Ui.treo(() => bang.tai(1)));
  ['tuNgay', 'denNgay', 'locTrangThai'].forEach(id => $(id).addEventListener('change', () => {
    if ($('tuNgay').value && $('denNgay').value && $('tuNgay').value > $('denNgay').value) return Ui.toast('Từ ngày phải trước hoặc bằng đến ngày.', 'warning');
    bang.tai(1);
  }));
  bang.tai(1);
})();
