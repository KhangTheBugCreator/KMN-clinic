/* Chuyên khoa (M4): danh sách khoa, chọn khoa để xem bác sĩ và lịch còn đặt được 7 ngày tới. */
(function () {
  const { esc, anhKhoa, linkDatLich, dsSlot, veAnh, hopLoi } = CongKhai;
  const $ = id => document.getElementById(id);
  let dsKhoa = [], dangChon = null;

  /** So khớp không dấu, không phân biệt hoa thường. */
  const chuan = s => (s || '').normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/đ/gi, 'd').toLowerCase().trim();

  function veKhoa() {
    const tim = chuan($('timKhoa').value);
    const ds = dsKhoa.map((k, i) => ({ k, i })).filter(x => !tim || chuan(x.k.tenKhoa).includes(tim));
    $('dsKhoa').innerHTML = ds.length ? ds.map(({ k, i }) => {
      const chon = k.maKhoa === dangChon;
      return `<div class="col-sm-6 col-lg-3"><a href="?ma=${encodeURIComponent(k.maKhoa)}" data-khoa="${esc(k.maKhoa)}" class="text-decoration-none">
        <div class="card-kmn khoa-card hover-lift ${chon ? 'border-primary' : ''}" ${chon ? 'style="border-width:2px"' : ''}>
        <img src="${anhKhoa(k.maKhoa, i)}" class="khoa-img" alt="" loading="lazy"><div class="card-body"><h3>${esc(k.tenKhoa)}</h3>
        ${k.moTa ? `<p>${esc(k.moTa)}</p>` : '<p class="fst-italic">Chưa có mô tả.</p>'}
        <div class="d-flex justify-content-between align-items-center"><span class="meta"><i class="bi bi-person-badge me-1"></i>${k.soBacSi} bác sĩ</span>
        ${chon ? '<span class="badge-st st-xacnhan">Đang xem</span>' : '<span class="small fw-semibold text-primary">Xem bác sĩ <i class="bi bi-arrow-right"></i></span>'}</div></div></div></a></div>`;
    }).join('') : `<div class="col-12"><div class="empty-state"><i class="bi bi-search"></i>${dsKhoa.length ? 'Không có khoa nào khớp từ khóa.' : 'Chưa có chuyên khoa.'}</div></div>`;
  }

  async function chonKhoa(ma, cuon) {
    const k = dsKhoa.find(x => x.maKhoa === ma);
    if (!k) { $('chiTietKhoa').classList.add('d-none'); return; }
    dangChon = ma;
    veKhoa();
    history.replaceState(null, '', '?ma=' + encodeURIComponent(ma));
    $('ctTen').textContent = k.tenKhoa;
    $('ctPhu').textContent = `${k.soBacSi} bác sĩ` + (k.cacPhong.length ? ' · ' + k.cacPhong.join(', ') : '');
    $('ctDatLich').href = linkDatLich({ maKhoa: ma });
    $('ctBacSi').innerHTML = Ui.dongDangTai(3);
    $('chiTietKhoa').classList.remove('d-none');
    if (cuon) $('chiTietKhoa').scrollIntoView({ behavior: 'smooth', block: 'start' });
    try {
      const kq = await Api.get(`cong-khai/bac-si?maKhoa=${encodeURIComponent(ma)}&pageSize=24`);
      if (dangChon !== ma) return; // người dùng đã chọn khoa khác
      $('ctBacSi').innerHTML = kq.items.length ? kq.items.map(b => `<tr>
          <td><div class="d-flex align-items-center gap-2"><img class="avatar" alt="" data-avatar="${esc(b.duongDanAnh || '')}"><b class="text-navy">${esc(b.hoTen)}</b></div></td>
          <td>${dsSlot(b.lichSapToi, false, 'Chưa có lịch trong 7 ngày tới')}</td>
          <td class="text-end text-nowrap"><a href="bac-si.html?ma=${encodeURIComponent(b.maNv)}" class="btn btn-soft btn-sm">Xem lịch</a>
            ${b.lichSapToi.length ? `<a href="${linkDatLich({ maNv: b.maNv })}" class="btn btn-primary btn-sm">Đặt lịch</a>` : ''}</td></tr>`).join('')
        : Ui.dongTrong(3, 'Khoa chưa có bác sĩ.', 'person-badge');
      veAnh($('ctBacSi'));
    } catch (e) {
      $('ctBacSi').innerHTML = Ui.dongTrong(3, e.message, 'wifi-off');
    }
  }

  async function tai() {
    try {
      dsKhoa = await Api.get('cong-khai/khoa');
    } catch (e) {
      $('dsKhoa').innerHTML = hopLoi(e, true);
      $('dsKhoa').querySelector('[data-thu-lai]').addEventListener('click', tai);
      return;
    }
    veKhoa();
    const ma = Ui.thamSo('ma');
    if (ma) chonKhoa(ma, true);
  }

  document.addEventListener('DOMContentLoaded', () => {
    $('timKhoa').addEventListener('input', Ui.treo(veKhoa, 150));
    $('dsKhoa').addEventListener('click', e => {
      const a = e.target.closest('[data-khoa]');
      if (!a) return;
      e.preventDefault();
      chonKhoa(a.dataset.khoa, true);
    });
    tai();
  });
})();
