/* =========================================================
   bac-si.js – hàm dùng chung cho các trang bác sĩ (M7).
   Nạp sau api.js, ui.js, layout.js.
   ========================================================= */
(function () {
  'use strict';
  const esc = Ui.esc;
  const pad = n => String(n).padStart(2, '0');
  const homNay = () => { const d = new Date(); return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`; };
  const gioPhut = v => { const d = new Date(v); return `${d.getHours()}:${pad(d.getMinutes())}`; };

  /** Tuổi tính theo ngày sinh (DateOnly "yyyy-MM-dd"). */
  function tuoi(ngaySinh) {
    const [y, m, d] = ngaySinh.split('-').map(Number), nay = new Date();
    let t = nay.getFullYear() - y;
    if (nay.getMonth() + 1 < m || (nay.getMonth() + 1 === m && nay.getDate() < d)) t--;
    return t;
  }

  const thongTinKhach = h => `${esc(h.gioiTinh)} · ${tuoi(h.ngaySinh)} tuổi`;
  const nguon = h => h.maLichHen ? `Có hẹn ${esc(h.maLichHen)}` : 'Khách vãng lai';
  const tenThuoc = t => `${esc(t.tenThuoc)} ${Number(t.hamLuong).toLocaleString('vi-VN')}`;

  /** Một mục hàng chờ. current: đang mở; link: dẫn tới trang khám (dashboard) hay chọn tại chỗ (trang khám). */
  function mucHangCho(h, i, { current = false, link = false } = {}) {
    const cu = h.ngayGioKham.slice(0, 10) !== homNay();
    const nut = link
      ? `<a href="kham-benh.html?ma=${encodeURIComponent(h.maHoSo)}" class="btn ${i === 0 ? 'btn-primary' : 'btn-soft'} btn-sm">Khám</a>`
      : '';
    return `<div class="queue-item ${current ? 'current' : ''}" ${link ? '' : `data-chon-ho-so="${esc(h.maHoSo)}" role="button" tabindex="0"`}>
      <span class="num">${i + 1}</span>
      <div class="flex-grow-1 min-w-0"><b class="text-navy">${esc(h.tenKhach)}</b> <small class="text-muted-kmn">· ${thongTinKhach(h)}</small>
        ${Ui.badgeTrangThai(h.trangThai)}<br>
        <small class="text-muted-kmn"><i class="bi bi-clock me-1"></i>${cu ? Ui.fmtNgayGio(h.ngayGioKham) + ' <span class="text-danger">(ngày trước)</span>' : 'Tiếp nhận ' + gioPhut(h.ngayGioKham)}
          · <span class="mono">${esc(h.maHoSo)}</span> · ${nguon(h)}</small><br>
        <small class="d-block text-truncate"><i class="bi bi-chat-left-text me-1 text-primary"></i>${esc(h.trieuChung)}</small></div>
      ${nut}</div>`;
  }

  window.BacSi = { esc, homNay, gioPhut, tuoi, thongTinKhach, nguon, tenThuoc, mucHangCho };
})();
