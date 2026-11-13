/* Dashboard lễ tân (M6): số liệu hôm nay, lịch hẹn hôm nay, bác sĩ đang trực, tiếp nhận gần đây. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const esc = Ui.esc;
  const nd = Auth.nguoiDung();
  let ds = [];

  const gioHienTai = new Date().getHours();
  $('loiChao').textContent = `${gioHienTai < 11 ? 'Chào buổi sáng' : gioHienTai < 18 ? 'Chào buổi chiều' : 'Chào buổi tối'}, ${nd.hoTen}`;
  $('homNay').textContent = new Date().toLocaleDateString('vi-VN', { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' });

  async function taiTongQuan() {
    try {
      const t = await Api.get('le-tan/tong-quan');
      $('tqChoXacNhan').textContent = t.choXacNhan;
      $('tqHomNay').textContent = t.lichHenHomNay;
      $('tqDaXacNhan').textContent = `${t.daXacNhanHomNay} đã xác nhận, chờ tiếp nhận`;
      $('tqTiepNhan').textContent = t.daTiepNhanHomNay;
      $('tqVangLai').textContent = `${t.vangLaiHomNay} khách vãng lai`;
      $('tqBacSi').textContent = t.bacSiDangTruc;
    } catch (e) { Ui.loi(e); }
  }

  async function taiBacSi() {
    try {
      const bs = await Api.get('le-tan/bac-si-dang-truc');
      $('dsBacSi').innerHTML = bs.length ? bs.map(LeTan.theBacSiTruc).join('')
        : '<div class="empty-state py-3"><i class="bi bi-moon"></i>Hiện không có bác sĩ nào đang trực.</div>';
      LeTan.veAnh($('dsBacSi'));
    } catch (e) { $('dsBacSi').innerHTML = `<div class="small text-danger">${esc(e.message)}</div>`; }
  }

  async function taiHoSo() {
    try {
      const hs = (await Api.get('le-tan/ho-so')).slice(0, 5);
      $('dsHoSo').innerHTML = hs.length ? hs.map(h => `<div class="queue-item">
          <div class="flex-grow-1"><b class="text-navy">${esc(h.tenKhach)}</b> <span class="mono small">${esc(h.maHoSo)}</span><br>
          <small class="text-muted-kmn">${Ui.fmtNgayGio(h.ngayGioKham).slice(-5)} · ${esc(h.tenBacSi)} · ${h.maLichHen ? 'Có hẹn' : 'Vãng lai'}</small></div>
          <span class="badge-st ${h.choKham ? 'st-xacnhan' : 'st-kham'}">${h.choKham ? 'Chờ khám' : 'Xong'}</span></div>`).join('')
        : '<div class="empty-state py-3"><i class="bi bi-journal"></i>Chưa tiếp nhận khách nào hôm nay.</div>';
    } catch (e) { $('dsHoSo').innerHTML = `<div class="small text-danger">${esc(e.message)}</div>`; }
  }

  const bang = DanhSach.bangPhanTrang({
    url: 'le-tan/lich-hen', tbody: $('bang'), phanTrang: $('phanTrang'), soCot: 5, pageSize: 8,
    rong: 'Hôm nay chưa có lịch hẹn.',
    thamSo: () => ({ ngay: LeTan.homNay(), trangThai: $('locTrangThai').value }),
    dong: h => LeTan.dongLichHen(h, false, true),
    sauKhiVe: items => { ds = items; }
  });
  const taiTatCa = () => { bang.tai(); taiTongQuan(); taiBacSi(); taiHoSo(); };
  LeTan.ganThaoTac($('bang'), () => ds, taiTatCa);
  $('locTrangThai').addEventListener('change', () => bang.tai(1));

  bang.tai(1);
  taiTongQuan();
  taiBacSi();
  taiHoSo();
})();
