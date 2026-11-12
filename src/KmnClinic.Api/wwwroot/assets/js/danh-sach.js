/* =========================================================
   danh-sach.js – khung dùng chung cho các trang danh sách có
   tìm kiếm, phân trang và form thêm/sửa trong modal.
   ========================================================= */
(function () {
  'use strict';

  /**
   * Bảng có phân trang lấy dữ liệu từ API.
   * cfg: { url, tbody, phanTrang, soCot, dong(item) -> html, thamSo() -> object, rong, pageSize, sauKhiVe(items) }
   * Trả về { tai(trang?) } để tải lại.
   */
  function bangPhanTrang(cfg) {
    let trang = 1;
    async function tai(p) {
      if (p) trang = p;
      cfg.tbody.innerHTML = Ui.dongDangTai(cfg.soCot);
      const ts = new URLSearchParams({ page: trang, pageSize: cfg.pageSize || 10 });
      Object.entries(cfg.thamSo ? cfg.thamSo() : {}).forEach(([k, v]) => { if (v !== '' && v != null && v !== false) ts.set(k, v); });
      try {
        const kq = await Api.get(cfg.url + '?' + ts);
        if (!kq.items.length && trang > 1) return tai(trang - 1); // xóa hết trang cuối thì lùi một trang
        cfg.tbody.innerHTML = kq.items.length ? kq.items.map(cfg.dong).join('') : Ui.dongTrong(cfg.soCot, cfg.rong || 'Chưa có dữ liệu.');
        if (cfg.phanTrang) Ui.phanTrang(cfg.phanTrang, kq, tai);
        cfg.sauKhiVe?.(kq.items);
      } catch (err) {
        cfg.tbody.innerHTML = Ui.dongTrong(cfg.soCot, err.message, 'exclamation-circle');
      }
    }
    return { tai };
  }

  /**
   * Modal form thêm/sửa. cfg: { tieuDe, noiDung (html các ô), nutLuu, khiMo(form), thuThap(form) -> body | string lỗi, gui(body) -> Promise }
   * Lỗi từ API hiện ngay trong modal, modal chỉ đóng khi lưu thành công.
   */
  function moForm(cfg) {
    const wrap = document.createElement('div');
    wrap.className = 'modal fade';
    wrap.tabIndex = -1;
    wrap.innerHTML = `
      <div class="modal-dialog modal-dialog-centered ${cfg.rong ? 'modal-lg' : ''}"><form class="modal-content rounded-4 border-0" novalidate>
        <div class="modal-header border-0"><h2 class="modal-title h5">${Ui.esc(cfg.tieuDe)}</h2>
          <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Đóng"></button></div>
        <div class="modal-body pt-0"><div class="alert alert-danger small d-none" role="alert"></div>${cfg.noiDung}</div>
        <div class="modal-footer border-0"><button type="button" class="btn btn-soft" data-bs-dismiss="modal">Hủy</button>
          <button type="submit" class="btn btn-primary">${Ui.esc(cfg.nutLuu || 'Lưu')}</button></div>
      </form></div>`;
    document.body.appendChild(wrap);
    const form = wrap.querySelector('form'), alertEl = wrap.querySelector('.alert');
    const m = bootstrap.Modal.getOrCreateInstance(wrap);
    const baoLoi = msg => { alertEl.textContent = msg; alertEl.classList.remove('d-none'); alertEl.scrollIntoView({ block: 'nearest' }); };
    cfg.khiMo?.(form);
    form.addEventListener('submit', async e => {
      e.preventDefault();
      alertEl.classList.add('d-none');
      const body = cfg.thuThap(form);
      if (typeof body === 'string') return baoLoi(body);
      if (body === null) return; // người dùng hủy ở bước xác nhận
      try {
        await Ui.dangXuLy(form.querySelector('[type=submit]'), () => cfg.gui(body));
        m.hide();
      } catch (err) { baoLoi(err.message); }
    });
    wrap.addEventListener('shown.bs.modal', () => form.querySelector('input:not([disabled]),select:not([disabled]),textarea')?.focus());
    wrap.addEventListener('hidden.bs.modal', () => wrap.remove());
    m.show();
    return { form, dong: () => m.hide() };
  }

  /** Xác nhận rồi gọi API xóa; báo kết quả bằng toast. */
  async function xoa({ tieuDe, noiDung, url, sauKhiXoa }) {
    if (!await Ui.xacNhan({ tieuDe, noiDung, nutDongY: 'Xóa', kieu: 'danger', icon: 'trash' })) return;
    try {
      const kq = await Api.del(url);
      Ui.toast(kq?.message || 'Đã xóa.');
      sauKhiXoa?.();
    } catch (err) { Ui.loi(err); }
  }

  window.DanhSach = { bangPhanTrang, moForm, xoa };
})();
