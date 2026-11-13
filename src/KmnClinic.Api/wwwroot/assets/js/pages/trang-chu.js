/* Trang chủ (M4): số liệu, chuyên khoa, bác sĩ có lịch gần nhất, lịch khám 7 ngày tới, liên hệ. */
(function () {
  const { esc, homNay, congNgay, ngayNgan, gio, anhKhoa, linkDatLich, dsSlot, veAnh, hopLoi, doKhoaVaoSelect } = CongKhai;
  const $ = id => document.getElementById(id);
  const SO_NGAY = 7;

  /* ---------- Nội dung tĩnh từ SITE ---------- */
  function veThongTinTinh() {
    const h = SITE.workingHours[0];
    $('heroThongTin').innerHTML = `<span><i class="bi bi-clock text-primary me-1"></i>${esc(h.days)}: ${esc(h.hours)}</span>
      <span><i class="bi bi-telephone text-primary me-1"></i>Hotline <a class="text-reset" href="tel:${esc(SITE.hotline.replace(/\s/g, ''))}">${esc(SITE.hotline)}</a></span>`;
    if (SITE.about.trim()) { $('gioiThieu').textContent = SITE.about; $('gioiThieu').classList.remove('d-none'); }

    const muc = (icon, mau, nhan, html) => `<div class="contact-item"><span class="icon-circle ${mau}"><i class="bi bi-${icon}"></i></span>
      <div><b class="text-navy">${nhan}</b><br>${html}</div></div>`;
    $('lienHe').innerHTML =
      muc('geo-alt', 'ic-blue', 'Địa chỉ', esc(SITE.address)) +
      muc('telephone', 'ic-cyan', 'Điện thoại', `${esc(SITE.phone)} · Hotline ${esc(SITE.hotline)}`) +
      muc('envelope', 'ic-green', 'Email', `<a href="mailto:${esc(SITE.email)}">${esc(SITE.email)}</a>`) +
      muc('clock', 'ic-orange', 'Giờ làm việc', SITE.workingHours.map(x => `${esc(x.days)}: ${esc(x.hours)}`).join('<br>'));
    if (SITE.googleMapsEmbedUrl.startsWith('https://www.google.com/maps/embed')) $('banDo').src = SITE.googleMapsEmbedUrl;
    else $('banDo').remove();

    // Tra cứu nhanh: chỉ chọn được ngày trong 14 ngày tới (giống API)
    $('tcNgay').min = homNay();
    $('tcNgay').max = congNgay(homNay(), 13);
    $('formTraCuu').addEventListener('submit', () => {
      // bỏ tham số rỗng cho URL gọn
      $('formTraCuu').querySelectorAll('[name]').forEach(el => { el.disabled = !el.value.trim(); });
      setTimeout(() => $('formTraCuu').querySelectorAll('[name]').forEach(el => { el.disabled = false; }));
    });
  }

  /* ---------- Số liệu ở hero ---------- */
  async function taiTongQuan() {
    try {
      const t = await Api.get('cong-khai/tong-quan');
      if (t.soKhoa > 0) {
        $('noiKhoa').querySelector('[data-so]').textContent = `${t.soKhoa} chuyên khoa`;
        $('noiKhoa').classList.remove('d-none');
      }
      if (t.soBacSi > 0) {
        $('noiBacSi').querySelector('[data-so]').textContent = `${t.soBacSi} bác sĩ`;
        $('noiBacSi').querySelector('[data-phu]').textContent = t.soBacSiCoLich7Ngay > 0
          ? `${t.soBacSiCoLich7Ngay} bác sĩ có lịch 7 ngày tới` : 'Đội ngũ bác sĩ của phòng khám';
        $('noiBacSi').classList.remove('d-none');
      }
    } catch (e) { /* ô số liệu chỉ là phụ: lỗi thì để ẩn */ }
  }

  /* ---------- Chuyên khoa ---------- */
  async function taiKhoa() {
    const el = $('dsKhoa');
    try {
      const ds = await Api.get('cong-khai/khoa');
      doKhoaVaoSelect($('tcKhoa'), ds);
      el.innerHTML = ds.length ? ds.map((k, i) => `<div class="col-sm-6 col-lg-3">
        <a href="chuyen-khoa.html?ma=${encodeURIComponent(k.maKhoa)}" class="text-decoration-none"><div class="card-kmn khoa-card hover-lift">
          <img src="${anhKhoa(k.maKhoa, i)}" class="khoa-img" alt="" loading="lazy"><div class="card-body"><h3>${esc(k.tenKhoa)}</h3>
          ${k.moTa ? `<p>${esc(k.moTa)}</p>` : '<p class="fst-italic">Chưa có mô tả.</p>'}
          <span class="meta"><i class="bi bi-person-badge me-1"></i>${k.soBacSi} bác sĩ</span></div></div></a></div>`).join('')
        : '<div class="col-12"><div class="empty-state"><i class="bi bi-diagram-3"></i>Chưa có chuyên khoa.</div></div>';
    } catch (e) {
      el.innerHTML = hopLoi(e, true);
      el.querySelector('[data-thu-lai]').addEventListener('click', taiKhoa);
    }
  }

  /* ---------- Bác sĩ ---------- */
  const theBacSi = b => `<div class="col-md-6 col-lg-3"><div class="card-kmn doctor-card hover-lift d-flex flex-column">
      <div class="doctor-head"><img class="avatar avatar-lg" alt="Ảnh ${esc(b.hoTen)}" data-avatar="${esc(b.duongDanAnh || '')}">
        <div><h3>${esc(b.hoTen)}</h3><span class="khoa-tag">${esc(b.tenKhoa)}</span></div></div>
      <hr><div class="small text-muted-kmn mb-2">Lịch gần nhất</div>
      ${dsSlot(b.lichSapToi.slice(0, 3), false)}
      <div class="mt-auto pt-3">${b.lichSapToi.length
        ? `<a href="${linkDatLich({ maNv: b.maNv })}" class="btn btn-primary btn-sm w-100">Đặt lịch</a>`
        : `<a href="bac-si.html?q=${encodeURIComponent(b.hoTen)}" class="btn btn-soft btn-sm w-100">Xem thông tin</a>`}</div></div></div>`;

  async function taiBacSi() {
    const el = $('dsBacSi');
    try {
      let ds;
      const noiBat = (SITE.featuredDoctorIds || []).slice(0, 4);
      if (noiBat.length) {
        $('tieuDeBacSi').textContent = 'Bác sĩ của phòng khám';
        // bác sĩ nổi bật không còn (đã nghỉ, sai mã) thì bỏ qua
        ds = (await Promise.all(noiBat.map(ma => Api.get('cong-khai/bac-si/' + encodeURIComponent(ma)).catch(() => null)))).filter(Boolean);
      } else {
        ds = (await Api.get('cong-khai/bac-si?pageSize=4')).items;
      }
      el.innerHTML = ds.length ? ds.map(theBacSi).join('')
        : '<div class="col-12"><div class="empty-state"><i class="bi bi-person-badge"></i>Chưa có thông tin bác sĩ.</div></div>';
      veAnh(el);
    } catch (e) {
      el.innerHTML = hopLoi(e, true);
      el.querySelector('[data-thu-lai]').addEventListener('click', taiBacSi);
    }
  }

  /* ---------- Lịch khám 7 ngày tới: hàng = ca, cột = ngày ---------- */
  async function taiLich() {
    const bang = $('bangLich');
    const tu = homNay();
    const ngay = Array.from({ length: SO_NGAY }, (_, i) => congNgay(tu, i));
    try {
      const ds = await Api.get(`cong-khai/lich-kham?tuNgay=${tu}&soNgay=${SO_NGAY}`);
      if (!ds.length) {
        bang.innerHTML = '<tbody><tr><td><div class="empty-state"><i class="bi bi-calendar-x"></i>Chưa có lịch khám còn đặt được trong 7 ngày tới.<br>Vui lòng gọi hotline '
          + esc(SITE.hotline) + ' để được hỗ trợ.</div></td></tr></tbody>';
        return;
      }
      const cacCa = [...new Map(ds.map(l => [l.maCa, l])).values()].sort((a, b) => a.gioBatDau.localeCompare(b.gioBatDau));
      const o = (ca, d) => ds.filter(l => l.maCa === ca && l.ngay === d).map(l => `<a class="week-chip text-decoration-none"
          href="${linkDatLich({ maNv: l.maNv, maLich: l.maLichLamViec })}" title="Đặt lịch ca này">${esc(l.hoTen)}<small>${esc(l.tenKhoa)} · ${esc(l.tenPhong)}</small></a>`).join('');
      bang.innerHTML = `<thead><tr><th>Ca</th>${ngay.map(d => `<th>${d === tu ? 'Hôm nay' : ngayNgan(d)}</th>`).join('')}</tr></thead>
        <tbody>${cacCa.map(c => `<tr><th>${esc(c.tenCa)}<br><small class="fw-normal text-muted-kmn">${gio(c)}</small></th>
          ${ngay.map(d => `<td>${o(c.maCa, d)}</td>`).join('')}</tr>`).join('')}</tbody>`;
    } catch (e) {
      bang.innerHTML = `<tbody><tr><td>${hopLoi(e, false)}</td></tr></tbody>`;
    }
  }

  document.addEventListener('DOMContentLoaded', () => {
    veThongTinTinh();
    taiTongQuan();
    taiKhoa();
    taiBacSi();
    taiLich();
  });
})();
