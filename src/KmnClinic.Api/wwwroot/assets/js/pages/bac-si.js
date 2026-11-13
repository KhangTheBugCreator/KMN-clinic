/* Tra cứu bác sĩ (M4): lọc theo tên, khoa, ngày; xem lịch còn đặt được 14 ngày tới. Bộ lọc nằm trên URL để chia sẻ được. */
(function () {
  const { esc, homNay, congNgay, ngayDai, gio, linkDatLich, dsSlot, veAnh, hopLoi, doKhoaVaoSelect } = CongKhai;
  const $ = id => document.getElementById(id);
  const PAGE_SIZE = 10;
  let dsKhoa = [], modal;

  const boLoc = () => ({ q: $('locTen').value.trim(), maKhoa: $('locKhoa').value, ngay: $('locNgay').value });

  function theBacSi(b) {
    const coLich = b.lichSapToi.length > 0;
    return `<div class="col-lg-6"><div class="card-kmn doctor-card"><div class="row g-3">
      <div class="col-sm-auto text-center"><img class="avatar avatar-xl" alt="Ảnh ${esc(b.hoTen)}" data-avatar="${esc(b.duongDanAnh || '')}"></div>
      <div class="col">
        <h3 class="h5">${esc(b.hoTen)}</h3>
        <span class="khoa-tag">${esc(b.tenKhoa)}</span>
        ${b.moTaKhoa ? `<p class="small text-muted-kmn mt-2 mb-2">${esc(b.moTaKhoa)}</p>` : '<div class="mb-2"></div>'}
        <div class="small text-muted-kmn mb-1">${boLoc().ngay ? 'Lịch trong ngày ' + Ui.fmtNgay(boLoc().ngay) : 'Lịch còn đặt được 7 ngày tới'}</div>
        <div class="mb-3">${dsSlot(b.lichSapToi, true)}</div>
        <div class="d-flex gap-2"><button type="button" class="btn btn-soft btn-sm" data-xem-lich="${esc(b.maNv)}"><i class="bi bi-calendar3 me-1"></i>Xem lịch 14 ngày</button>
          ${coLich ? `<a href="${linkDatLich({ maNv: b.maNv })}" class="btn btn-primary btn-sm"><i class="bi bi-calendar2-plus me-1"></i>Đặt lịch</a>`
            : '<button class="btn btn-primary btn-sm" disabled title="Bác sĩ chưa có lịch còn đặt được">Đặt lịch</button>'}</div>
      </div></div></div></div>`;
  }

  async function tai(trang = 1) {
    const l = boLoc();
    const p = new URLSearchParams();
    Object.entries(l).forEach(([k, v]) => { if (v) p.set(k, v); });
    if (trang > 1) p.set('page', trang);
    history.replaceState(null, '', p.toString() ? '?' + p : location.pathname);
    $('xoaLoc').classList.toggle('d-none', !(l.q || l.maKhoa || l.ngay));
    $('dsBacSi').innerHTML = '<div class="col-12"><div class="skeleton" style="height:200px"></div></div>';
    try {
      p.set('pageSize', PAGE_SIZE);
      const kq = await Api.get('cong-khai/bac-si?' + p);
      const tenKhoa = dsKhoa.find(k => k.maKhoa === l.maKhoa)?.tenKhoa;
      $('tomTat').innerHTML = `Tìm thấy <b class="text-navy">${kq.total} bác sĩ</b>`
        + (tenKhoa ? ` · ${esc(tenKhoa)}` : '') + (l.ngay ? ` · có lịch ngày ${Ui.fmtNgay(l.ngay)}` : '');
      $('dsBacSi').innerHTML = kq.items.length ? kq.items.map(theBacSi).join('')
        : '<div class="col-12"><div class="empty-state"><i class="bi bi-person-x"></i>Không tìm thấy bác sĩ phù hợp. Hãy thử bỏ bớt bộ lọc.</div></div>';
      veAnh($('dsBacSi'));
      if (kq.total > PAGE_SIZE) Ui.phanTrang($('phanTrang'), kq, t => { tai(t); window.scrollTo({ top: 0, behavior: 'smooth' }); });
      else $('phanTrang').innerHTML = '';
    } catch (e) {
      $('tomTat').textContent = '';
      $('phanTrang').innerHTML = '';
      $('dsBacSi').innerHTML = hopLoi(e, true);
      $('dsBacSi').querySelector('[data-thu-lai]').addEventListener('click', () => tai(trang));
    }
  }

  async function xemLich(ma) {
    $('lichTieuDe').textContent = 'Lịch làm việc';
    $('lichBang').innerHTML = Ui.dongDangTai(5);
    modal.show();
    try {
      const b = await Api.get('cong-khai/bac-si/' + encodeURIComponent(ma));
      $('lichTieuDe').textContent = `Lịch làm việc · ${b.hoTen}`;
      $('lichBang').innerHTML = b.lichSapToi.length ? b.lichSapToi.map(l => `<tr><td>${ngayDai(l.ngay)}</td><td>${esc(l.tenCa)}</td>
          <td>${gio(l)}</td><td>${esc(l.tenPhong)}</td>
          <td class="text-end"><a class="btn btn-primary btn-sm" href="${linkDatLich({ maNv: l.maNv, maLich: l.maLichLamViec })}">Chọn</a></td></tr>`).join('')
        : Ui.dongTrong(5, 'Bác sĩ chưa có lịch còn đặt được trong 14 ngày tới.', 'calendar-x');
    } catch (e) {
      $('lichBang').innerHTML = Ui.dongTrong(5, e.message, 'wifi-off');
    }
  }

  document.addEventListener('DOMContentLoaded', async () => {
    modal = new bootstrap.Modal($('lichModal'));
    $('locNgay').min = homNay();
    $('locNgay').max = congNgay(homNay(), 13);
    // Bộ lọc từ URL (ví dụ đến từ ô tra cứu nhanh ở trang chủ)
    $('locTen').value = Ui.thamSo('q') || '';
    const ngay = Ui.thamSo('ngay') || '';
    if (/^\d{4}-\d{2}-\d{2}$/.test(ngay) && ngay >= $('locNgay').min && ngay <= $('locNgay').max) $('locNgay').value = ngay;
    try {
      dsKhoa = await Api.get('cong-khai/khoa');
      doKhoaVaoSelect($('locKhoa'), dsKhoa, Ui.thamSo('maKhoa'));
    } catch (e) { /* vẫn tra cứu được theo tên, ngày */ }

    $('formLoc').addEventListener('submit', e => { e.preventDefault(); tai(1); });
    $('locKhoa').addEventListener('change', () => tai(1));
    $('locNgay').addEventListener('change', () => tai(1));
    $('xoaLoc').addEventListener('click', () => { $('formLoc').reset(); tai(1); });
    $('dsBacSi').addEventListener('click', e => {
      const b = e.target.closest('[data-xem-lich]');
      if (b) xemLich(b.dataset.xemLich);
    });

    tai(Math.max(1, parseInt(Ui.thamSo('page'), 10) || 1));
    if (Ui.thamSo('ma')) xemLich(Ui.thamSo('ma'));
  });
})();
