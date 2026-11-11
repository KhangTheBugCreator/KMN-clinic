/* Đăng nhập khách hàng (dang-nhap.html) và nhân viên (admin/dang-nhap.html). */
(function () {
  'use strict';
  const form = document.getElementById('formDangNhap');
  if (!form) return;
  const laNhanVien = form.dataset.loai === 'nhan-vien';
  const alertEl = document.getElementById('loginError');

  const baoLoi = msg => {
    alertEl.querySelector('span').textContent = msg;
    alertEl.classList.remove('d-none');
  };

  // Đã đăng nhập đúng loại tài khoản thì chuyển thẳng vào trang chính.
  const nd = Auth.nguoiDung();
  if (nd && (nd.loai === 'NV') === laNhanVien) {
    location.replace(Api.goc + Auth.trangChinh(nd));
    return;
  }
  if (Ui.thamSo('hetPhien')) baoLoi('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');

  document.getElementById('btnHienMk')?.addEventListener('click', e => {
    const hien = form.mk.type === 'password';
    form.mk.type = hien ? 'text' : 'password';
    e.currentTarget.innerHTML = `<i class="bi bi-eye${hien ? '-slash' : ''}"></i>`;
  });

  form.addEventListener('submit', async e => {
    e.preventDefault();
    alertEl.classList.add('d-none');
    const sdt = form.sdt.value.trim(), mk = form.mk.value;
    if (!/^0\d{9}$/.test(sdt)) return baoLoi('Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.');
    if (!mk) return baoLoi('Vui lòng nhập mật khẩu.');

    try {
      const phien = await Ui.dangXuLy(form.querySelector('[type=submit]'), () =>
        Api.post(laNhanVien ? 'auth/nhan-vien/dang-nhap' : 'auth/khach-hang/dang-nhap',
          { soDienThoai: sdt, matKhau: mk }));
      Auth.luu(phien);
      // Chỉ quay lại đường dẫn nội bộ của website (chống chuyển hướng ra ngoài).
      const quayLai = Ui.thamSo('quayLai');
      const hopLe = quayLai && quayLai.startsWith('/') && !quayLai.startsWith('//');
      location.href = hopLe ? quayLai : Api.goc + Auth.trangChinh(phien.nguoiDung);
    } catch (err) {
      baoLoi(err.message);
      form.mk.value = '';
      form.mk.focus();
    }
  });
})();
