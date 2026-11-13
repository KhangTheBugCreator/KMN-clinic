/* Hóa đơn chờ thu và Lịch sử thanh toán (M8): danh sách theo trạng thái ở data-trang-thai của trang, chi tiết bên phải. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const { esc, homNay } = ThuNgan;
  const trangThai = document.body.dataset.trangThai, choThu = trangThai === 'Chưa thanh toán';
  let maChon = null;

  const chiTiet = ThuNgan.khungChiTiet($('chiTiet'), {
    rong: choThu ? 'Chọn một hóa đơn để xem và thu tiền.' : 'Chọn một hóa đơn để xem lại, in hoặc xuất PDF.',
    sauThanhToan: () => bang.tai()
  });

  const dong = h => `<tr class="${h.maHoaDon === maChon ? 'table-active' : ''}" data-chon="${esc(h.maHoaDon)}" role="button">
      <td class="mono">${esc(h.maHoaDon)}</td>
      <td><b class="text-navy">${esc(h.tenKhach)}</b><br><small class="text-muted-kmn">${esc(h.sdtKhach)} · <span class="mono">${esc(h.maHoSo)}</span> · ${
        choThu ? 'khám ' + Ui.fmtNgayGio(h.ngayGioKham) : 'thu ' + Ui.fmtNgayGio(h.ngayGioThanhToan) + ' · ' + esc(h.tenNvThu || '')}</small></td>
      <td>${esc(h.tenBacSi)}<br><small class="text-muted-kmn">${esc(h.tenKhoa)}</small></td>
      <td class="text-end fw-semibold text-nowrap">${Ui.fmtTien(h.tongTien)}</td>
      <td class="text-end"><button class="btn btn-sm ${choThu ? 'btn-success' : 'btn-soft'}" data-chon="${esc(h.maHoaDon)}">${choThu ? 'Thu tiền' : 'Xem'}</button></td></tr>`;

  const bang = DanhSach.bangPhanTrang({
    url: 'thu-ngan/hoa-don', tbody: $('bang'), phanTrang: $('phanTrang'), soCot: 5, pageSize: 12,
    rong: choThu ? 'Không có hóa đơn nào chờ thu.' : 'Không có hóa đơn phù hợp.',
    thamSo: () => ({ trangThai, q: $('oTim').value.trim(),
      tuNgay: $('tuNgay')?.value || '', denNgay: $('denNgay')?.value || '', cuaToi: $('cuaToi')?.checked || false }),
    dong,
    sauKhiVe: items => { if (choThu && !maChon && items.length) chon(items[0].maHoaDon); } // chờ thu: tự mở hóa đơn đầu tiên
  });

  function chon(ma) {
    maChon = ma;
    $('bang').querySelectorAll('tr[data-chon]').forEach(tr => tr.classList.toggle('table-active', tr.dataset.chon === ma));
    chiTiet.mo(ma);
  }

  $('bang').addEventListener('click', e => {
    const t = e.target.closest('[data-chon]');
    if (!t) return;
    chon(t.dataset.chon);
    if (window.innerWidth < 1200) $('chiTiet').scrollIntoView({ behavior: 'smooth', block: 'start' }); // màn hình hẹp: chi tiết nằm dưới danh sách
  });
  $('oTim').addEventListener('input', Ui.treo(() => bang.tai(1)));
  if (!choThu) {
    if (Ui.thamSo('homNay')) { $('tuNgay').value = homNay(); $('denNgay').value = homNay(); }
    if (Ui.thamSo('cuaToi')) $('cuaToi').checked = true;
    ['tuNgay', 'denNgay', 'cuaToi'].forEach(id => $(id).addEventListener('change', () => {
      if ($('tuNgay').value && $('denNgay').value && $('tuNgay').value > $('denNgay').value) return Ui.toast('Từ ngày phải trước hoặc bằng đến ngày.', 'warning');
      bang.tai(1);
    }));
  }
  if (Ui.thamSo('ma')) chon(Ui.thamSo('ma'));
  bang.tai(1);
})();
