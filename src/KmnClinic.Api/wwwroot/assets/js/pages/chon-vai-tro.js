/* Trang Chọn vai trò: giả lập vai trò thay cho đăng nhập (quyết định 12/11/2026). */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const thongBao = $('thongBao');
  const khungNV = $('khungNhanVien'), khungKH = $('khungKhachHang');

  if (Ui.thamSo('hetPhien')) {
    thongBao.textContent = 'Phiên làm việc đã hết hạn. Vui lòng chọn lại vai trò.';
    thongBao.classList.remove('d-none');
  }

  /** Vào vai trò xong thì quay lại trang đang mở dở (chỉ đường dẫn trong website) hoặc trang chính của vai trò. */
  function vao(phien) {
    Auth.luu(phien);
    const quayLai = Ui.thamSo('quayLai');
    const can = Ui.thamSo('vaiTro');
    const hopLe = quayLai && quayLai.startsWith('/') && !quayLai.startsWith('//')
      && (!can || can === phien.nguoiDung.vaiTro);
    location.href = hopLe ? quayLai : Api.goc + Auth.trangChinh(phien.nguoiDung);
  }

  async function vaoVaiTro(loai, ma, btn) {
    try {
      const phien = await Ui.dangXuLy(btn, () => Api.post('gia-lap/vao-vai-tro', { loai, ma }));
      vao(phien);
    } catch (err) { Ui.loi(err); }
  }

  /* ---------- Bước 1: chọn vai trò ---------- */
  document.querySelectorAll('[data-vai-tro]').forEach(card => card.addEventListener('click', () => chonVaiTro(card)));

  function chonVaiTro(card) {
    document.querySelectorAll('[data-vai-tro]').forEach(c => {
      c.classList.toggle('selected', c === card);
      c.setAttribute('aria-checked', c === card);
    });
    if (card.dataset.vaiTro === 'KhachHang') {
      khungNV.classList.add('d-none'); khungKH.classList.remove('d-none');
      taiKhachHang('');
    } else {
      khungKH.classList.add('d-none'); khungNV.classList.remove('d-none');
      taiNhanVien(card.dataset.chucVu);
    }
  }

  /* ---------- Bước 2a: nhân viên ---------- */
  async function taiNhanVien(chucVu) {
    $('tenChucVu').textContent = chucVu.toLowerCase();
    const ds = $('dsNhanVien');
    ds.innerHTML = '<div class="col-12"><div class="skeleton" style="height:64px"></div></div>';
    try {
      const nvs = await Api.get('gia-lap/nhan-vien?chucVu=' + encodeURIComponent(chucVu));
      if (!nvs.length) {
        ds.innerHTML = `<div class="col-12 empty-state"><i class="bi bi-person-x"></i>Chưa có nhân viên nào có chức vụ ${Ui.esc(chucVu)} trong database.</div>`;
        return;
      }
      ds.innerHTML = nvs.map(nv => `
        <div class="col-sm-6 col-lg-4"><button type="button" class="pick-card w-100 d-flex gap-3 align-items-center text-start" data-ma="${Ui.esc(nv.ma)}">
          <img class="avatar" alt="" data-avatar="${Ui.esc(nv.duongDanAnh || '')}" style="width:48px;height:48px">
          <span><b class="text-navy d-block">${Ui.esc(nv.hoTen)}</b>
            <small class="text-muted-kmn">${Ui.esc(nv.ma)}${nv.tenKhoa ? ' · ' + Ui.esc(nv.tenKhoa) : ''}</small></span></button></div>`).join('');
      ds.querySelectorAll('img[data-avatar]').forEach(img => renderAvatar(img, img.dataset.avatar));
      ds.querySelectorAll('[data-ma]').forEach(b => b.addEventListener('click', () => vaoVaiTro('NV', b.dataset.ma, b)));
    } catch (err) { ds.innerHTML = ''; Ui.loi(err); }
  }

  /* ---------- Bước 2b: khách hàng ---------- */
  let hen;
  $('timKhach').addEventListener('input', e => {
    clearTimeout(hen);
    hen = setTimeout(() => taiKhachHang(e.target.value.trim()), 300);
  });

  async function taiKhachHang(q) {
    const ds = $('dsKhachHang');
    try {
      const khs = await Api.get('gia-lap/khach-hang' + (q ? '?q=' + encodeURIComponent(q) : ''));
      if (!khs.length) {
        ds.innerHTML = '<div class="empty-state"><i class="bi bi-search"></i>Không tìm thấy khách hàng phù hợp.</div>';
        return;
      }
      ds.innerHTML = khs.map(kh => `
        <button type="button" class="list-group-item list-group-item-action d-flex justify-content-between align-items-center" data-ma="${Ui.esc(kh.ma)}">
          <span><b class="text-navy">${Ui.esc(kh.hoTen)}</b><br><small class="text-muted-kmn">${Ui.esc(kh.soDienThoai)} · ${Ui.esc(kh.gioiTinh)} · ${kh.namSinh}</small></span>
          <i class="bi bi-chevron-right text-muted-kmn"></i></button>`).join('');
      ds.querySelectorAll('[data-ma]').forEach(b => b.addEventListener('click', () => vaoVaiTro('KH', b.dataset.ma, b)));
    } catch (err) { Ui.loi(err); }
  }

  const form = $('formKhachMoi'), formLoi = form.querySelector('.alert');
  const baoLoi = msg => { formLoi.textContent = msg; formLoi.classList.remove('d-none'); };
  form.addEventListener('submit', async e => {
    e.preventDefault();
    formLoi.classList.add('d-none');
    const du = {
      hoTen: form.hoTen.value.trim(), ngaySinh: form.ngaySinh.value || null, gioiTinh: form.gioiTinh.value,
      soDienThoai: form.sdt.value.trim(), diaChi: form.diaChi.value.trim() || null
    };
    if (!du.hoTen) return baoLoi('Vui lòng nhập họ tên.');
    if (!du.ngaySinh) return baoLoi('Vui lòng nhập ngày sinh.');
    if (!du.gioiTinh) return baoLoi('Vui lòng chọn giới tính.');
    if (!/^0\d{9}$/.test(du.soDienThoai)) return baoLoi('Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.');
    try {
      vao(await Ui.dangXuLy(form.querySelector('[type=submit]'), () => Api.post('gia-lap/khach-hang', du)));
    } catch (err) { baoLoi(err.message); }
  });

  // Được chuyển tới từ một trang cần vai trò cụ thể: chọn sẵn vai trò đó.
  const can = Ui.thamSo('vaiTro');
  const the = can && document.querySelector(`[data-vai-tro="${CSS.escape(can)}"]`);
  if (the) chonVaiTro(the);
})();
