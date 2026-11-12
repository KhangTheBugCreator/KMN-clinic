/* =========================================================
   ui.js – tiện ích giao diện dùng chung: toast, hộp xác nhận,
   định dạng tiền/ngày, chống XSS, khóa nút khi đang gửi.
   ========================================================= */
(function () {
  'use strict';

  /** Chống XSS: luôn escape dữ liệu từ API trước khi chèn vào HTML. */
  function esc(v) {
    return String(v ?? '').replace(/[&<>"']/g, c =>
      ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  }

  /* ---------- Toast ---------- */
  function vungToast() {
    let el = document.getElementById('toastZone');
    if (!el) {
      el = document.createElement('div');
      el.id = 'toastZone';
      el.className = 'toast-container position-fixed top-0 end-0 p-3';
      el.style.zIndex = 2100;
      document.body.appendChild(el);
    }
    return el;
  }

  /** kieu: 'success' | 'danger' | 'warning' | 'info' */
  function toast(message, kieu = 'success') {
    const icon = { success: 'check-circle', danger: 'exclamation-circle', warning: 'exclamation-triangle', info: 'info-circle' }[kieu] || 'info-circle';
    const el = document.createElement('div');
    el.className = `toast align-items-center text-bg-${kieu} border-0`;
    el.setAttribute('role', kieu === 'danger' ? 'alert' : 'status');
    el.innerHTML = `<div class="d-flex"><div class="toast-body"><i class="bi bi-${icon} me-2"></i>${esc(message)}</div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Đóng"></button></div>`;
    vungToast().appendChild(el);
    const t = bootstrap.Toast.getOrCreateInstance(el, { delay: kieu === 'danger' ? 6000 : 3500 });
    el.addEventListener('hidden.bs.toast', () => el.remove());
    t.show();
  }

  /** Hiện lỗi từ API (ApiError) hoặc lỗi bất kỳ. */
  function loi(err) {
    toast(err?.message || 'Có lỗi xảy ra. Vui lòng thử lại.', 'danger');
  }

  /* ---------- Hộp xác nhận (Promise<boolean>) ---------- */
  function xacNhan({ tieuDe = 'Xác nhận', noiDung = '', nutDongY = 'Đồng ý', kieu = 'primary', icon = 'question-circle' } = {}) {
    return new Promise(resolve => {
      const wrap = document.createElement('div');
      wrap.className = 'modal fade';
      wrap.tabIndex = -1;
      wrap.innerHTML = `
        <div class="modal-dialog modal-dialog-centered"><div class="modal-content rounded-4 border-0"><div class="modal-body p-4 text-center">
          <span class="icon-circle ic-${kieu === 'danger' ? 'red' : 'blue'} mb-3" style="width:60px;height:60px;font-size:1.6rem"><i class="bi bi-${icon}"></i></span>
          <h2 class="h5">${esc(tieuDe)}</h2>
          <p class="text-muted-kmn">${esc(noiDung)}</p>
          <div class="d-flex justify-content-center gap-2">
            <button type="button" class="btn btn-soft" data-kq="0">Quay lại</button>
            <button type="button" class="btn btn-${kieu}" data-kq="1">${esc(nutDongY)}</button></div>
        </div></div></div>`;
      document.body.appendChild(wrap);
      const m = bootstrap.Modal.getOrCreateInstance(wrap);
      let kq = false;
      wrap.querySelectorAll('[data-kq]').forEach(b => b.addEventListener('click', () => { kq = b.dataset.kq === '1'; m.hide(); }));
      wrap.addEventListener('hidden.bs.modal', () => { wrap.remove(); resolve(kq); });
      m.show();
    });
  }

  /**
   * Khóa nút trong lúc chờ API (chống bấm 2 lần), hiện vòng xoay, mở lại khi xong.
   * Dùng: await Ui.dangXuLy(btn, () => Api.post(...))
   */
  async function dangXuLy(btn, fn) {
    if (!btn || btn.disabled) return;
    const html = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = `<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>${esc(btn.textContent.trim())}`;
    try { return await fn(); }
    finally { btn.disabled = false; btn.innerHTML = html; }
  }

  /* ---------- Định dạng ---------- */
  const fmtTien = n => (Number(n) || 0).toLocaleString('vi-VN') + ' đ';
  const pad = n => String(n).padStart(2, '0');
  function fmtNgay(v) {
    if (!v) return '';
    const d = new Date(v);
    return isNaN(d) ? '' : `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`;
  }
  function fmtNgayGio(v) {
    if (!v) return '';
    const d = new Date(v);
    return isNaN(d) ? '' : `${fmtNgay(d)} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }
  const fmtGio = v => (v || '').slice(0, 5).replace(/^0/, '');

  /** Trạng thái hiển thị dạng badge (khớp CHECK trong database). */
  function badgeTrangThai(tt) {
    const map = {
      'Đã đặt': 'st-dat', 'Đã xác nhận': 'st-xacnhan', 'Đã khám': 'st-kham', 'Đã hủy': 'st-huy',
      'Chưa thanh toán': 'st-chua-tt', 'Đã thanh toán': 'st-kham'
    };
    return `<span class="badge-st ${map[tt] || 'st-huy'}">${esc(tt)}</span>`;
  }

  /** Lấy tham số trên URL. */
  const thamSo = ten => new URLSearchParams(location.search).get(ten);

  window.Ui = { esc, toast, loi, xacNhan, dangXuLy, fmtTien, fmtNgay, fmtNgayGio, fmtGio, badgeTrangThai, thamSo };
})();
