/* =========================================================
   layout.js – dựng header, footer, sidebar dùng chung.
   Cần nạp sau api.js và ui.js.
   Nội dung tĩnh (SITE) lấy từ site.json của chủ dự án.
   ========================================================= */

const SITE = {
  name: 'KMN Clinic',
  fullName: 'Phòng khám Đa khoa KMN',
  address: '126 Nguyễn Thiện Thành, Phường Hòa Thuận, Vĩnh Long, Việt Nam',
  phone: '036 6767 367',
  hotline: '123 321 367',
  email: 'khangnhutminhtvu@gmail.com',
  workingHours: [
    { days: 'Thứ 2 – Thứ 6', hours: '7:00 – 17:00' },
    { days: 'Thứ 7 – Chủ nhật', hours: '8:00 – 16:00' }
  ]
};

/* Đường dẫn gốc tới thư mục assets, tính theo vị trí trang */
const ROOT = document.currentScript.src.replace(/assets\/js\/layout\.js.*$/, '');
const IMG = ROOT + 'assets/img/';

/* ---------- Ảnh đại diện: luôn có ảnh dự phòng ---------- */
const DEFAULT_AVATAR = IMG + 'avatar-default.svg';

/**
 * Gán ảnh đại diện an toàn cho thẻ <img>.
 * - url rỗng / không phải https  -> dùng ảnh mặc định ngay
 * - ảnh lỗi khi tải              -> onerror đổi sang ảnh mặc định (chỉ 1 lần)
 * - quá 8 giây chưa tải xong      -> dùng ảnh mặc định
 */
function renderAvatar(img, url) {
  const useDefault = () => { img.onerror = null; img.src = DEFAULT_AVATAR; };
  let ok = false;
  try { ok = !!url && new URL(url).protocol === 'https:'; } catch (e) { ok = false; }
  if (!ok) { useDefault(); return; }
  img.onerror = useDefault;
  const timer = setTimeout(() => { if (!img.complete || img.naturalWidth === 0) useDefault(); }, 8000);
  img.onload = () => clearTimeout(timer);
  img.src = url;
}

/* ---------- Public: header + footer ---------- */
function publicHeader(active) {
  const links = [
    ['trang-chu', 'trang-chu.html', 'Trang chủ'],
    ['gioi-thieu', 'trang-chu.html#gioi-thieu', 'Giới thiệu'],
    ['chuyen-khoa', 'chuyen-khoa.html', 'Chuyên khoa'],
    ['bac-si', 'bac-si.html', 'Bác sĩ'],
    ['lich-kham', 'trang-chu.html#lich-kham', 'Lịch khám'],
    ['lien-he', 'trang-chu.html#lien-he', 'Liên hệ']
  ];
  const items = links.map(([k, href, t]) =>
    `<li class="nav-item"><a class="nav-link ${k === active ? 'active' : ''}" href="${href}">${t}</a></li>`).join('');
  return `
  <div class="topbar d-none d-md-block">
    <div class="container d-flex justify-content-between py-1">
      <span><i class="bi bi-geo-alt me-1"></i>${SITE.address}</span>
      <span><i class="bi bi-clock me-1"></i>${SITE.workingHours[0].days}: ${SITE.workingHours[0].hours}
        <span class="mx-2">·</span><i class="bi bi-telephone me-1"></i><a href="tel:${SITE.hotline.replace(/\s/g, '')}">${SITE.hotline}</a></span>
    </div>
  </div>
  <header class="site-header">
    <nav class="navbar navbar-expand-xl">
      <div class="container">
        <a class="navbar-brand brand" href="trang-chu.html"><img src="${IMG}logo.png" alt="${SITE.name}"></a>
        <button class="navbar-toggler border-0" type="button" data-bs-toggle="collapse" data-bs-target="#mainNav" aria-label="Mở menu">
          <i class="bi bi-list fs-2 text-navy"></i></button>
        <div class="collapse navbar-collapse" id="mainNav">
          <ul class="navbar-nav mx-auto gap-xl-1">${items}</ul>
          <div class="d-flex flex-column flex-xl-row gap-2 mt-3 mt-xl-0">${publicAccount()}
            <a href="dat-lich.html" class="btn btn-primary"><i class="bi bi-calendar2-plus me-1"></i>Đặt lịch khám</a>
          </div>
        </div>
      </div>
    </nav>
  </header>`;
}

/** Nút tài khoản trên header public: chưa đăng nhập / khách hàng / nhân viên. */
function publicAccount() {
  const nd = window.Auth ? Auth.nguoiDung() : null;
  if (!nd) return `
            <a href="dang-nhap.html" class="btn btn-soft">Đăng nhập</a>
            <a href="dang-ky.html" class="btn btn-outline-primary">Đăng ký</a>`;
  const vao = nd.loai === 'NV'
    ? `<li><a class="dropdown-item" href="${Auth.trangChinh(nd)}"><i class="bi bi-grid-1x2 me-2"></i>Trang nội bộ</a></li>`
    : '';
  return `
            <div class="dropdown">
              <button class="btn btn-soft dropdown-toggle w-100" data-bs-toggle="dropdown" aria-expanded="false">
                <i class="bi bi-person-circle me-1"></i>${Ui.esc(nd.hoTen)}</button>
              <ul class="dropdown-menu dropdown-menu-end shadow border-0 rounded-4">${vao}
                <li><a class="dropdown-item" href="#" data-action="doi-mat-khau"><i class="bi bi-key me-2"></i>Đổi mật khẩu</a></li>
                <li><hr class="dropdown-divider"></li>
                <li><a class="dropdown-item text-danger" href="#" data-action="dang-xuat"><i class="bi bi-box-arrow-right me-2"></i>Đăng xuất</a></li>
              </ul>
            </div>`;
}

function publicFooter() {
  const hours = SITE.workingHours.map(h => `<li class="mb-1">${h.days}: <b>${h.hours}</b></li>`).join('');
  return `
  <footer class="site-footer">
    <div class="container">
      <div class="row g-4">
        <div class="col-lg-4">
          <img src="${IMG}logo.png" alt="${SITE.name}" class="footer-logo mb-3">
          <p class="text-muted-kmn mb-0">${SITE.fullName}. Đặt lịch khám trực tuyến, tra cứu bác sĩ và lịch làm việc.</p>
        </div>
        <div class="col-6 col-lg-2">
          <h6>Khám phá</h6>
          <ul class="list-unstyled">
            <li class="mb-1"><a href="chuyen-khoa.html">Chuyên khoa</a></li>
            <li class="mb-1"><a href="bac-si.html">Bác sĩ</a></li>
            <li class="mb-1"><a href="dat-lich.html">Đặt lịch khám</a></li>
          </ul>
        </div>
        <div class="col-6 col-lg-3">
          <h6>Giờ làm việc</h6>
          <ul class="list-unstyled">${hours}</ul>
        </div>
        <div class="col-lg-3">
          <h6>Liên hệ</h6>
          <ul class="list-unstyled">
            <li class="mb-1"><i class="bi bi-geo-alt me-2 text-primary"></i>${SITE.address}</li>
            <li class="mb-1"><i class="bi bi-telephone me-2 text-primary"></i>${SITE.phone}</li>
            <li class="mb-1"><i class="bi bi-headset me-2 text-primary"></i>Hotline ${SITE.hotline}</li>
            <li class="mb-1"><i class="bi bi-envelope me-2 text-primary"></i>${SITE.email}</li>
          </ul>
        </div>
      </div>
      <div class="copy d-flex flex-wrap justify-content-between gap-2">
        <span>© 2026 ${SITE.fullName}</span>
        <span>Hình ảnh trên website là ảnh minh họa.</span>
      </div>
    </div>
  </footer>`;
}

/* ---------- Internal: sidebar theo chức vụ ---------- */
const ROLE_MENU = {
  QuanTriVien: {
    icon: 'bi-shield-check',
    groups: [
      ['Tổng quan', [['bi-grid-1x2', 'Dashboard', 'dashboard-quan-tri.html'], ['bi-bar-chart-line', 'Thống kê', '#'], ['bi-file-earmark-excel', 'Báo cáo Excel', '#']]],
      ['Danh mục', [['bi-diagram-3', 'Khoa', '#'], ['bi-door-open', 'Phòng làm việc', '#'], ['bi-clock-history', 'Ca làm việc', '#'], ['bi-capsule', 'Thuốc', '#']]],
      ['Con người', [['bi-person-badge', 'Nhân viên', '#'], ['bi-people', 'Khách hàng', '#']]],
      ['Vận hành', [['bi-calendar3', 'Lịch làm việc', '#'], ['bi-calendar-check', 'Lịch hẹn', '#'], ['bi-journal-medical', 'Hồ sơ khám', '#'], ['bi-receipt', 'Hóa đơn', '#']]]
    ]
  },
  LeTan: {
    icon: 'bi-person-workspace',
    groups: [
      ['Tổng quan', [['bi-grid-1x2', 'Dashboard', 'dashboard-le-tan.html']]],
      ['Tiếp đón', [['bi-calendar-check', 'Lịch hẹn', '#'], ['bi-person-plus', 'Tiếp nhận khách', '#'], ['bi-people', 'Khách hàng', '#'], ['bi-journal-medical', 'Hồ sơ khám', '#']]],
      ['Tra cứu', [['bi-calendar3', 'Lịch làm việc bác sĩ', '#']]]
    ]
  },
  BacSi: {
    icon: 'bi-heart-pulse',
    groups: [
      ['Tổng quan', [['bi-grid-1x2', 'Dashboard', 'dashboard-bac-si.html']]],
      ['Khám bệnh', [['bi-hourglass-split', 'Hàng chờ khám', '#'], ['bi-journal-medical', 'Hồ sơ của tôi', '#'], ['bi-capsule', 'Tra cứu thuốc', '#']]],
      ['Lịch', [['bi-calendar3', 'Lịch làm việc của tôi', '#'], ['bi-calendar-check', 'Lịch hẹn của tôi', '#']]]
    ]
  },
  ThuNgan: {
    icon: 'bi-cash-coin',
    groups: [
      ['Tổng quan', [['bi-grid-1x2', 'Dashboard', 'dashboard-thu-ngan.html']]],
      ['Thanh toán', [['bi-receipt', 'Hóa đơn chờ thu', '#'], ['bi-check2-circle', 'Lịch sử thanh toán', '#']]]
    ]
  }
};

function adminSidebar(role, roleLabel, activeHref) {
  const menu = ROLE_MENU[role];
  const groups = menu.groups.map(([title, links]) => `
    <div class="nav-group">
      <div class="nav-group-title">${title}</div>
      ${links.map(([ic, t, href, count]) => `
        <a class="side-link ${href === activeHref ? 'active' : ''}" href="${href}">
          <i class="bi ${ic}"></i><span>${t}</span>${count ? `<span class="count">${count}</span>` : ''}</a>`).join('')}
    </div>`).join('');
  return `
  <aside class="sidebar" id="sidebar">
    <div class="brand"><img src="${IMG}logo.png" alt="${SITE.name}"></div>
    <div class="role-chip"><i class="bi ${menu.icon}"></i>${Ui.esc(roleLabel)}</div>
    ${groups}
    <div class="side-foot">${SITE.fullName}<br>Hệ thống quản lý nội bộ</div>
  </aside>
  <div class="sidebar-backdrop" id="sidebarBackdrop"></div>`;
}

function adminTopnav(user) {
  return `
  <div class="topnav">
    <button class="icon-btn d-lg-none" id="btnMenu" aria-label="Mở menu"><i class="bi bi-list"></i></button>
    <div class="quick-find position-relative">
      <i class="bi bi-search"></i>
      <input class="form-control" placeholder="${Ui.esc(user.search)}" aria-label="Tra cứu nhanh">
    </div>
    <div class="ms-auto d-flex align-items-center gap-2">
      <div class="dropdown">
        <button class="btn p-0 border-0 user-box" data-bs-toggle="dropdown" aria-expanded="false">
          <img class="avatar" alt="" data-avatar="${Ui.esc(user.avatar || '')}">
          <span class="meta text-start"><span class="name d-block">${Ui.esc(user.name)}</span><small>${Ui.esc(user.role)}</small></span>
          <i class="bi bi-chevron-down text-muted-kmn small"></i>
        </button>
        <ul class="dropdown-menu dropdown-menu-end shadow border-0 rounded-4">
          <li><a class="dropdown-item" href="#" data-action="doi-mat-khau"><i class="bi bi-key me-2"></i>Đổi mật khẩu</a></li>
          <li><hr class="dropdown-divider"></li>
          <li><a class="dropdown-item text-danger" href="#" data-action="dang-xuat"><i class="bi bi-box-arrow-right me-2"></i>Đăng xuất</a></li>
        </ul>
      </div>
    </div>
  </div>`;
}

/* ---------- Đổi mật khẩu (dùng chung cho khách hàng và nhân viên) ---------- */
function moDoiMatKhau() {
  const wrap = document.createElement('div');
  wrap.className = 'modal fade';
  wrap.tabIndex = -1;
  wrap.innerHTML = `
    <div class="modal-dialog modal-dialog-centered"><form class="modal-content rounded-4 border-0" novalidate>
      <div class="modal-header border-0"><h2 class="modal-title h5">Đổi mật khẩu</h2>
        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Đóng"></button></div>
      <div class="modal-body pt-0">
        <div class="alert alert-danger small d-none" role="alert"></div>
        <label class="form-label" for="mkCu">Mật khẩu hiện tại</label>
        <input id="mkCu" type="password" class="form-control mb-3" autocomplete="current-password" required>
        <label class="form-label" for="mkMoi">Mật khẩu mới</label>
        <input id="mkMoi" type="password" class="form-control mb-3" autocomplete="new-password" minlength="8" required>
        <label class="form-label" for="mkMoi2">Nhập lại mật khẩu mới</label>
        <input id="mkMoi2" type="password" class="form-control" autocomplete="new-password" required>
        <div class="form-text">Tối thiểu 8 ký tự.</div>
      </div>
      <div class="modal-footer border-0"><button type="button" class="btn btn-soft" data-bs-dismiss="modal">Hủy</button>
        <button type="submit" class="btn btn-primary">Lưu mật khẩu</button></div>
    </form></div>`;
  document.body.appendChild(wrap);
  const m = bootstrap.Modal.getOrCreateInstance(wrap);
  const form = wrap.querySelector('form'), alertEl = wrap.querySelector('.alert');
  const baoLoi = msg => { alertEl.textContent = msg; alertEl.classList.remove('d-none'); };
  form.addEventListener('submit', async e => {
    e.preventDefault();
    alertEl.classList.add('d-none');
    const cu = form.mkCu.value, moi = form.mkMoi.value, moi2 = form.mkMoi2.value;
    if (!cu) return baoLoi('Vui lòng nhập mật khẩu hiện tại.');
    if (moi.length < 8) return baoLoi('Mật khẩu mới phải có ít nhất 8 ký tự.');
    if (moi !== moi2) return baoLoi('Hai lần nhập mật khẩu mới không khớp.');
    try {
      await Ui.dangXuLy(form.querySelector('[type=submit]'),
        () => Api.put('auth/doi-mat-khau', { matKhauCu: cu, matKhauMoi: moi }));
      m.hide();
      Ui.toast('Đã đổi mật khẩu.');
    } catch (err) { baoLoi(err.message); }
  });
  wrap.addEventListener('hidden.bs.modal', () => wrap.remove());
  m.show();
}

/* ---------- Khởi tạo khi trang tải xong ---------- */
(function khoiTao() {
  const body = document.body;

  // Trang nội bộ: chặn ngay khi chưa đăng nhập hoặc sai vai trò (backend vẫn kiểm tra quyền ở từng API).
  if (body.dataset.layout === 'admin' && !Auth.yeuCau([body.dataset.vaiTro], true)) {
    body.style.visibility = 'hidden';
    return;
  }

  document.addEventListener('DOMContentLoaded', () => {
    if (body.dataset.layout === 'public') {
      document.getElementById('site-header')?.insertAdjacentHTML('afterbegin', publicHeader(body.dataset.active));
      document.getElementById('site-footer')?.insertAdjacentHTML('afterbegin', publicFooter());
    }

    if (body.dataset.layout === 'admin') {
      const nd = Auth.nguoiDung();
      const shell = document.getElementById('admin-shell');
      shell.insertAdjacentHTML('afterbegin', adminSidebar(nd.vaiTro, nd.chucVu || '', location.pathname.split('/').pop()));
      document.getElementById('topnav').innerHTML = adminTopnav({
        name: nd.hoTen, role: nd.chucVu || '', avatar: nd.duongDanAnh,
        search: body.dataset.search || 'Tra cứu nhanh...'
      });
      // Mở/đóng sidebar trên điện thoại
      const sb = document.getElementById('sidebar'), bd = document.getElementById('sidebarBackdrop');
      const toggle = () => { sb.classList.toggle('show'); bd.classList.toggle('show'); };
      document.getElementById('btnMenu')?.addEventListener('click', toggle);
      bd.addEventListener('click', toggle);
    }

    // Đăng xuất / đổi mật khẩu
    document.addEventListener('click', e => {
      const a = e.target.closest('[data-action]');
      if (!a) return;
      if (a.dataset.action === 'dang-xuat') { e.preventDefault(); Auth.dangXuat(); }
      if (a.dataset.action === 'doi-mat-khau') { e.preventDefault(); moDoiMatKhau(); }
    });

    // Ảnh đại diện: mọi <img data-avatar> đều đi qua renderAvatar
    document.querySelectorAll('img[data-avatar]').forEach(img => renderAvatar(img, img.dataset.avatar));

    // Trang còn dùng dữ liệu mẫu (chưa nối API) có nhãn nhắc
    if (body.dataset.mau === '1') {
      body.insertAdjacentHTML('beforeend',
        '<div class="design-ribbon"><i class="bi bi-brush me-1"></i>Nội dung mẫu · chưa nối dữ liệu</div>');
    }
  });
})();
