/* Lịch hẹn của tôi (M5): khách xem lịch hẹn của chính mình và tự hủy (Đã đặt / Đã xác nhận, trước giờ bắt đầu ca). */
(function () {
  const { esc, ngayDai } = CongKhai;
  const $ = id => document.getElementById(id);
  let ds = [], nhom = 'sap-toi';

  /** Sắp tới = còn hiệu lực (Đã đặt, Đã xác nhận) và chưa tới giờ bắt đầu ca. */
  const laSapToi = h => h.coTheHuy;

  function the(h) {
    const gioKham = `${Ui.fmtGio(h.gioBatDau)}–${Ui.fmtGio(h.gioKetThuc)}`;
    const ghiChu = {
      'Đã đặt': 'Đang chờ lễ tân gọi điện xác nhận.',
      'Đã xác nhận': 'Lễ tân đã xác nhận. Vui lòng đến trước giờ bắt đầu ca và báo tên tại quầy.',
      'Đã khám': 'Đã được tiếp nhận khám.',
      'Đã hủy': 'Lịch hẹn đã hủy.'
    }[h.trangThai] || '';
    const quaGio = !h.coTheHuy && (h.trangThai === 'Đã đặt' || h.trangThai === 'Đã xác nhận');
    return `<div class="col-lg-6"><div class="card-kmn p-3 h-100">
      <div class="d-flex justify-content-between align-items-start gap-2 mb-2">
        <div><span class="small text-muted-kmn">Mã lịch hẹn</span><br><b class="text-navy">${esc(h.maLichHen)}</b></div>
        ${Ui.badgeTrangThai(h.trangThai)}</div>
      <div class="d-flex gap-3 align-items-center mb-2"><img class="avatar" alt="" data-avatar="${esc(h.duongDanAnh || '')}">
        <div><b class="text-navy">${esc(h.tenBacSi)}</b><br><span class="small text-muted-kmn">${esc(h.tenKhoa)}</span></div></div>
      <div class="small"><i class="bi bi-calendar3 me-1 text-primary"></i>${ngayDai(h.ngayKham)}
        <span class="mx-1">·</span>${esc(h.tenCa)} ${gioKham}<span class="mx-1">·</span>${esc(h.tenPhong)}</div>
      ${h.trieuChungBanDau ? `<div class="small text-muted-kmn mt-1"><i class="bi bi-chat-left-text me-1"></i>${esc(h.trieuChungBanDau)}</div>` : ''}
      <div class="small text-muted-kmn mt-2">${quaGio ? 'Ca khám đã bắt đầu hoặc đã qua.' : ghiChu} Đặt lúc ${Ui.fmtNgayGio(h.ngayGioDat)}.</div>
      ${h.coTheHuy ? `<div class="text-end mt-2"><button type="button" class="btn btn-outline-danger btn-sm" data-huy="${esc(h.maLichHen)}"><i class="bi bi-x-circle me-1"></i>Hủy lịch hẹn</button></div>` : ''}
    </div></div>`;
  }

  function ve() {
    const sapToi = ds.filter(laSapToi), daQua = ds.filter(h => !laSapToi(h));
    // Sắp tới: gần nhất lên đầu; đã qua: mới nhất lên đầu (API đã sắp giảm dần)
    sapToi.reverse();
    $('demSapToi').textContent = sapToi.length;
    $('demDaQua').textContent = daQua.length;
    const hien = nhom === 'sap-toi' ? sapToi : daQua;
    $('dsLichHen').innerHTML = hien.length ? hien.map(the).join('')
      : `<div class="col-12"><div class="empty-state"><i class="bi bi-calendar-x"></i>${nhom === 'sap-toi'
        ? 'Bạn chưa có lịch hẹn sắp tới.<div class="mt-2"><a href="dat-lich.html" class="btn btn-primary btn-sm">Đặt lịch khám</a></div>'
        : 'Chưa có lịch hẹn nào ở mục này.'}</div></div>`;
    CongKhai.veAnh($('dsLichHen'));
  }

  async function tai() {
    try {
      ds = await Api.get('lich-hen-cua-toi');
      ve();
    } catch (e) {
      $('dsLichHen').innerHTML = CongKhai.hopLoi(e, true);
      $('dsLichHen').querySelector('[data-thu-lai]')?.addEventListener('click', tai);
    }
  }

  async function huy(btn) {
    const h = ds.find(x => x.maLichHen === btn.dataset.huy);
    const dongY = await Ui.xacNhan({
      tieuDe: 'Hủy lịch hẹn?', kieu: 'danger', icon: 'x-circle', nutDongY: 'Hủy lịch hẹn',
      noiDung: `${h.tenBacSi} · ${ngayDai(h.ngayKham)} · ${h.tenCa}. Sau khi hủy không khôi phục được, muốn khám bạn cần đặt lịch mới.`
    });
    if (!dongY) return;
    try {
      await Ui.dangXuLy(btn, () => Api.post(`lich-hen-cua-toi/${encodeURIComponent(h.maLichHen)}/huy`));
      Ui.toast('Đã hủy lịch hẹn ' + h.maLichHen + '.');
    } catch (e) { Ui.loi(e); }
    tai();
  }

  document.addEventListener('DOMContentLoaded', () => {
    const nd = Auth.nguoiDung();
    if (!nd || nd.vaiTro !== 'KhachHang') {
      $('linkChonVaiTro').href = 'chon-vai-tro.html?vaiTro=KhachHang&quayLai=' + encodeURIComponent(location.pathname);
      if (nd) $('canVaiTroMoTa').textContent = `Bạn đang giả lập vai trò ${nd.chucVu || 'nhân viên'} (${nd.hoTen}). Trang này dành cho khách hàng.`;
      $('canVaiTro').classList.remove('d-none');
      return;
    }
    $('loiChao').innerHTML = `Lịch hẹn của <b class="text-navy">${esc(nd.hoTen)}</b>. Bạn có thể tự hủy khi lịch còn ở trạng thái Đã đặt hoặc Đã xác nhận và trước giờ bắt đầu ca.`;
    $('khungLichHen').classList.remove('d-none');
    $('nutDatMoi').classList.remove('d-none');
    document.querySelectorAll('[data-nhom]').forEach(b => b.addEventListener('click', () => {
      nhom = b.dataset.nhom;
      document.querySelectorAll('[data-nhom]').forEach(x => x.classList.toggle('active', x === b));
      ve();
    }));
    $('dsLichHen').addEventListener('click', e => { const b = e.target.closest('[data-huy]'); if (b) huy(b); });
    tai();
  });
})();
