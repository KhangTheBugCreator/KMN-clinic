/* Hồ sơ khám (M10, quản trị viên): hồ sơ của mọi bác sĩ, chi tiết bên phải, chỉ đọc. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const { esc, thongTinKhach, tenThuoc } = BacSi;
  let maChon = null;

  const dong = h => `<tr class="${h.maHoSo === maChon ? 'table-active' : ''}" data-chon="${esc(h.maHoSo)}" role="button">
      <td class="mono">${esc(h.maHoSo)}<br><small class="text-muted-kmn">${Ui.fmtNgayGio(h.ngayGioKham)}</small></td>
      <td><b class="text-navy">${esc(h.tenKhach)}</b><br><small class="text-muted-kmn">${thongTinKhach(h)} · ${h.maLichHen ? 'Có hẹn' : 'Vãng lai'}</small></td>
      <td>${esc(h.tenBacSi)}<br><small class="text-muted-kmn">${esc(h.tenKhoa)}</small></td>
      <td>${Ui.badgeTrangThai(h.trangThai)}</td>
      <td class="text-end text-nowrap">${h.tongTien != null ? Ui.fmtTien(h.tongTien) : '<span class="text-muted-kmn">—</span>'}</td></tr>`;

  const bang = DanhSach.bangPhanTrang({
    url: 'quan-tri/ho-so', tbody: $('bang'), phanTrang: $('phanTrang'), soCot: 5, pageSize: 12,
    rong: 'Không có hồ sơ phù hợp.',
    thamSo: () => ({ q: $('oTim').value.trim(), tuNgay: $('tuNgay').value, denNgay: $('denNgay').value,
      trangThai: $('locTrangThai').value, maNv: $('locBacSi').value }),
    dong
  });

  const veRong = () => { $('chiTiet').innerHTML = '<div class="empty-state"><i class="bi bi-journal-medical"></i>Chọn một hồ sơ để xem chi tiết.</div>'; };
  const dongTT = (nhan, gt) => `<div class="d-flex justify-content-between gap-3 small py-1 border-bottom"><span class="text-muted-kmn text-nowrap">${nhan}</span><span class="text-end">${gt}</span></div>`;
  const hoac = v => v ? esc(v) : '<span class="text-muted-kmn">Chưa ghi</span>';

  function veChiTiet(hs) {
    const don = hs.donThuoc.length
      ? `<div class="table-responsive"><table class="table table-sm mb-2"><thead class="small text-muted-kmn"><tr><th>Thuốc</th><th class="text-center">SL</th><th>Liều dùng</th><th class="text-end">Thành tiền</th></tr></thead>
          <tbody class="small">${hs.donThuoc.map(t => `<tr><td>${tenThuoc(t)} <span class="text-muted-kmn">${esc(t.donViTinh)}</span></td><td class="text-center">${t.soLuong}</td>
            <td>${esc(t.lieuDung)}</td><td class="text-end text-nowrap">${Ui.fmtTien(t.thanhTien)}</td></tr>`).join('')}</tbody></table></div>`
      : '<div class="small text-muted-kmn mb-2">Không kê thuốc.</div>';
    $('chiTiet').innerHTML = `<div class="panel-title flex-wrap gap-2"><h2 class="mb-0">Hồ sơ <span class="mono">${esc(hs.maHoSo)}</span></h2>${Ui.badgeTrangThai(hs.trangThai)}</div>
      ${dongTT('Bệnh nhân', `<b class="text-navy">${esc(hs.tenKhach)}</b> · ${thongTinKhach(hs)} · <span class="mono">${esc(hs.maKh)}</span>`)}
      ${dongTT('Thời gian', Ui.fmtNgayGio(hs.ngayGioKham) + ' · ' + BacSi.nguon(hs))}
      ${dongTT('Bác sĩ khám', `${esc(hs.tenBacSi)} · ${esc(hs.tenKhoa)}`)}
      ${dongTT('Lễ tân tiếp nhận', esc(hs.tenNvTiepNhan))}
      ${dongTT('Triệu chứng', esc(hs.trieuChung))}
      ${dongTT('Chẩn đoán', hoac(hs.chanDoanBenh))}
      ${dongTT('Lời dặn', hoac(hs.loiDan))}
      ${dongTT('Tiền công khám', Ui.fmtTien(hs.tienCongKham))}
      <h3 class="h6 mt-3">Đơn thuốc</h3>${don}
      ${hs.hoaDon
        ? `<div class="d-flex justify-content-between align-items-center small mt-2 p-2 rounded-3 bg-light"><span>Hóa đơn <a class="mono" href="qt-hoa-don.html?ma=${encodeURIComponent(hs.hoaDon.maHoaDon)}">${esc(hs.hoaDon.maHoaDon)}</a> · ${Ui.badgeTrangThai(hs.hoaDon.trangThai)}</span><b class="text-primary">${Ui.fmtTien(hs.hoaDon.tongTien)}</b></div>`
        : '<div class="small text-muted-kmn mt-2"><i class="bi bi-hourglass-split me-1"></i>Bác sĩ chưa hoàn tất khám nên chưa có hóa đơn.</div>'}`;
  }

  async function chon(ma) {
    maChon = ma;
    $('bang').querySelectorAll('tr[data-chon]').forEach(tr => tr.classList.toggle('table-active', tr.dataset.chon === ma));
    $('chiTiet').innerHTML = '<div class="skeleton" style="height:280px"></div>';
    try { veChiTiet(await Api.get('quan-tri/ho-so/' + encodeURIComponent(ma))); }
    catch (e) { $('chiTiet').innerHTML = `<div class="empty-state"><i class="bi bi-exclamation-circle"></i>${esc(e.message)}</div>`; }
  }

  Api.get('lich-lam-viec/bac-si').then(bs => {
    $('locBacSi').insertAdjacentHTML('beforeend', bs.map(b => `<option value="${esc(b.maNv)}">${esc(b.hoTen)} · ${esc(b.tenKhoa)}</option>`).join(''));
  }).catch(() => { /* vẫn lọc được theo cách khác */ });

  $('bang').addEventListener('click', e => {
    const t = e.target.closest('[data-chon]');
    if (!t) return;
    chon(t.dataset.chon);
    if (window.innerWidth < 1200) $('chiTiet').scrollIntoView({ behavior: 'smooth', block: 'start' });
  });
  $('oTim').addEventListener('input', Ui.treo(() => bang.tai(1)));
  if (Ui.thamSo('trangThai')) $('locTrangThai').value = Ui.thamSo('trangThai');
  ['tuNgay', 'denNgay', 'locTrangThai', 'locBacSi'].forEach(id => $(id).addEventListener('change', () => {
    if ($('tuNgay').value && $('denNgay').value && $('tuNgay').value > $('denNgay').value) return Ui.toast('Từ ngày phải trước hoặc bằng đến ngày.', 'warning');
    bang.tai(1);
  }));
  veRong();
  if (Ui.thamSo('ma')) chon(Ui.thamSo('ma'));
  bang.tai(1);
})();
