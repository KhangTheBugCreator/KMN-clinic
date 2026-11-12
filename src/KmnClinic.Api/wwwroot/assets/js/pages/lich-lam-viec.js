/* Lịch làm việc (M3): quản trị viên xếp lịch; lễ tân xem toàn bộ; bác sĩ chỉ xem lịch của mình (API tự lọc). */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const nd = Auth.nguoiDung();
  const laQuanTri = nd?.vaiTro === 'QuanTriVien';
  const laBacSi = nd?.vaiTro === 'BacSi';
  const THU = ['Chủ nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7'];
  const THU_NGAN = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];

  /* ---------- Ngày tháng (theo giờ máy, dạng YYYY-MM-DD) ---------- */
  const pad = n => String(n).padStart(2, '0');
  const iso = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  const doc = s => { const [y, m, d] = s.split('-').map(Number); return new Date(y, m - 1, d); };
  const cong = (d, n) => { const x = new Date(d); x.setDate(x.getDate() + n); return x; };
  const dauTuan = d => cong(d, -((d.getDay() + 6) % 7));
  const homNay = iso(new Date());
  const ngayNgan = d => `${pad(d.getDate())}/${pad(d.getMonth() + 1)}`;

  let kieu = 'tuan', moc = new Date();
  let dsCa = [], dsKhoa = [], dsPhong = [], dsBacSi = [], dsLich = [];

  if (laQuanTri) $('nutQuanTri').classList.remove('d-none');
  if (laBacSi) {
    $('boLoc').classList.add('d-none');
    $('tieuDe').textContent = $('tenTrang').textContent = 'Lịch làm việc của tôi';
    $('nhomMenu').textContent = 'Lịch';
    $('moTa').textContent = 'Các ca bạn được xếp trực, kèm phòng và số lịch hẹn của khách.';
  } else if (!laQuanTri) {
    $('tieuDe').textContent = $('tenTrang').textContent = 'Lịch làm việc bác sĩ';
    $('nhomMenu').textContent = 'Tra cứu';
    $('moTa').textContent = 'Bác sĩ trực theo ngày, ca và phòng.';
  }

  /* ---------- Tải dữ liệu ---------- */
  function khoangHienTai() {
    if (kieu === 'ngay') return [moc, moc];
    if (kieu === 'tuan') { const t = dauTuan(moc); return [t, cong(t, 6)]; }
    const dau = new Date(moc.getFullYear(), moc.getMonth(), 1);
    const cuoi = new Date(moc.getFullYear(), moc.getMonth() + 1, 0);
    return [dauTuan(dau), cong(dauTuan(cuoi), 6)]; // đủ các tuần chứa tháng (tối đa 42 ngày)
  }

  async function tai() {
    const [tu, den] = khoangHienTai();
    $('khoang').textContent = kieu === 'thang' ? `Tháng ${moc.getMonth() + 1}/${moc.getFullYear()}`
      : kieu === 'ngay' ? `${THU[moc.getDay()]}, ${Ui.fmtNgay(iso(moc))}` : `${Ui.fmtNgay(iso(tu))} – ${Ui.fmtNgay(iso(den))}`;
    $('lich').innerHTML = '<div class="skeleton" style="height:320px"></div>';
    const ts = new URLSearchParams({ tuNgay: iso(tu), denNgay: iso(den) });
    if (!laBacSi) [['maKhoa', 'locKhoa'], ['maNv', 'locBacSi'], ['maPhong', 'locPhong']].forEach(([k, id]) => { if ($(id).value) ts.set(k, $(id).value); });
    try {
      dsLich = await Api.get('lich-lam-viec?' + ts);
      ({ ngay: veNgay, tuan: veTuan, thang: veThang })[kieu](tu, den);
    } catch (err) { $('lich').innerHTML = `<div class="empty-state"><i class="bi bi-exclamation-circle"></i>${Ui.esc(err.message)}</div>`; }
  }

  const chip = l => `<button type="button" class="chip-lich ${l.soLichHen ? 'co-hen' : ''}" data-ma="${Ui.esc(l.maLichLamViec)}"
      title="${Ui.esc(`${l.hoTenNv} · ${l.tenPhong} · ${l.tenKhoa}`)}">
      <b>${Ui.esc(laBacSi ? l.tenPhong : l.hoTenNv)}</b>${Ui.esc(laBacSi ? l.tenKhoa : l.tenPhong)}${l.soLichHen ? ` · <i class="bi bi-calendar-check"></i> ${l.soLichHenConHieuLuc}/${l.soLichHen}` : ''}</button>`;

  function veTuan(tu) {
    const ngays = [...Array(7)].map((_, i) => cong(tu, i));
    if (!dsCa.length) { $('lich').innerHTML = '<div class="empty-state"><i class="bi bi-clock"></i>Chưa có ca làm việc. Quản trị viên cần tạo ca trước.</div>'; return; }
    const head = ngays.map(d => `<th class="${iso(d) === homNay ? 'hom-nay' : ''}">${THU[d.getDay()]}<br><span class="fw-normal">${ngayNgan(d)}</span></th>`).join('');
    const rows = dsCa.map(ca => `<tr><th class="cot-ca">${Ui.esc(ca.tenCa)}<br><small class="text-muted-kmn fw-normal">${Ui.fmtGio(ca.gioBatDau)}–${Ui.fmtGio(ca.gioKetThuc)}</small></th>${ngays.map(d => {
      const n = iso(d), qua = n < homNay;
      const ls = dsLich.filter(l => l.ngayLamViec === n && l.maCa === ca.maCa);
      return `<td class="${qua ? 'da-qua' : ''}">${ls.map(chip).join('')}${laQuanTri && !qua
        ? `<button type="button" class="nut-them-o" data-them-ngay="${n}" data-them-ca="${Ui.esc(ca.maCa)}" aria-label="Thêm lịch ${THU[d.getDay()]} ${ngayNgan(d)}, ${Ui.esc(ca.tenCa)}"><i class="bi bi-plus"></i></button>` : ''}</td>`;
    }).join('')}</tr>`).join('');
    $('lich').innerHTML = `<table class="table lich-tuan mb-0"><thead><tr><th class="cot-ca">Ca</th>${head}</tr></thead><tbody>${rows}</tbody></table>
      ${dsLich.length ? '' : `<p class="text-muted-kmn small mt-2 mb-0"><i class="bi bi-info-circle me-1"></i>${laBacSi ? 'Bạn chưa được xếp lịch trong tuần này.' : 'Chưa có lịch làm việc nào trong tuần này.'}</p>`}`;
  }

  function veThang(tu, den) {
    const thang = moc.getMonth();
    let html = [1, 2, 3, 4, 5, 6, 0].map(t => `<div class="ten-thu">${THU_NGAN[t]}</div>`).join('');
    for (let d = new Date(tu); d <= den; d = cong(d, 1)) {
      const n = iso(d), ls = dsLich.filter(l => l.ngayLamViec === n);
      html += `<div class="o-ngay ${d.getMonth() !== thang ? 'ngoai-thang' : ''} ${n < homNay ? 'da-qua' : ''} ${n === homNay ? 'hom-nay' : ''}"
        data-ngay="${n}" role="button" tabindex="0" aria-label="${THU[d.getDay()]} ${Ui.fmtNgay(n)}: ${ls.length} lịch">
        <span class="so-ngay">${d.getDate()}</span>
        ${ls.slice(0, 3).map(l => `<div class="dong-ngan">${Ui.esc(l.tenCa)} · ${Ui.esc(laBacSi ? l.tenPhong : l.hoTenNv)}</div>`).join('')}
        ${ls.length > 3 ? `<div class="small text-primary mt-1">+${ls.length - 3} lịch</div>` : ''}</div>`;
    }
    $('lich').innerHTML = `<div class="lich-thang">${html}</div>`;
  }

  function veNgay() {
    const n = iso(moc), qua = n < homNay;
    const ls = dsLich.filter(l => l.ngayLamViec === n);
    $('lich').innerHTML = `<table class="table table-kmn mb-0">
      <thead><tr><th>Ca</th><th>Bác sĩ</th><th>Khoa</th><th>Phòng</th><th class="text-center">Lịch hẹn</th><th class="text-end">Chi tiết</th></tr></thead>
      <tbody>${ls.length ? ls.map(l => `<tr>
        <td><b class="text-navy">${Ui.esc(l.tenCa)}</b><br><small class="text-muted-kmn">${Ui.fmtGio(l.gioBatDau)}–${Ui.fmtGio(l.gioKetThuc)}</small></td>
        <td><div class="d-flex align-items-center gap-2"><img class="avatar" alt="" data-avatar="${Ui.esc(l.duongDanAnh || '')}" referrerpolicy="no-referrer" style="width:36px;height:36px">${Ui.esc(l.hoTenNv)}</div></td>
        <td>${Ui.esc(l.tenKhoa)}</td><td>${Ui.esc(l.tenPhong)}</td>
        <td class="text-center">${l.soLichHenConHieuLuc} / ${l.soLichHen}</td>
        <td class="text-end"><button class="btn btn-sm btn-soft" data-ma="${Ui.esc(l.maLichLamViec)}"><i class="bi bi-eye"></i> Xem</button></td></tr>`).join('')
        : Ui.dongTrong(6, laBacSi ? 'Bạn không có lịch trực ngày này.' : 'Chưa có lịch làm việc ngày này.', 'calendar-x')}</tbody></table>
      ${laQuanTri && !qua ? `<button class="btn btn-soft mt-3" data-them-ngay="${n}"><i class="bi bi-plus-lg me-1"></i>Thêm lịch ngày này</button>` : ''}`;
    $('lich').querySelectorAll('img[data-avatar]').forEach(img => renderAvatar(img, img.dataset.avatar));
  }

  /* ---------- Chi tiết một lịch ---------- */
  function moChiTiet(l) {
    const qua = l.ngayLamViec < homNay;
    const lyDo = qua ? 'Lịch đã qua nên không sửa, xóa được để giữ lịch sử.'
      : l.soLichHen ? `Lịch đã có ${l.soLichHen} lịch hẹn của khách nên không sửa, xóa được.` : '';
    const wrap = document.createElement('div');
    wrap.className = 'modal fade'; wrap.tabIndex = -1;
    wrap.innerHTML = `<div class="modal-dialog modal-dialog-centered"><div class="modal-content rounded-4 border-0">
      <div class="modal-header border-0"><h2 class="modal-title h5">Lịch ${Ui.esc(l.maLichLamViec)}</h2>
        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Đóng"></button></div>
      <div class="modal-body pt-0">
        <div class="d-flex align-items-center gap-3 mb-3"><img class="avatar" alt="" referrerpolicy="no-referrer" style="width:56px;height:56px">
          <div><b class="text-navy">${Ui.esc(l.hoTenNv)}</b><br><small class="text-muted-kmn">${Ui.esc(l.tenKhoa)} · ${Ui.esc(l.maNv)}</small></div></div>
        <dl class="row small mb-0">
          <dt class="col-4">Ngày</dt><dd class="col-8">${THU[doc(l.ngayLamViec).getDay()]}, ${Ui.fmtNgay(l.ngayLamViec)}</dd>
          <dt class="col-4">Ca</dt><dd class="col-8">${Ui.esc(l.tenCa)} (${Ui.fmtGio(l.gioBatDau)}–${Ui.fmtGio(l.gioKetThuc)})</dd>
          <dt class="col-4">Phòng</dt><dd class="col-8">${Ui.esc(l.tenPhong)}</dd>
          <dt class="col-4">Lịch hẹn</dt><dd class="col-8">${l.soLichHen} lịch, ${l.soLichHenConHieuLuc} đang chờ khám</dd>
        </dl>
        ${laQuanTri && lyDo ? `<div class="alert alert-secondary small mt-3 mb-0"><i class="bi bi-lock me-1"></i>${lyDo}</div>` : ''}
      </div>
      ${laQuanTri ? `<div class="modal-footer border-0">
        <button type="button" class="btn btn-outline-danger me-auto" data-hanh-dong="xoa" ${lyDo ? 'disabled' : ''}><i class="bi bi-trash me-1"></i>Xóa</button>
        <button type="button" class="btn btn-primary" data-hanh-dong="sua" ${lyDo ? 'disabled' : ''}><i class="bi bi-pencil me-1"></i>Sửa</button></div>` : ''}
    </div></div>`;
    document.body.appendChild(wrap);
    renderAvatar(wrap.querySelector('img'), l.duongDanAnh || '');
    const m = bootstrap.Modal.getOrCreateInstance(wrap);
    wrap.addEventListener('click', e => {
      const b = e.target.closest('[data-hanh-dong]');
      if (!b) return;
      m.hide();
      if (b.dataset.hanhDong === 'sua') wrap.addEventListener('hidden.bs.modal', () => moForm(l), { once: true });
      else wrap.addEventListener('hidden.bs.modal', () => DanhSach.xoa({
        tieuDe: `Xóa lịch ${l.hoTenNv}, ${l.tenCa} ${Ui.fmtNgay(l.ngayLamViec)}?`, noiDung: 'Thao tác này không hoàn tác được.',
        url: 'lich-lam-viec/' + encodeURIComponent(l.maLichLamViec), sauKhiXoa: tai }), { once: true });
    });
    wrap.addEventListener('hidden.bs.modal', () => setTimeout(() => wrap.remove()));
    m.show();
  }

  /* ---------- Form xếp lịch ---------- */
  const opt = (v, t, extra = '') => `<option value="${Ui.esc(v)}" ${extra}>${Ui.esc(t)}</option>`;
  const oChonChung = () => `
    <div class="col-sm-6"><label class="form-label" for="maCa">Ca</label><select id="maCa" name="maCa" class="form-select" required>
      <option value="">Chọn ca</option>${dsCa.map(c => opt(c.maCa, `${c.tenCa} (${Ui.fmtGio(c.gioBatDau)}–${Ui.fmtGio(c.gioKetThuc)})`)).join('')}</select></div>
    <div class="col-sm-6"><label class="form-label" for="maNv">Bác sĩ</label><select id="maNv" name="maNv" class="form-select" required></select></div>
    <div class="col-sm-6"><label class="form-label" for="maPhong">Phòng</label><select id="maPhong" name="maPhong" class="form-select" required></select>
      <div class="form-text" id="ghiChuPhong">Chọn bác sĩ trước; chỉ hiện phòng thuộc khoa của bác sĩ (RB11).</div></div>`;

  /** Cập nhật danh sách bác sĩ, phòng; đánh dấu người, phòng đã bận ở ngày + ca đang chọn (một ngày). */
  async function capNhatLuaChon(f, boQuaMa) {
    const bs = dsBacSi.find(b => b.maNv === f.maNv.value);
    let ban = [];
    const ngay = f.ngayLamViec?.value;
    if (ngay && f.maCa.value) {
      try { ban = (await Api.get(`lich-lam-viec?tuNgay=${ngay}&denNgay=${ngay}&maCa=${encodeURIComponent(f.maCa.value)}`)).filter(l => l.maLichLamViec !== boQuaMa); }
      catch { ban = []; }
    }
    const bsBan = new Map(ban.map(l => [l.maNv, l.tenPhong])), phongBan = new Map(ban.map(l => [l.maPhong, l.hoTenNv]));
    const chonNv = f.maNv.value, chonPhong = f.maPhong.value;
    f.maNv.innerHTML = '<option value="">Chọn bác sĩ</option>' + dsKhoa.map(k => {
      const trong = dsBacSi.filter(b => b.maKhoa === k.maKhoa);
      return trong.length ? `<optgroup label="${Ui.esc(k.tenKhoa)}">${trong.map(b => opt(b.maNv, b.hoTen + (bsBan.has(b.maNv) ? ` (đã trực ${bsBan.get(b.maNv)})` : ''), bsBan.has(b.maNv) ? 'disabled' : '')).join('')}</optgroup>` : '';
    }).join('');
    f.maNv.value = bsBan.has(chonNv) ? '' : chonNv;
    const khoa = dsBacSi.find(b => b.maNv === f.maNv.value)?.maKhoa || bs?.maKhoa;
    const phongs = f.maNv.value ? dsPhong.filter(p => p.maKhoa === khoa) : [];
    f.maPhong.innerHTML = `<option value="">${f.maNv.value ? 'Chọn phòng' : 'Chọn bác sĩ trước'}</option>` +
      phongs.map(p => opt(p.maPhong, p.tenPhong + (phongBan.has(p.maPhong) ? ` (đã xếp cho ${phongBan.get(p.maPhong)})` : ''), phongBan.has(p.maPhong) ? 'disabled' : '')).join('');
    f.maPhong.value = phongs.some(p => p.maPhong === chonPhong && !phongBan.has(chonPhong)) ? chonPhong : '';
    $('ghiChuPhong').textContent = f.maNv.value && !phongs.length ? 'Khoa của bác sĩ này chưa có phòng nào.' : 'Chỉ hiện phòng thuộc khoa của bác sĩ (RB11). Mục mờ là đã có lịch trong ca này.';
  }

  function moForm(l, macDinh = {}) {
    DanhSach.moForm({
      tieuDe: l ? `Sửa lịch ${l.maLichLamViec}` : 'Thêm lịch làm việc',
      noiDung: `<div class="row g-3">
        <div class="col-sm-6"><label class="form-label" for="ngayLamViec">Ngày làm việc</label><input id="ngayLamViec" name="ngayLamViec" type="date" class="form-control" min="${homNay}" required></div>
        ${oChonChung()}</div>`,
      khiMo: f => {
        f.ngayLamViec.value = l?.ngayLamViec || macDinh.ngay || homNay;
        f.maCa.value = l?.maCa || macDinh.ca || '';
        const capNhat = () => capNhatLuaChon(f, l?.maLichLamViec);
        capNhat().then(() => { if (l) { f.maNv.value = l.maNv; return capNhat().then(() => { f.maPhong.value = l.maPhong; }); } });
        ['ngayLamViec', 'maCa', 'maNv'].forEach(n => f[n].addEventListener('change', capNhat));
      },
      thuThap: f => {
        const body = { ngayLamViec: f.ngayLamViec.value, maCa: f.maCa.value, maNv: f.maNv.value, maPhong: f.maPhong.value };
        if (!body.ngayLamViec) return 'Vui lòng chọn ngày làm việc.';
        if (body.ngayLamViec < homNay) return 'Không được xếp lịch cho ngày đã qua (RB9).';
        if (!body.maCa) return 'Vui lòng chọn ca.';
        if (!body.maNv) return 'Vui lòng chọn bác sĩ.';
        if (!body.maPhong) return 'Vui lòng chọn phòng.';
        return body;
      },
      gui: async body => {
        await (l ? Api.put('lich-lam-viec/' + encodeURIComponent(l.maLichLamViec), body) : Api.post('lich-lam-viec', body));
        Ui.toast(l ? 'Đã cập nhật lịch làm việc.' : 'Đã thêm lịch làm việc.');
        tai();
      }
    });
  }

  function moHangLoat() {
    const tuan = dauTuan(cong(new Date(), 7));
    DanhSach.moForm({
      tieuDe: 'Xếp lịch nhiều ngày', nutLuu: 'Xếp lịch', rong: true,
      noiDung: `<p class="small text-muted-kmn">Xếp cùng một bác sĩ vào cùng phòng, cùng ca cho các thứ đã chọn (tối đa 62 ngày).
          Nếu có ngày bị trùng, hệ thống không tạo lịch nào và báo các ngày trùng.</p>
        <div class="row g-3">
        <div class="col-sm-6"><label class="form-label" for="tuNgay">Từ ngày</label><input id="tuNgay" name="tuNgay" type="date" class="form-control" min="${homNay}" required></div>
        <div class="col-sm-6"><label class="form-label" for="denNgay">Đến ngày</label><input id="denNgay" name="denNgay" type="date" class="form-control" min="${homNay}" required></div>
        <div class="col-12"><span class="form-label d-block">Các thứ trong tuần</span><div class="btn-group flex-wrap thu-chon" role="group" aria-label="Các thứ trong tuần">
          ${[1, 2, 3, 4, 5, 6, 0].map(t => `<input type="checkbox" class="btn-check" name="thu" id="thu${t}" value="${t}" ${t >= 1 && t <= 5 ? 'checked' : ''}><label class="btn btn-outline-primary" for="thu${t}">${THU_NGAN[t]}</label>`).join('')}
        </div><div class="form-text" id="soNgay"></div></div>
        ${oChonChung()}</div>`,
      khiMo: f => {
        f.tuNgay.value = iso(tuan); f.denNgay.value = iso(cong(tuan, 6));
        const dem = () => {
          const thu = [...f.querySelectorAll('[name=thu]:checked')].map(x => +x.value);
          let n = 0;
          if (f.tuNgay.value && f.denNgay.value) for (let d = doc(f.tuNgay.value); d <= doc(f.denNgay.value); d = cong(d, 1)) if (thu.includes(d.getDay())) n++;
          $('soNgay').textContent = `Sẽ tạo ${n} lịch làm việc.`;
        };
        f.addEventListener('change', dem); dem();
        capNhatLuaChon(f);
        f.maNv.addEventListener('change', () => capNhatLuaChon(f));
      },
      thuThap: f => {
        const body = { tuNgay: f.tuNgay.value, denNgay: f.denNgay.value, cacThu: [...f.querySelectorAll('[name=thu]:checked')].map(x => +x.value),
          maCa: f.maCa.value, maNv: f.maNv.value, maPhong: f.maPhong.value };
        if (!body.tuNgay || !body.denNgay) return 'Vui lòng chọn khoảng ngày.';
        if (body.tuNgay < homNay) return 'Không được xếp lịch cho ngày đã qua (RB9).';
        if (body.denNgay < body.tuNgay) return 'Ngày kết thúc phải sau ngày bắt đầu.';
        if (!body.cacThu.length) return 'Vui lòng chọn ít nhất một thứ trong tuần.';
        if (!body.maCa || !body.maNv || !body.maPhong) return 'Vui lòng chọn đủ ca, bác sĩ và phòng.';
        return body;
      },
      gui: async body => {
        const kq = await Api.post('lich-lam-viec/hang-loat', body);
        Ui.toast(`Đã tạo ${kq.soLichDaTao} lịch làm việc.`);
        moc = doc(body.tuNgay); tai();
      }
    });
  }

  /* ---------- Sự kiện ---------- */
  const buoc = huong => {
    if (kieu === 'ngay') moc = cong(moc, huong);
    else if (kieu === 'tuan') moc = cong(moc, 7 * huong);
    else moc = new Date(moc.getFullYear(), moc.getMonth() + huong, 1);
    tai();
  };
  $('btnTruoc').addEventListener('click', () => buoc(-1));
  $('btnSau').addEventListener('click', () => buoc(1));
  $('btnHomNay').addEventListener('click', () => { moc = new Date(); tai(); });
  document.querySelectorAll('[name=kieuXem]').forEach(r => r.addEventListener('change', () => { kieu = r.value; tai(); }));
  $('locKhoa').addEventListener('change', () => { locTheoKhoa(); tai(); });
  ['locBacSi', 'locPhong'].forEach(id => $(id).addEventListener('change', tai));
  $('btnThem').addEventListener('click', () => moForm(null));
  $('btnHangLoat').addEventListener('click', moHangLoat);

  $('lich').addEventListener('click', e => {
    const c = e.target.closest('[data-ma]');
    if (c) { const l = dsLich.find(x => x.maLichLamViec === c.dataset.ma); if (l) moChiTiet(l); return; }
    const t = e.target.closest('[data-them-ngay]');
    if (t) { moForm(null, { ngay: t.dataset.themNgay, ca: t.dataset.themCa }); return; }
    const o = e.target.closest('[data-ngay]');
    if (o) { moc = doc(o.dataset.ngay); kieu = 'ngay'; $('xemNgay').checked = true; tai(); }
  });
  $('lich').addEventListener('keydown', e => {
    if ((e.key === 'Enter' || e.key === ' ') && e.target.matches('[data-ngay]')) { e.preventDefault(); e.target.click(); }
  });

  /** Lọc khoa thì danh sách bác sĩ và phòng chỉ còn của khoa đó. */
  function locTheoKhoa() {
    const k = $('locKhoa').value;
    const giuBs = $('locBacSi').value, giuPhong = $('locPhong').value;
    $('locBacSi').innerHTML = '<option value="">Tất cả bác sĩ</option>' + dsBacSi.filter(b => !k || b.maKhoa === k).map(b => opt(b.maNv, b.hoTen)).join('');
    $('locPhong').innerHTML = '<option value="">Tất cả phòng</option>' + dsPhong.filter(p => !k || p.maKhoa === k).map(p => opt(p.maPhong, p.tenPhong)).join('');
    $('locBacSi').value = [...$('locBacSi').options].some(o => o.value === giuBs) ? giuBs : '';
    $('locPhong').value = [...$('locPhong').options].some(o => o.value === giuPhong) ? giuPhong : '';
  }

  (async () => {
    try {
      const [ca, khoa, phong, bs] = await Promise.all([
        Api.get('ca'), Api.get('khoa?pageSize=100'), Api.get('phong?pageSize=100'), laBacSi ? [] : Api.get('lich-lam-viec/bac-si')]);
      dsCa = ca; dsKhoa = khoa.items; dsPhong = phong.items; dsBacSi = bs;
      $('locKhoa').insertAdjacentHTML('beforeend', dsKhoa.map(k => opt(k.maKhoa, k.tenKhoa)).join(''));
      locTheoKhoa();
    } catch (err) { Ui.loi(err); }
    tai();
  })();
})();
