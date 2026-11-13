/* Khám bệnh (M7): hàng chờ của bác sĩ, ghi triệu chứng, chẩn đoán, lời dặn, tiền công, kê đơn, hoàn tất (tạo hóa đơn, khóa hồ sơ).
   Mở ?ma=HS... để xem một hồ sơ cụ thể (hồ sơ đã hoàn tất chỉ xem). */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const { esc, mucHangCho, thongTinKhach, nguon, tenThuoc } = BacSi;
  let hangCho = [], hs = null, thayDoi = false, ketQuaThuoc = [], thuocChon = null;

  /* ---------- Hàng chờ ---------- */
  async function taiHangCho() {
    const el = $('hangCho');
    try {
      hangCho = await Api.get('bac-si/hang-cho');
      el.innerHTML = hangCho.length ? hangCho.map((h, i) => mucHangCho(h, i, { current: hs?.maHoSo === h.maHoSo })).join('')
        : '<div class="empty-state"><i class="bi bi-cup-hot"></i>Chưa có bệnh nhân nào chờ khám.<br>Lễ tân tiếp nhận xong, bệnh nhân sẽ hiện ở đây.</div>';
    } catch (e) { el.innerHTML = `<div class="small text-danger">${esc(e.message)}</div>`; }
  }

  async function chonHoSo(ma) {
    if (hs?.maHoSo === ma) return;
    if (thayDoi && !await Ui.xacNhan({ tieuDe: 'Chưa lưu thông tin khám', noiDung: `Bạn chưa lưu thay đổi của hồ sơ ${hs.maHoSo}. Bỏ thay đổi và mở hồ sơ khác?`, nutDongY: 'Bỏ thay đổi', kieu: 'danger' })) return;
    $('khungKham').innerHTML = '<div class="card-kmn panel"><div class="skeleton" style="height:320px"></div></div>';
    try {
      ve(await Api.get('bac-si/ho-so/' + encodeURIComponent(ma)));
      history.replaceState(null, '', '?ma=' + encodeURIComponent(ma));
    } catch (e) {
      hs = null;
      $('khungKham').innerHTML = `<div class="card-kmn panel"><div class="empty-state"><i class="bi bi-exclamation-circle"></i>${esc(e.message)}</div></div>`;
    }
    $('hangCho').querySelectorAll('[data-chon-ho-so]').forEach(x => x.classList.toggle('current', x.dataset.chonHoSo === hs?.maHoSo));
  }

  /* ---------- Khung khám ---------- */
  function ve(chiTiet) {
    hs = chiTiet; thayDoi = false; thuocChon = null;
    const khoa = hs.daKhoa, dis = khoa ? 'disabled' : '';
    $('khungKham').innerHTML = `
      <div class="card-kmn panel mb-3" style="height:auto">
        <div class="panel-title flex-wrap gap-2"><div><h2 class="mb-0">${esc(hs.tenKhach)} <small class="text-muted-kmn fw-normal">· ${thongTinKhach(hs)}</small></h2>
          <small class="text-muted-kmn"><span class="mono">${esc(hs.maHoSo)}</span> · ${nguon(hs)} · Tiếp nhận ${Ui.fmtNgayGio(hs.ngayGioKham)} bởi ${esc(hs.tenNvTiepNhan)}</small></div>
          ${Ui.badgeTrangThai(hs.trangThai)}</div>
        ${khoa ? `<div class="alert alert-success border-0 rounded-3 small mb-3"><i class="bi bi-lock me-1"></i>Hồ sơ đã hoàn tất, hóa đơn <b class="mono">${esc(hs.hoaDon.maHoaDon)}</b>
          ${esc(hs.hoaDon.trangThai.toLowerCase())}. Hồ sơ chỉ xem, không sửa được nữa.</div>` : ''}
        <form id="formKham" class="row g-3" novalidate>
          <div class="col-lg-5">
            <label class="form-label" for="trieuChung">Triệu chứng</label>
            <textarea class="form-control mb-2" id="trieuChung" rows="2" maxlength="255" ${dis}>${esc(hs.trieuChung)}</textarea>
            <label class="form-label" for="chanDoan">Chẩn đoán ${khoa ? '' : '<small class="text-muted-kmn">(bắt buộc khi hoàn tất)</small>'}</label>
            <textarea class="form-control mb-2" id="chanDoan" rows="2" maxlength="255" ${dis}>${esc(hs.chanDoanBenh || '')}</textarea>
            <label class="form-label" for="loiDan">Lời dặn</label>
            <textarea class="form-control mb-2" id="loiDan" rows="2" maxlength="255" ${dis}>${esc(hs.loiDan || '')}</textarea>
            <label class="form-label" for="tienCong">Tiền công khám (đ)</label>
            <input class="form-control" id="tienCong" type="number" min="0" step="1000" inputmode="numeric" value="${Number(hs.tienCongKham)}" ${dis}>
          </div>
          <div class="col-lg-7">
            <span class="form-label d-block">Đơn thuốc</span>
            ${khoa ? '' : `<div class="border rounded-3 p-2 mb-2">
              <div class="input-group input-group-sm"><span class="input-group-text"><i class="bi bi-search"></i></span>
                <input class="form-control" id="timThuoc" placeholder="Tìm thuốc theo tên hoặc mã" aria-label="Tìm thuốc" autocomplete="off"></div>
              <div id="ketQuaThuoc"></div>
              <div id="thuocDaChon" class="d-none mt-2"></div>
            </div>`}
            <div class="table-responsive"><table class="table table-sm table-kmn mb-2">
              <thead><tr><th>Thuốc</th><th class="text-center">SL</th><th class="text-end">Đơn giá</th><th class="text-end">Thành tiền</th>${khoa ? '' : '<th></th>'}</tr></thead>
              <tbody>${hs.donThuoc.length ? hs.donThuoc.map(dongThuoc).join('')
                : `<tr><td colspan="${khoa ? 4 : 5}" class="text-center text-muted-kmn small py-3">Chưa kê thuốc.</td></tr>`}</tbody>
              <tfoot>
                <tr><td colspan="3" class="small text-muted-kmn">Tiền thuốc</td><td class="text-end">${Ui.fmtTien(hs.tienThuoc)}</td>${khoa ? '' : '<td></td>'}</tr>
                <tr><td colspan="3" class="small text-muted-kmn">Tiền công khám</td><td class="text-end" id="hienTienCong"></td>${khoa ? '' : '<td></td>'}</tr>
                <tr><th colspan="3">${khoa ? 'Tổng hóa đơn' : 'Tổng tạm tính'}</th><th class="text-end text-primary" id="hienTong"></th>${khoa ? '' : '<th></th>'}</tr>
              </tfoot>
            </table></div>
            ${khoa ? '' : `<p class="small text-muted-kmn mb-2"><i class="bi bi-info-circle me-1"></i>Đơn giá chốt theo danh mục lúc kê. Kho chỉ bị trừ khi thu ngân xác nhận thanh toán.</p>
            <div class="alert alert-danger small d-none" id="loiKham" role="alert"></div>
            <div class="d-flex justify-content-end flex-wrap gap-2">
              <button type="button" class="btn btn-soft" id="btnLuu"><i class="bi bi-save me-1"></i>Lưu nháp</button>
              <button type="submit" class="btn btn-primary" id="btnHoanTat"><i class="bi bi-check2-all me-1"></i>Hoàn tất khám</button></div>`}
            ${khoa && hangCho.length ? `<div class="text-end"><button type="button" class="btn btn-primary" id="btnTiepTheo">Bệnh nhân tiếp theo <i class="bi bi-arrow-right ms-1"></i></button></div>` : ''}
          </div>
        </form>
      </div>
      <div class="card-kmn panel" style="height:auto">
        <div class="panel-title"><h2>Lần khám trước của bệnh nhân</h2><span class="small text-muted-kmn">Chỉ xem</span></div>
        <div id="lanKhamTruoc"><div class="skeleton" style="height:48px"></div></div>
      </div>`;
    capNhatTong();
    taiLanKhamTruoc();
  }

  function dongThuoc(t) {
    const thieu = t.soLuong > t.soLuongTon;
    return `<tr>
      <td><b>${tenThuoc(t)}</b> <small class="text-muted-kmn">${esc(t.donViTinh)}</small><br><small class="text-muted-kmn"><i class="bi bi-prescription2 me-1"></i>${esc(t.lieuDung)}</small>
        ${!hs.daKhoa && thieu ? `<br><small class="text-danger"><i class="bi bi-exclamation-triangle me-1"></i>Kho chỉ còn ${t.soLuongTon}, cần sửa trước khi hoàn tất</small>` : ''}</td>
      <td class="text-center">${t.soLuong}</td>
      <td class="text-end">${Ui.fmtTien(t.donGia)}</td>
      <td class="text-end">${Ui.fmtTien(t.thanhTien)}</td>
      ${hs.daKhoa ? '' : `<td class="text-end text-nowrap">
        <button type="button" class="btn btn-sm btn-link p-0 me-2" data-sua-thuoc="${esc(t.maThuoc)}" title="Sửa số lượng, liều dùng" aria-label="Sửa ${esc(t.tenThuoc)}"><i class="bi bi-pencil"></i></button>
        <button type="button" class="btn btn-sm btn-link text-danger p-0" data-xoa-thuoc="${esc(t.maThuoc)}" title="Bỏ khỏi đơn" aria-label="Bỏ ${esc(t.tenThuoc)}"><i class="bi bi-x-lg"></i></button></td>`}</tr>`;
  }

  const tienCongNhap = () => hs.daKhoa ? Number(hs.tienCongKham) : Math.max(0, Number($('tienCong').value) || 0);
  function capNhatTong() {
    $('hienTienCong').textContent = Ui.fmtTien(tienCongNhap());
    $('hienTong').textContent = Ui.fmtTien(hs.daKhoa ? hs.hoaDon.tongTien : tienCongNhap() + Number(hs.tienThuoc));
  }

  async function taiLanKhamTruoc() {
    const ma = hs.maHoSo, el = $('lanKhamTruoc');
    try {
      const ds = await Api.get(`bac-si/ho-so/${encodeURIComponent(ma)}/lan-kham-truoc`);
      if (hs?.maHoSo !== ma) return;
      el.innerHTML = ds.length ? `<ul class="timeline">${ds.map(l => `<li>
          <div class="t">${Ui.fmtNgayGio(l.ngayGioKham)} · ${esc(l.chanDoanBenh || 'Chưa ghi chẩn đoán')}</div>
          <small class="text-muted-kmn">${esc(l.tenBacSi)} · ${esc(l.tenKhoa)} · <span class="mono">${esc(l.maHoSo)}</span></small>
          <div class="small mt-1"><b>Triệu chứng:</b> ${esc(l.trieuChung)}</div>
          ${l.loiDan ? `<div class="small"><b>Lời dặn:</b> ${esc(l.loiDan)}</div>` : ''}
          ${l.thuoc.length ? `<div class="small"><b>Thuốc:</b> ${l.thuoc.map(esc).join('; ')}</div>` : ''}</li>`).join('')}</ul>`
        : '<div class="small text-muted-kmn">Chưa có lần khám nào trước đây.</div>';
    } catch (e) { el.innerHTML = `<div class="small text-danger">${esc(e.message)}</div>`; }
  }

  /* ---------- Lưu, hoàn tất ---------- */
  function thuThap() {
    const tien = $('tienCong').value.trim();
    const body = { trieuChung: $('trieuChung').value.trim(), chanDoanBenh: $('chanDoan').value.trim() || null,
      loiDan: $('loiDan').value.trim() || null, tienCongKham: tien === '' ? null : Number(tien) };
    if (!body.trieuChung) return 'Vui lòng nhập triệu chứng.';
    if (body.tienCongKham == null || !Number.isInteger(body.tienCongKham) || body.tienCongKham < 0) return 'Tiền công khám là số đồng nguyên, không âm.';
    return body;
  }
  const baoLoi = msg => { $('loiKham').textContent = msg; $('loiKham').classList.remove('d-none'); };

  async function luu() {
    $('loiKham').classList.add('d-none');
    const body = thuThap();
    if (typeof body === 'string') return baoLoi(body);
    try {
      ve(await Ui.dangXuLy($('btnLuu'), () => Api.put('bac-si/ho-so/' + encodeURIComponent(hs.maHoSo), body)));
      Ui.toast('Đã lưu nháp hồ sơ ' + hs.maHoSo + '.');
      taiHangCho();
    } catch (e) { baoLoi(e.message); }
  }

  async function hoanTat(e) {
    e.preventDefault();
    $('loiKham').classList.add('d-none');
    const body = thuThap();
    if (typeof body === 'string') return baoLoi(body);
    if (!body.chanDoanBenh) { baoLoi('Vui lòng nhập chẩn đoán trước khi hoàn tất khám.'); return $('chanDoan').focus(); }
    const tong = body.tienCongKham + Number(hs.tienThuoc);
    if (!await Ui.xacNhan({
      tieuDe: 'Hoàn tất khám', nutDongY: 'Hoàn tất', icon: 'check2-all',
      noiDung: `Lập hóa đơn ${Ui.fmtTien(tong)} cho ${hs.tenKhach} (${hs.donThuoc.length} thuốc). Sau khi hoàn tất, hồ sơ và đơn thuốc không sửa được nữa.`
    })) return;
    try {
      ve(await Ui.dangXuLy($('btnHoanTat'), () => Api.post(`bac-si/ho-so/${encodeURIComponent(hs.maHoSo)}/hoan-tat`, body)));
      Ui.toast(`Đã hoàn tất hồ sơ ${hs.maHoSo}, hóa đơn ${hs.hoaDon.maHoaDon} chờ thu ngân.`);
      await taiHangCho();
      ve(hs); // vẽ lại để hiện nút "Bệnh nhân tiếp theo" theo hàng chờ mới
    } catch (err) {
      baoLoi(err.message);
      if (err.status === 409) chonLai();
    }
  }

  /** Hồ sơ có thể vừa bị hoàn tất ở tab khác: tải lại. */
  async function chonLai() {
    const ma = hs.maHoSo; hs = null; thayDoi = false;
    await chonHoSo(ma);
    taiHangCho();
  }

  /* ---------- Kê đơn ---------- */
  async function timThuoc() {
    const q = $('timThuoc').value.trim(), el = $('ketQuaThuoc');
    if (q.length < 2) { el.innerHTML = ''; return; }
    try {
      ketQuaThuoc = (await Api.get('thuoc?pageSize=6&q=' + encodeURIComponent(q))).items;
      const daCo = new Set(hs.donThuoc.map(t => t.maThuoc));
      el.innerHTML = ketQuaThuoc.length ? `<div class="list-group list-group-flush mt-1">${ketQuaThuoc.map(t => {
          const tat = daCo.has(t.maThuoc) || t.soLuongTon <= 0;
          return `<button type="button" class="list-group-item list-group-item-action small d-flex justify-content-between gap-2" data-chon-thuoc="${esc(t.maThuoc)}" ${tat ? 'disabled' : ''}>
            <span><b>${tenThuoc(t)}</b> <span class="text-muted-kmn">${esc(t.donViTinh)} · ${esc(t.nsx)}</span></span>
            <span class="text-nowrap ${t.sapHet ? 'text-warning' : 'text-muted-kmn'}">${daCo.has(t.maThuoc) ? 'Đã có trong đơn' : `${Ui.fmtTien(t.donGiaThuoc)} · tồn ${t.soLuongTon}`}</span></button>`;
        }).join('')}</div>`
        : '<div class="small text-muted-kmn p-2">Không tìm thấy thuốc.</div>';
    } catch (e) { el.innerHTML = `<div class="small text-danger p-2">${esc(e.message)}</div>`; }
  }

  function chonThuoc(t) {
    thuocChon = t;
    $('ketQuaThuoc').innerHTML = '';
    $('timThuoc').value = '';
    const el = $('thuocDaChon');
    el.classList.remove('d-none');
    el.innerHTML = `<div class="d-flex justify-content-between align-items-center small mb-2"><span><i class="bi bi-capsule text-primary me-1"></i><b>${tenThuoc(t)}</b>
        ${esc(t.donViTinh)} · ${Ui.fmtTien(t.donGiaThuoc)} · tồn ${t.soLuongTon}</span>
        <button type="button" class="btn btn-sm btn-link p-0" id="btnBoChonThuoc">Đổi</button></div>
      <div class="row g-2">
        <div class="col-3"><label class="visually-hidden" for="soLuongThuoc">Số lượng</label>
          <input class="form-control form-control-sm" id="soLuongThuoc" type="number" min="1" max="${t.soLuongTon}" step="1" placeholder="SL" value="1"></div>
        <div class="col"><label class="visually-hidden" for="lieuDung">Liều dùng</label>
          <input class="form-control form-control-sm" id="lieuDung" maxlength="255" placeholder="Liều dùng, ví dụ: ngày 2 lần, mỗi lần 1 viên sau ăn"></div>
        <div class="col-auto"><button type="button" class="btn btn-sm btn-primary" id="btnThemThuoc"><i class="bi bi-plus-lg"></i> Kê</button></div>
      </div>
      <div class="small text-danger mt-1 d-none" id="loiThuoc"></div>`;
    $('soLuongThuoc').focus();
  }

  function boChonThuoc() {
    thuocChon = null;
    $('thuocDaChon').classList.add('d-none');
    $('timThuoc').focus();
  }

  async function keThuoc() {
    const loi = msg => { $('loiThuoc').textContent = msg; $('loiThuoc').classList.remove('d-none'); };
    const soLuong = Number($('soLuongThuoc').value), lieuDung = $('lieuDung').value.trim();
    if (!Number.isInteger(soLuong) || soLuong < 1) return loi('Số lượng là số nguyên từ 1.');
    if (soLuong > thuocChon.soLuongTon) return loi(`Kho chỉ còn ${thuocChon.soLuongTon}.`);
    if (!lieuDung) { loi('Vui lòng nhập liều dùng.'); return $('lieuDung').focus(); }
    try {
      const moi = await Ui.dangXuLy($('btnThemThuoc'), () => Api.post(`bac-si/ho-so/${encodeURIComponent(hs.maHoSo)}/don-thuoc`, { maThuoc: thuocChon.maThuoc, soLuong, lieuDung }));
      giuNhapRoiVe(moi);
      Ui.toast(`Đã kê ${thuocChon?.tenThuoc || 'thuốc'}.`);
      taiHangCho();
    } catch (e) { loi(e.message); }
  }

  /** Vẽ lại sau khi đổi đơn thuốc nhưng giữ phần bác sĩ đang gõ dở ở form khám. */
  function giuNhapRoiVe(moi) {
    const nhap = thayDoi ? { tc: $('trieuChung').value, cd: $('chanDoan').value, ld: $('loiDan').value, tien: $('tienCong').value } : null;
    ve(moi);
    if (nhap) {
      $('trieuChung').value = nhap.tc; $('chanDoan').value = nhap.cd; $('loiDan').value = nhap.ld; $('tienCong').value = nhap.tien;
      thayDoi = true; capNhatTong();
    }
  }

  function suaThuoc(ma) {
    const t = hs.donThuoc.find(x => x.maThuoc === ma);
    DanhSach.moForm({
      tieuDe: 'Sửa thuốc trong đơn', nutLuu: 'Lưu',
      noiDung: `<p class="small mb-3"><b>${tenThuoc(t)}</b> ${esc(t.donViTinh)} · ${Ui.fmtTien(t.donGia)} (giá đã chốt) · tồn ${t.soLuongTon}</p>
        <div class="row g-3"><div class="col-4"><label class="form-label" for="suaSl">Số lượng</label>
          <input class="form-control" id="suaSl" name="soLuong" type="number" min="1" step="1" value="${t.soLuong}"></div>
        <div class="col-8"><label class="form-label" for="suaLd">Liều dùng</label>
          <input class="form-control" id="suaLd" name="lieuDung" maxlength="255" value="${esc(t.lieuDung)}"></div></div>`,
      thuThap: f => {
        const body = { soLuong: Number(f.soLuong.value), lieuDung: f.lieuDung.value.trim() };
        if (!Number.isInteger(body.soLuong) || body.soLuong < 1) return 'Số lượng là số nguyên từ 1.';
        if (body.soLuong > t.soLuongTon) return `Kho chỉ còn ${t.soLuongTon}.`;
        if (!body.lieuDung) return 'Vui lòng nhập liều dùng.';
        return body;
      },
      gui: async body => {
        giuNhapRoiVe(await Api.put(`bac-si/ho-so/${encodeURIComponent(hs.maHoSo)}/don-thuoc/${encodeURIComponent(ma)}`, body));
        Ui.toast('Đã cập nhật đơn thuốc.');
      }
    });
  }

  async function xoaThuoc(ma) {
    const t = hs.donThuoc.find(x => x.maThuoc === ma);
    if (!await Ui.xacNhan({ tieuDe: 'Bỏ thuốc khỏi đơn', noiDung: `Bỏ ${t.tenThuoc} khỏi đơn thuốc?`, nutDongY: 'Bỏ', kieu: 'danger', icon: 'trash' })) return;
    try {
      giuNhapRoiVe(await Api.del(`bac-si/ho-so/${encodeURIComponent(hs.maHoSo)}/don-thuoc/${encodeURIComponent(ma)}`));
      Ui.toast('Đã bỏ thuốc khỏi đơn.');
      taiHangCho();
    } catch (e) { Ui.loi(e); if (e.status === 409) chonLai(); }
  }

  /* ---------- Sự kiện ---------- */
  const chon = e => {
    const c = e.target.closest('[data-chon-ho-so]');
    if (!c || (e.type === 'keydown' && e.key !== 'Enter' && e.key !== ' ')) return;
    e.preventDefault();
    chonHoSo(c.dataset.chonHoSo);
  };
  $('hangCho').addEventListener('click', chon);
  $('hangCho').addEventListener('keydown', chon);
  $('btnTaiHangCho').addEventListener('click', taiHangCho);

  const timTreo = Ui.treo(timThuoc);
  const k = $('khungKham');
  k.addEventListener('input', e => {
    if (e.target.id === 'timThuoc') return timTreo();
    if (['trieuChung', 'chanDoan', 'loiDan', 'tienCong'].includes(e.target.id)) { thayDoi = true; if (e.target.id === 'tienCong') capNhatTong(); }
  });
  k.addEventListener('submit', e => { if (e.target.id === 'formKham') hoanTat(e); });
  k.addEventListener('keydown', e => {
    if (e.key === 'Enter' && ['soLuongThuoc', 'lieuDung'].includes(e.target.id)) { e.preventDefault(); keThuoc(); }
  });
  k.addEventListener('click', e => {
    const b = e.target.closest('button');
    if (!b) return;
    if (b.dataset.chonThuoc) chonThuoc(ketQuaThuoc.find(t => t.maThuoc === b.dataset.chonThuoc));
    else if (b.id === 'btnBoChonThuoc') boChonThuoc();
    else if (b.id === 'btnThemThuoc') keThuoc();
    else if (b.id === 'btnLuu') luu();
    else if (b.id === 'btnTiepTheo') chonHoSo(hangCho[0].maHoSo);
    else if (b.dataset.suaThuoc) suaThuoc(b.dataset.suaThuoc);
    else if (b.dataset.xoaThuoc) xoaThuoc(b.dataset.xoaThuoc);
  });
  window.addEventListener('beforeunload', e => { if (thayDoi) { e.preventDefault(); e.returnValue = ''; } });

  (async () => {
    await taiHangCho();
    const ma = Ui.thamSo('ma') || hangCho[0]?.maHoSo;
    if (ma) chonHoSo(ma);
    else $('khungKham').innerHTML = '<div class="card-kmn panel"><div class="empty-state"><i class="bi bi-clipboard2-pulse"></i>Chọn bệnh nhân trong hàng chờ để bắt đầu khám.</div></div>';
  })();
})();
