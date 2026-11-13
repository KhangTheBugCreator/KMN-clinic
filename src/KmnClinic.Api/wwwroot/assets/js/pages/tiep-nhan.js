/* Tiếp nhận khách vãng lai (M6): chọn hoặc tạo khách, chọn bác sĩ đang trực, nhập triệu chứng → hồ sơ khám. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const esc = Ui.esc;
  let khach = null, maBacSi = null, ketQua = [];

  /* ---------- 1. Khách hàng ---------- */
  async function timKhach() {
    const q = $('oTimKhach').value.trim();
    if (q.length < 2) { $('ketQuaKhach').innerHTML = ''; return; }
    try {
      ketQua = (await Api.get('khach-hang?pageSize=6&q=' + encodeURIComponent(q))).items;
      $('ketQuaKhach').innerHTML = ketQua.length ? ketQua.map(k => `<button type="button" class="queue-item w-100 text-start border-0" data-chon-khach="${esc(k.maKh)}">
          <span class="icon-circle ic-blue" style="width:36px;height:36px"><i class="bi bi-person"></i></span>
          <span class="flex-grow-1"><b class="text-navy">${esc(k.hoTen)}</b><br><small class="text-muted-kmn">${esc(k.soDienThoai)} · ${esc(k.gioiTinh)} · ${Ui.fmtNgay(k.ngaySinh)}</small></span>
          <span class="small text-primary">Chọn</span></button>`).join('')
        : `<div class="small text-muted-kmn p-2"><i class="bi bi-person-x me-1"></i>Không tìm thấy khách. Bấm <b>Khách mới</b> để tạo.</div>`;
    } catch (e) { $('ketQuaKhach').innerHTML = `<div class="small text-danger p-2">${esc(e.message)}</div>`; }
  }

  async function chonKhach(k) {
    khach = k;
    $('khungTim').classList.add('d-none');
    $('khachDaChon').classList.remove('d-none');
    $('khachDaChon').innerHTML = `<div class="queue-item mb-0"><span class="icon-circle ic-green" style="width:36px;height:36px"><i class="bi bi-person-check"></i></span>
      <div class="flex-grow-1"><b class="text-navy">${esc(k.hoTen)}</b> <span class="mono small">${esc(k.maKh)}</span><br>
        <small class="text-muted-kmn">${esc(k.soDienThoai)} · ${esc(k.gioiTinh)} · ${Ui.fmtNgay(k.ngaySinh)}</small></div>
      <button type="button" class="btn btn-sm btn-soft" id="btnDoiKhach">Đổi</button></div>`;
    // Khách có lịch hẹn hôm nay thì nên tiếp nhận theo lịch hẹn
    $('canhBaoHen').classList.add('d-none');
    try {
      const kq = await Api.get(`le-tan/lich-hen?ngay=${LeTan.homNay()}&q=${encodeURIComponent(k.soDienThoai)}&pageSize=10`);
      const conHieuLuc = kq.items.filter(h => h.maKh === k.maKh && (h.trangThai === 'Đã đặt' || h.trangThai === 'Đã xác nhận'));
      if (conHieuLuc.length && khach === k) {
        $('canhBaoHen').innerHTML = `<div class="alert alert-warning small border-0 rounded-3 mt-2 mb-0"><i class="bi bi-calendar-check me-1"></i>
          Khách có lịch hẹn hôm nay: ${conHieuLuc.map(h => `<b>${esc(h.maLichHen)}</b> (${esc(h.tenBacSi)}, ${esc(h.tenCa)})`).join(', ')}.
          <a href="lich-hen.html?ngay=${LeTan.homNay()}">Tiếp nhận theo lịch hẹn</a> thay vì vãng lai.</div>`;
        $('canhBaoHen').classList.remove('d-none');
      }
    } catch (e) { /* chỉ là gợi ý */ }
  }

  function boChonKhach() {
    khach = null;
    $('khachDaChon').classList.add('d-none');
    $('canhBaoHen').classList.add('d-none');
    $('khungTim').classList.remove('d-none');
    $('oTimKhach').focus();
  }

  function moKhachMoi() {
    const q = $('oTimKhach').value.trim();
    DanhSach.moForm({
      tieuDe: 'Khách hàng mới', nutLuu: 'Lưu và chọn',
      noiDung: `<div class="row g-3">
        <div class="col-sm-6"><label class="form-label" for="soDienThoai">Số điện thoại</label><input id="soDienThoai" name="soDienThoai" class="form-control" inputmode="numeric" maxlength="10" placeholder="0xxxxxxxxx" required></div>
        <div class="col-sm-6"><label class="form-label" for="hoTen">Họ tên</label><input id="hoTen" name="hoTen" class="form-control" maxlength="50" required></div>
        <div class="col-sm-6"><label class="form-label" for="ngaySinh">Ngày sinh</label><input id="ngaySinh" name="ngaySinh" type="date" class="form-control" required></div>
        <div class="col-sm-6"><span class="form-label d-block">Giới tính</span>
          <div class="form-check form-check-inline"><input class="form-check-input" type="radio" name="gioiTinh" id="gtNam" value="Nam"><label class="form-check-label" for="gtNam">Nam</label></div>
          <div class="form-check form-check-inline"><input class="form-check-input" type="radio" name="gioiTinh" id="gtNu" value="Nữ"><label class="form-check-label" for="gtNu">Nữ</label></div></div>
        <div class="col-12"><label class="form-label" for="diaChi">Địa chỉ <small class="text-muted-kmn">(không bắt buộc)</small></label><input id="diaChi" name="diaChi" class="form-control" maxlength="100"></div>
      </div>`,
      khiMo: f => {
        f.ngaySinh.max = LeTan.homNay();
        if (/^\d+$/.test(q)) f.soDienThoai.value = q; else if (q) f.hoTen.value = q;
      },
      thuThap: f => {
        const body = { hoTen: f.hoTen.value.trim(), soDienThoai: f.soDienThoai.value.trim(), ngaySinh: f.ngaySinh.value || null,
          gioiTinh: f.gioiTinh.value, diaChi: f.diaChi.value.trim() || null };
        if (!/^0\d{9}$/.test(body.soDienThoai)) return 'Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.';
        if (!body.hoTen) return 'Vui lòng nhập họ tên.';
        if (!body.ngaySinh) return 'Vui lòng nhập ngày sinh.';
        if (!body.gioiTinh) return 'Vui lòng chọn giới tính.';
        return body;
      },
      gui: async body => {
        const k = await Api.post('khach-hang', body);
        Ui.toast(`Đã thêm khách hàng ${k.maKh}.`);
        chonKhach(k);
      }
    });
  }

  /* ---------- 2. Bác sĩ đang trực ---------- */
  async function taiBacSi() {
    $('dsBacSi').innerHTML = '<div class="col-12"><div class="skeleton" style="height:64px"></div></div>';
    try {
      const ds = await Api.get('le-tan/bac-si-dang-truc');
      if (!ds.some(b => b.maNv === maBacSi)) maBacSi = null;
      $('dsBacSi').innerHTML = ds.length ? ds.map(b => `<div class="col-md-6"><div class="pick-card ${b.maNv === maBacSi ? 'selected' : ''}" data-bac-si="${esc(b.maNv)}" role="button" tabindex="0">
          <div class="d-flex gap-2 align-items-center"><img class="avatar" alt="" data-avatar="${esc(b.duongDanAnh || '')}" style="width:40px;height:40px">
          <div class="flex-grow-1"><b class="text-navy">${esc(b.hoTen)}</b><br><small class="text-muted-kmn">${esc(b.tenKhoa)} · ${esc(b.tenPhong)}</small></div>
          ${b.maNv === maBacSi ? '<i class="bi bi-check-circle-fill text-primary"></i>' : ''}</div>
          <div class="small text-muted-kmn mt-1">${esc(b.tenCa)} ${LeTan.gio(b)} · ${b.soChoKham} đang chờ khám</div></div></div>`).join('')
        : '<div class="col-12"><div class="empty-state"><i class="bi bi-moon"></i>Hiện không có bác sĩ nào đang trực. Khách vãng lai chỉ được nhận từ 30 phút trước giờ bắt đầu ca.</div></div>';
      LeTan.veAnh($('dsBacSi'));
    } catch (e) { $('dsBacSi').innerHTML = `<div class="col-12 small text-danger">${esc(e.message)}</div>`; }
  }

  /* ---------- Hồ sơ hôm nay ---------- */
  async function taiHoSo() {
    try {
      const ds = await Api.get('le-tan/ho-so');
      $('demHoSo').textContent = `${ds.length} hồ sơ`;
      $('dsHoSo').innerHTML = ds.length ? ds.map(h => `<div class="queue-item">
          <span class="icon-circle ${h.maLichHen ? 'ic-blue' : 'ic-orange'}" style="width:36px;height:36px"><i class="bi bi-${h.maLichHen ? 'calendar-check' : 'person-walking'}"></i></span>
          <div class="flex-grow-1"><b class="text-navy">${esc(h.tenKhach)}</b> <span class="mono small">${esc(h.maHoSo)}</span><br>
            <small class="text-muted-kmn">${Ui.fmtNgayGio(h.ngayGioKham).slice(-5)} · ${esc(h.tenBacSi)} · ${h.maLichHen ? 'Có hẹn ' + esc(h.maLichHen) : 'Vãng lai'}</small></div>
          <span class="badge-st ${h.choKham ? 'st-xacnhan' : 'st-kham'}">${h.choKham ? 'Chờ khám' : 'Đã khám xong'}</span></div>`).join('')
        : '<div class="empty-state"><i class="bi bi-journal"></i>Chưa tiếp nhận khách nào hôm nay.</div>';
    } catch (e) { $('dsHoSo').innerHTML = `<div class="small text-danger">${esc(e.message)}</div>`; }
  }

  /* ---------- Gửi ---------- */
  async function tao(e) {
    e.preventDefault();
    const loi = msg => { $('loiForm').textContent = msg; $('loiForm').classList.remove('d-none'); };
    $('loiForm').classList.add('d-none');
    const trieuChung = $('trieuChung').value.trim();
    if (!khach) return loi('Vui lòng chọn hoặc tạo khách hàng.');
    if (!maBacSi) return loi('Vui lòng chọn bác sĩ đang trực.');
    if (!trieuChung) return loi('Vui lòng nhập triệu chứng.');
    try {
      const hs = await Ui.dangXuLy($('btnTao'), () => Api.post('le-tan/tiep-nhan-vang-lai', { maKh: khach.maKh, maNvKham: maBacSi, trieuChung }));
      Ui.toast(`Đã tạo hồ sơ ${hs.maHoSo} cho ${hs.tenKhach}, chuyển tới ${hs.tenBacSi}.`);
      $('trieuChung').value = '';
      $('oTimKhach').value = '';
      $('ketQuaKhach').innerHTML = '';
      boChonKhach();
      maBacSi = null;
      taiBacSi();
      taiHoSo();
    } catch (err) {
      loi(err.message);
      if (err.status === 400) taiBacSi(); // ca có thể vừa kết thúc
    }
  }

  $('oTimKhach').addEventListener('input', Ui.treo(timKhach));
  $('ketQuaKhach').addEventListener('click', e => {
    const b = e.target.closest('[data-chon-khach]');
    if (b) chonKhach(ketQua.find(k => k.maKh === b.dataset.chonKhach));
  });
  $('khachDaChon').addEventListener('click', e => { if (e.target.closest('#btnDoiKhach')) boChonKhach(); });
  $('btnKhachMoi').addEventListener('click', moKhachMoi);
  const chonBs = e => {
    const c = e.target.closest('[data-bac-si]');
    if (!c || (e.type === 'keydown' && e.key !== 'Enter' && e.key !== ' ')) return;
    e.preventDefault();
    maBacSi = c.dataset.bacSi;
    $('dsBacSi').querySelectorAll('[data-bac-si]').forEach(x => {
      const sel = x.dataset.bacSi === maBacSi;
      x.classList.toggle('selected', sel);
      x.querySelector('.bi-check-circle-fill')?.remove();
      if (sel) x.querySelector('.d-flex').insertAdjacentHTML('beforeend', '<i class="bi bi-check-circle-fill text-primary"></i>');
    });
  };
  $('dsBacSi').addEventListener('click', chonBs);
  $('dsBacSi').addEventListener('keydown', chonBs);
  $('btnTaiBacSi').addEventListener('click', taiBacSi);
  $('formVangLai').addEventListener('submit', tao);
  taiBacSi();
  taiHoSo();
})();
