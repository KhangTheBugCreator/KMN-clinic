/* =========================================================
   cong-khai.js – hàm dùng chung cho các trang công khai (M4).
   Nạp sau api.js, ui.js, layout.js. Dữ liệu lấy từ /api/cong-khai.
   ========================================================= */
(function () {
  const esc = Ui.esc;
  const THU = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
  const THU_DAY = ['Chủ nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7'];
  const pad = n => String(n).padStart(2, '0');

  /** Ngày theo giờ máy người xem, dạng YYYY-MM-DD. */
  const iso = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  const homNay = () => iso(new Date());
  const congNgay = (s, n) => { const [y, m, d] = s.split('-').map(Number); return iso(new Date(y, m - 1, d + n)); };
  const thuCua = s => { const [y, m, d] = s.split('-').map(Number); return new Date(y, m - 1, d).getDay(); };
  /** "T5 12/11" */
  const ngayNgan = s => `${THU[thuCua(s)]} ${s.slice(8, 10)}/${s.slice(5, 7)}`;
  /** "Thứ 5, 12/11/2026" */
  const ngayDai = s => `${THU_DAY[thuCua(s)]}, ${Ui.fmtNgay(s)}`;
  const gio = l => `${Ui.fmtGio(l.gioBatDau)}–${Ui.fmtGio(l.gioKetThuc)}`;

  /** Ảnh khoa: ảnh riêng trong SITE.khoaImages, nếu không có thì xoay vòng bộ ảnh chung. */
  const anhKhoa = (maKhoa, i) => IMG + (SITE.khoaImages[maKhoa] || `khoa/khoa-${(i % 4) + 1}.svg`);

  /** Link sang trang đặt lịch (M5) với các lựa chọn đã có. */
  function linkDatLich(t = {}) {
    const p = new URLSearchParams();
    Object.entries(t).forEach(([k, v]) => { if (v) p.set(k, v); });
    const q = p.toString();
    return 'dat-lich.html' + (q ? '?' + q : '');
  }

  const slot = (l, chiTiet) => `<a class="slot text-decoration-none" href="${linkDatLich({ maNv: l.maNv, maLich: l.maLichLamViec })}"
      title="Đặt lịch ca này"><b>${ngayNgan(l.ngay)}</b>${esc(l.tenCa)}${chiTiet ? ' ' + gio(l) : ''} · ${esc(l.tenPhong)}</a>`;
  const dsSlot = (ds, chiTiet, rong = 'Chưa có lịch còn đặt được') => ds.length
    ? `<div class="slot-list">${ds.map(l => slot(l, chiTiet)).join('')}</div>`
    : `<div class="text-muted-kmn small"><i class="bi bi-calendar-x me-1"></i>${esc(rong)}</div>`;

  /** Gán ảnh đại diện cho các <img data-avatar> mới vẽ (qua kiểm tra an toàn của renderAvatar). */
  const veAnh = el => el.querySelectorAll('img[data-avatar]').forEach(img => renderAvatar(img, img.dataset.avatar));

  /** Ô báo lỗi khi không tải được dữ liệu. */
  const hopLoi = (err, thuLai) => `<div class="col-12"><div class="empty-state"><i class="bi bi-wifi-off"></i>${esc(err?.message || 'Không tải được dữ liệu.')}
    ${thuLai ? '<div class="mt-2"><button type="button" class="btn btn-soft btn-sm" data-thu-lai>Thử lại</button></div>' : ''}</div></div>`;

  /** Đổ danh sách khoa vào <select> (giữ option đầu "Tất cả"). */
  function doKhoaVaoSelect(sel, dsKhoa, chon) {
    sel.insertAdjacentHTML('beforeend', dsKhoa.map(k => `<option value="${esc(k.maKhoa)}">${esc(k.tenKhoa)}</option>`).join(''));
    if (chon && dsKhoa.some(k => k.maKhoa === chon)) sel.value = chon;
  }

  window.CongKhai = { esc, homNay, congNgay, ngayNgan, ngayDai, gio, anhKhoa, linkDatLich, slot, dsSlot, veAnh, hopLoi, doKhoaVaoSelect };
})();
