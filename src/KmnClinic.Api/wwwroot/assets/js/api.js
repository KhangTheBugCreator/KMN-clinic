/* =========================================================
   api.js – gọi REST API bằng Fetch và quản lý phiên đăng nhập.
   - Tự gắn token JWT vào header Authorization.
   - Lỗi từ backend luôn có dạng { message } (tiếng Việt), được ném ra dưới dạng ApiError.
   - 401: xóa phiên và chuyển về trang đăng nhập phù hợp.
   ========================================================= */
(function () {
  'use strict';

  const KHOA_PHIEN = 'kmn.phien';

  /* ---------- Phiên đăng nhập ---------- */
  const Auth = {
    /** Lưu phiên trả về từ /api/gia-lap/... (vai trò giả lập). */
    luu(phien) {
      localStorage.setItem(KHOA_PHIEN, JSON.stringify({
        token: phien.token, hetHan: phien.hetHan, nguoiDung: phien.nguoiDung
      }));
    },
    lay() {
      try {
        const p = JSON.parse(localStorage.getItem(KHOA_PHIEN) || 'null');
        if (!p || !p.token) return null;
        if (p.hetHan && new Date(p.hetHan) <= new Date()) { Auth.xoa(); return null; }
        return p;
      } catch (e) { Auth.xoa(); return null; }
    },
    xoa() { localStorage.removeItem(KHOA_PHIEN); },
    nguoiDung() { return Auth.lay()?.nguoiDung || null; },
    laNhanVien() { return Auth.nguoiDung()?.loai === 'NV'; },

    /** Trang tương ứng khi đăng nhập xong, theo vai trò. */
    trangChinh(nd) {
      switch (nd?.vaiTro) {
        case 'QuanTriVien': return 'admin/dashboard-quan-tri.html';
        case 'LeTan': return 'admin/dashboard-le-tan.html';
        case 'BacSi': return 'admin/dashboard-bac-si.html';
        case 'ThuNgan': return 'admin/dashboard-thu-ngan.html';
        case 'KhachHang': return 'trang-chu.html';
        default: return 'trang-chu.html';
      }
    },

    /** Trang chọn vai trò (thay cho đăng nhập). */
    trangChonVaiTro() {
      return Api.goc + 'chon-vai-tro.html';
    },

    /**
     * Chặn trang theo vai trò ngay khi tải (chỉ để điều hướng giao diện;
     * quyền thật được kiểm tra ở backend cho từng API).
     */
    yeuCau(vaiTros) {
      const nd = Auth.nguoiDung();
      if (!nd || (vaiTros && vaiTros.length && !vaiTros.includes(nd.vaiTro))) {
        const quayLai = encodeURIComponent(location.pathname + location.search);
        const can = vaiTros && vaiTros.length ? '&vaiTro=' + encodeURIComponent(vaiTros[0]) : '';
        location.replace(Auth.trangChonVaiTro() + '?quayLai=' + quayLai + can);
        return false;
      }
      return true;
    },

    /** Thoát vai trò hiện tại và về trang chọn vai trò. */
    doiVaiTro() {
      Auth.xoa();
      location.href = Auth.trangChonVaiTro();
    }
  };

  /* ---------- Lỗi API ---------- */
  class ApiError extends Error {
    constructor(status, message) { super(message); this.status = status; this.name = 'ApiError'; }
  }

  /* ---------- Gọi API ---------- */
  // Gốc của website (thư mục chứa assets/), tính từ vị trí file này.
  const goc = (document.currentScript?.src || location.href).replace(/assets\/js\/api\.js.*$/, '');

  async function request(method, url, body) {
    const headers = { 'Accept': 'application/json' };
    const phien = Auth.lay();
    if (phien) headers['Authorization'] = 'Bearer ' + phien.token;
    const opt = { method, headers };
    if (body !== undefined) {
      headers['Content-Type'] = 'application/json';
      opt.body = JSON.stringify(body);
    }

    let res;
    try {
      res = await fetch(goc + 'api/' + url.replace(/^\/?(api\/)?/, ''), opt);
    } catch (e) {
      throw new ApiError(0, 'Không kết nối được máy chủ. Vui lòng kiểm tra mạng và thử lại.');
    }

    let data = null;
    const text = await res.text();
    if (text) { try { data = JSON.parse(text); } catch (e) { data = null; } }

    if (res.ok) return data;

    const message = data?.message || 'Có lỗi xảy ra. Vui lòng thử lại.';
    // Phiên hết hạn hoặc không hợp lệ: về trang chọn vai trò.
    if (res.status === 401 && phien) {
      Auth.xoa();
      location.href = Auth.trangChonVaiTro() + '?hetPhien=1';
    }
    throw new ApiError(res.status, message);
  }

  const Api = {
    goc,
    get: (url) => request('GET', url),
    post: (url, body) => request('POST', url, body ?? {}),
    put: (url, body) => request('PUT', url, body ?? {}),
    del: (url) => request('DELETE', url),
  };

  window.Api = Api;
  window.Auth = Auth;
  window.ApiError = ApiError;
})();
