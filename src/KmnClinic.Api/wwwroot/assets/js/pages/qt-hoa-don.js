/* Hóa đơn (M10, quản trị viên): mọi hóa đơn, chi tiết bên phải, in và xuất PDF. Không có nút thu tiền. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const esc = Ui.esc;
  let maChon = null;

  const chiTiet = ThuNgan.khungChiTiet($('chiTiet'), { api: 'quan-tri', chiXem: true, rong: 'Chọn một hóa đơn để xem, in hoặc xuất PDF.' });

  const dong = h => `<tr class="${h.maHoaDon === maChon ? 'table-active' : ''}" data-chon="${esc(h.maHoaDon)}" role="button">
      <td class="mono">${esc(h.maHoaDon)}</td>
      <td><b class="text-navy">${esc(h.tenKhach)}</b><br><small class="text-muted-kmn">${esc(h.sdtKhach)} · <span class="mono">${esc(h.maHoSo)}</span></small></td>
      <td>${esc(h.tenBacSi)}<br><small class="text-muted-kmn">${esc(h.tenKhoa)}</small></td>
      <td>${Ui.badgeTrangThai(h.trangThai)}<br><small class="text-muted-kmn">${h.ngayGioThanhToan
        ? Ui.fmtNgayGio(h.ngayGioThanhToan) + ' · ' + esc(h.tenNvThu || '') : 'khám ' + Ui.fmtNgayGio(h.ngayGioKham)}</small></td>
      <td class="text-end fw-semibold text-nowrap">${Ui.fmtTien(h.tongTien)}</td></tr>`;

  const bang = DanhSach.bangPhanTrang({
    url: 'quan-tri/hoa-don', tbody: $('bang'), phanTrang: $('phanTrang'), soCot: 5, pageSize: 12,
    rong: 'Không có hóa đơn phù hợp.',
    thamSo: () => ({ q: $('oTim').value.trim(), trangThai: $('locTrangThai').value, tuNgay: $('tuNgay').value, denNgay: $('denNgay').value }),
    dong
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
    if (window.innerWidth < 1200) $('chiTiet').scrollIntoView({ behavior: 'smooth', block: 'start' });
  });
  $('oTim').addEventListener('input', Ui.treo(() => bang.tai(1)));
  if (Ui.thamSo('trangThai')) $('locTrangThai').value = Ui.thamSo('trangThai');
  ['locTrangThai', 'tuNgay', 'denNgay'].forEach(id => $(id).addEventListener('change', () => {
    if ($('tuNgay').value && $('denNgay').value && $('tuNgay').value > $('denNgay').value) return Ui.toast('Từ ngày phải trước hoặc bằng đến ngày.', 'warning');
    bang.tai(1);
  }));
  if (Ui.thamSo('ma')) chon(Ui.thamSo('ma'));
  bang.tai(1);
})();
