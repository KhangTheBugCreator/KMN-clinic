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
  ],
  heroTitle: 'Chăm sóc sức khỏe tận tâm cho cả gia đình',
  heroSubtitle: 'Tra cứu bác sĩ, xem lịch làm việc và đặt lịch khám trực tuyến chỉ trong vài bước.',
  // Đoạn giới thiệu do chủ phòng khám cung cấp; để trống thì trang chủ không hiện đoạn văn này.
  about: 'KMN Clinic là phòng khám đa khoa tư nhân tại Phường Hòa Thuận, Vĩnh Long. Phòng khám tiếp nhận khám theo chuyên khoa với lịch làm việc của bác sĩ được công bố rõ ràng. Bạn có thể xem lịch, chọn bác sĩ và đặt lịch khám trực tuyến trước khi đến.',
  googleMapsEmbedUrl: 'https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d1124.5380450024638!2d106.34575418830666!3d9.92345159459746!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x31a0175ea296facb%3A0x55ded92e29068221!2zxJDhuqFpIEjhu41jIFRyw6AgVmluaA!5e1!3m2!1svi!2s!4v1794408505176!5m2!1svi!2s',
  // Mã bác sĩ nổi bật trên trang chủ; để trống thì lấy các bác sĩ có lịch gần nhất.
  featuredDoctorIds: [],
  // Ảnh riêng cho từng khoa, ví dụ { K0001: 'khoa/noi.svg' } (đường dẫn trong assets/img); khoa chưa có ảnh dùng khoa-1..4 xoay vòng.
  khoaImages: {}
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
  try {
    const u = new URL(url);
    const h = u.hostname.toLowerCase().replace(/^\[|\]$/g, '');
    // chỉ HTTPS công khai: không tải ảnh từ localhost, IP nội bộ, tên máy LAN (cùng quy tắc với backend)
    const noiBo = !h.includes('.') && !h.includes(':') || /\.(localhost|local|internal|lan|home|corp|intranet)$/.test(h)
      || /^(0|10|127)\./.test(h) || /^169\.254\./.test(h) || /^192\.168\./.test(h) || /^172\.(1[6-9]|2\d|3[01])\./.test(h)
      || /^100\.(6[4-9]|[7-9]\d|1[01]\d|12[0-7])\./.test(h) || /^(2(2[4-9]|[3-5]\d))\./.test(h)
      || h === '::1' || /^(fc|fd|fe8|fe9|fea|feb)/.test(h) && h.includes(':');
    ok = !!url && u.protocol === 'https:' && !u.username && !u.password && !u.port && !noiBo;
  } catch (e) { ok = false; }
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

/** Nút vai trò trên header public: chưa chọn / đang giả lập khách hàng hoặc nhân viên. */
function publicAccount() {
  const nd = window.Auth ? Auth.nguoiDung() : null;
  if (!nd) return `
            <a href="chon-vai-tro.html" class="btn btn-soft"><i class="bi bi-person-badge me-1"></i>Chọn vai trò</a>`;
  const vao = nd.loai === 'NV'
    ? `<li><a class="dropdown-item" href="${Auth.trangChinh(nd)}"><i class="bi bi-grid-1x2 me-2"></i>Trang nội bộ</a></li>`
    : `<li><a class="dropdown-item" href="lich-hen-cua-toi.html"><i class="bi bi-list-check me-2"></i>Lịch hẹn của tôi</a></li>`;
  return `
            <div class="dropdown">
              <button class="btn btn-soft dropdown-toggle w-100" data-bs-toggle="dropdown" aria-expanded="false">
                <i class="bi bi-person-circle me-1"></i>${Ui.esc(nd.hoTen)}</button>
              <ul class="dropdown-menu dropdown-menu-end shadow border-0 rounded-4">
                <li><span class="dropdown-item-text small text-muted-kmn">Đang giả lập: ${Ui.esc(nd.chucVu || 'Khách hàng')}</span></li>${vao}
                <li><hr class="dropdown-divider"></li>
                <li><a class="dropdown-item" href="#" data-action="doi-vai-tro"><i class="bi bi-arrow-left-right me-2"></i>Đổi vai trò</a></li>
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
      ['Tổng quan', [['bi-grid-1x2', 'Dashboard', 'dashboard-quan-tri.html'], ['bi-file-earmark-excel', 'Báo cáo Excel', 'bao-cao.html']]],
      ['Danh mục', [['bi-diagram-3', 'Khoa', 'danh-muc-khoa.html'], ['bi-door-open', 'Phòng làm việc', 'danh-muc-phong.html'], ['bi-clock-history', 'Ca làm việc', 'danh-muc-ca.html'], ['bi-capsule', 'Thuốc', 'thuoc.html']]],
      ['Con người', [['bi-person-badge', 'Nhân viên', 'nhan-vien.html'], ['bi-people', 'Khách hàng', 'khach-hang.html']]],
      ['Vận hành', [['bi-calendar3', 'Lịch làm việc', 'lich-lam-viec.html'], ['bi-calendar-check', 'Lịch hẹn', 'qt-lich-hen.html'], ['bi-journal-medical', 'Hồ sơ khám', 'qt-ho-so.html'], ['bi-receipt', 'Hóa đơn', 'qt-hoa-don.html']]]
    ]
  },
  LeTan: {
    icon: 'bi-person-workspace',
    groups: [
      ['Tổng quan', [['bi-grid-1x2', 'Dashboard', 'dashboard-le-tan.html']]],
      ['Tiếp đón', [['bi-calendar-check', 'Lịch hẹn', 'lich-hen.html'], ['bi-person-plus', 'Tiếp nhận khách', 'tiep-nhan.html'], ['bi-people', 'Khách hàng', 'khach-hang.html']]],
      ['Tra cứu', [['bi-calendar3', 'Lịch làm việc bác sĩ', 'lich-lam-viec.html']]]
    ]
  },
  BacSi: {
    icon: 'bi-heart-pulse',
    groups: [
      ['Tổng quan', [['bi-grid-1x2', 'Dashboard', 'dashboard-bac-si.html']]],
      ['Khám bệnh', [['bi-hourglass-split', 'Hàng chờ khám', 'kham-benh.html'], ['bi-journal-medical', 'Hồ sơ của tôi', 'ho-so-cua-toi.html'], ['bi-capsule', 'Tra cứu thuốc', 'thuoc.html']]],
      ['Lịch', [['bi-calendar3', 'Lịch làm việc của tôi', 'lich-lam-viec.html'], ['bi-calendar-check', 'Lịch hẹn của tôi', 'lich-hen-bac-si.html']]]
    ]
  },
  ThuNgan: {
    icon: 'bi-cash-coin',
    groups: [
      ['Tổng quan', [['bi-grid-1x2', 'Dashboard', 'dashboard-thu-ngan.html']]],
      ['Thanh toán', [['bi-receipt', 'Hóa đơn chờ thu', 'hoa-don-cho-thu.html'], ['bi-check2-circle', 'Lịch sử thanh toán', 'lich-su-thanh-toan.html']]],
      ['Tra cứu', [['bi-capsule', 'Thuốc', 'thuoc.html']]]
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
          <li><span class="dropdown-item-text small text-muted-kmn">Đang giả lập vai trò này</span></li>
          <li><a class="dropdown-item" href="../trang-chu.html"><i class="bi bi-house me-2"></i>Xem website</a></li>
          <li><hr class="dropdown-divider"></li>
          <li><a class="dropdown-item" href="#" data-action="doi-vai-tro"><i class="bi bi-arrow-left-right me-2"></i>Đổi vai trò</a></li>
        </ul>
      </div>
    </div>
  </div>`;
}

/* ---------- Khởi tạo khi trang tải xong ---------- */
(function khoiTao() {
  const body = document.body;

  // Trang nội bộ: chặn ngay khi chưa đăng nhập hoặc sai vai trò (backend vẫn kiểm tra quyền ở từng API).
  if (body.dataset.layout === 'admin' && !Auth.yeuCau((body.dataset.vaiTro || '').split(',').filter(Boolean))) {
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

    // Đổi vai trò
    document.addEventListener('click', e => {
      const a = e.target.closest('[data-action="doi-vai-tro"]');
      if (a) { e.preventDefault(); Auth.doiVaiTro(); }
    });

    // Ảnh đại diện: mọi <img data-avatar> đều đi qua renderAvatar
    document.querySelectorAll('img[data-avatar]').forEach(img => renderAvatar(img, img.dataset.avatar));
  });
})();
