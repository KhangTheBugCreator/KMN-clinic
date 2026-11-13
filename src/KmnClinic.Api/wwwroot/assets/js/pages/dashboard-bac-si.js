/* Dashboard bác sĩ (M7): số liệu, hàng chờ, lịch trực 7 ngày tới, hồ sơ hoàn tất hôm nay. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const { esc, homNay, mucHangCho } = BacSi;
  const nd = Auth.nguoiDung();
  const gio = l => `${Ui.fmtGio(l.gioBatDau)}–${Ui.fmtGio(l.gioKetThuc)}`;

  const h = new Date().getHours();
  $('loiChao').textContent = `${h < 11 ? 'Chào buổi sáng' : h < 18 ? 'Chào buổi chiều' : 'Chào buổi tối'}, ${nd.hoTen}`;
  $('homNay').textContent = new Date().toLocaleDateString('vi-VN', { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' });

  async function taiTongQuan() {
    try {
      const t = await Api.get('bac-si/tong-quan');
      $('tqCho').textContent = t.choKham;
      $('tqDang').textContent = t.dangKham;
      $('tqXong').textContent = t.hoanTatHomNay;
      $('tqTong').textContent = `Trên ${t.tongHoSoHomNay} hồ sơ tiếp nhận hôm nay`;
      if (t.caHienTai) {
        $('tqCa').textContent = t.caHienTai.tenCa;
        $('tqCaPhu').textContent = `${t.caHienTai.tenPhong} · ${gio(t.caHienTai)} · ${t.caHienTai.soLichHen} lịch hẹn`;
      } else {
        $('tqCa').textContent = '—';
        $('tqCaPhu').textContent = 'Không có ca đang diễn ra';
      }
      const tenNgay = d => d === homNay() ? 'Hôm nay' : new Date(d + 'T00:00').toLocaleDateString('vi-VN', { weekday: 'long', day: '2-digit', month: '2-digit' });
      $('lichTruc').innerHTML = t.lichSapToi.length ? `<ul class="timeline">${t.lichSapToi.map(l => `<li>
          <div class="t">${esc(tenNgay(l.ngay))} · ${esc(l.tenCa)}</div>
          <small class="text-muted-kmn">${gio(l)} · ${esc(l.tenPhong)} · ${l.soLichHen} lịch hẹn</small></li>`).join('')}</ul>`
        : '<div class="empty-state py-3"><i class="bi bi-calendar-x"></i>Chưa có lịch trực trong 7 ngày tới.</div>';
    } catch (e) { Ui.loi(e); }
  }

  async function taiHangCho() {
    try {
      const ds = await Api.get('bac-si/hang-cho');
      $('hangCho').innerHTML = ds.length ? ds.map((x, i) => mucHangCho(x, i, { current: i === 0, link: true })).join('')
        : '<div class="empty-state"><i class="bi bi-cup-hot"></i>Chưa có bệnh nhân nào chờ khám.</div>';
    } catch (e) { $('hangCho').innerHTML = `<div class="small text-danger">${esc(e.message)}</div>`; }
  }

  async function taiDaXong() {
    try {
      const kq = await Api.get(`bac-si/ho-so?tuNgay=${homNay()}&denNgay=${homNay()}&pageSize=8`);
      const xong = kq.items.filter(x => x.maHoaDon);
      $('daXong').innerHTML = xong.length ? xong.map(x => `<tr>
          <td class="mono"><a href="kham-benh.html?ma=${encodeURIComponent(x.maHoSo)}">${esc(x.maHoSo)}</a></td><td>${esc(x.tenKhach)}</td>
          <td class="small">${esc(x.chanDoanBenh || '')}</td><td>${Ui.badgeTrangThai(x.trangThai)}</td></tr>`).join('')
        : Ui.dongTrong(4, 'Chưa hoàn tất hồ sơ nào hôm nay.');
    } catch (e) { $('daXong').innerHTML = Ui.dongTrong(4, e.message, 'exclamation-circle'); }
  }

  taiTongQuan();
  taiHangCho();
  taiDaXong();
})();
