/* =========================================================
   thu-ngan.js – hàm dùng chung cho các trang thu ngân (M8):
   khung chi tiết hóa đơn, xác nhận thanh toán, in và xuất PDF A4 (jsPDF).
   Nạp sau api.js, ui.js, layout.js; trang có in/PDF nạp thêm
   vendor/jspdf/jspdf.umd.min.js và jspdf.plugin.autotable.min.js.
   ========================================================= */
(function () {
  'use strict';
  const esc = Ui.esc;
  const pad = n => String(n).padStart(2, '0');
  const homNay = () => { const d = new Date(); return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`; };
  const so = n => (Number(n) || 0).toLocaleString('vi-VN');
  const tenThuoc = t => `${t.tenThuoc} ${Number(t.hamLuong).toLocaleString('vi-VN')}`;
  const daThu = hd => hd.trangThai === 'Đã thanh toán';

  /* ---------- Đọc số tiền bằng chữ ---------- */
  const CS = ['không', 'một', 'hai', 'ba', 'bốn', 'năm', 'sáu', 'bảy', 'tám', 'chín'];
  function docBaSo(g, du) {
    const h = Math.floor(g / 100), t = Math.floor(g / 10) % 10, u = g % 10, s = [];
    if (h > 0 || du) s.push(CS[h] + ' trăm');
    if (t === 0) { if (u > 0 && (h > 0 || du)) s.push('linh'); }
    else s.push(t === 1 ? 'mười' : CS[t] + ' mươi');
    if (u > 0) s.push(u === 1 && t >= 2 ? 'mốt' : u === 5 && t >= 1 ? 'lăm' : CS[u]);
    return s.join(' ');
  }
  /** 254000 → "Hai trăm năm mươi bốn nghìn đồng". Hỗ trợ tới hàng trăm tỷ (đủ cho decimal(12,0)). */
  function docTien(n) {
    n = Math.round(Number(n) || 0);
    if (n === 0) return 'Không đồng';
    const nhom = [];
    while (n > 0) { nhom.push(n % 1000); n = Math.floor(n / 1000); }
    const donVi = ['', ' nghìn', ' triệu', ' tỷ', ' nghìn tỷ'], phan = [];
    for (let i = nhom.length - 1; i >= 0; i--) if (nhom[i]) phan.push(docBaSo(nhom[i], i < nhom.length - 1) + donVi[i]);
    const s = phan.join(' ');
    return s.charAt(0).toUpperCase() + s.slice(1) + ' đồng';
  }

  /* ---------- Khung chi tiết ---------- */
  function chiTiet(hd, chiXem = false) {
    const thieu = !daThu(hd) && hd.thuoc.some(t => t.soLuong > t.soLuongTon);
    return `<div class="panel-title flex-wrap gap-2"><h2 class="mb-0">Hóa đơn <span class="mono">${esc(hd.maHoaDon)}</span></h2>${Ui.badgeTrangThai(hd.trangThai)}</div>
      <div class="small mb-1"><b class="text-navy">${esc(hd.tenKhach)}</b> · ${esc(hd.sdtKhach)} · <span class="mono">${esc(hd.maKh)}</span></div>
      <div class="small text-muted-kmn mb-2">Hồ sơ <span class="mono">${esc(hd.maHoSo)}</span> · ${Ui.fmtNgayGio(hd.ngayGioKham)} · ${esc(hd.tenBacSi)} · ${esc(hd.tenKhoa)}<br>
        Chẩn đoán: ${esc(hd.chanDoanBenh || '')}</div>
      <div class="table-responsive"><table class="table table-sm mb-2">
        <thead class="small text-muted-kmn"><tr><th>Khoản</th><th class="text-center">SL</th><th class="text-end">Đơn giá</th><th class="text-end">Thành tiền</th></tr></thead>
        <tbody class="small">
          <tr><td>Tiền công khám</td><td class="text-center">—</td><td class="text-end">—</td><td class="text-end">${so(hd.tienCongKham)}</td></tr>
          ${hd.thuoc.map(t => `<tr><td>${esc(tenThuoc(t))} <span class="text-muted-kmn">${esc(t.donViTinh)}</span>
            ${!daThu(hd) && t.soLuong > t.soLuongTon ? `<br><span class="text-danger"><i class="bi bi-exclamation-triangle me-1"></i>Kho chỉ còn ${t.soLuongTon}</span>` : ''}</td>
            <td class="text-center">${t.soLuong}</td><td class="text-end">${so(t.donGia)}</td><td class="text-end">${so(t.thanhTien)}</td></tr>`).join('')}
        </tbody>
        <tfoot><tr><th colspan="3">Tổng tiền</th><th class="text-end fs-5 text-primary text-nowrap">${Ui.fmtTien(hd.tongTien)}</th></tr></tfoot>
      </table></div>
      <div class="small text-muted-kmn mb-3 fst-italic">${esc(docTien(hd.tongTien))}</div>
      ${daThu(hd)
        ? `<div class="alert alert-success border-0 rounded-3 small"><i class="bi bi-check2-circle me-1"></i>Đã thu lúc ${Ui.fmtNgayGio(hd.ngayGioThanhToan)} · ${esc(hd.tenNvThu || '')}</div>`
        : chiXem ? '<div class="alert alert-secondary border-0 rounded-3 small"><i class="bi bi-hourglass-split me-1"></i>Hóa đơn đang chờ thu ngân thu tiền.</div>'
        : `${thieu ? '<div class="alert alert-warning border-0 rounded-3 small"><i class="bi bi-exclamation-triangle me-1"></i>Kho không đủ thuốc, chưa thanh toán được. Báo quản trị viên nhập thêm thuốc.</div>' : ''}
          <button class="btn btn-success w-100 mb-2" data-thanh-toan="${esc(hd.maHoaDon)}" ${thieu ? 'disabled' : ''}><i class="bi bi-cash-coin me-1"></i>Xác nhận đã thu ${Ui.fmtTien(hd.tongTien)}</button>`}
      <div class="d-flex gap-2">
        <button class="btn btn-soft flex-fill" data-in="${esc(hd.maHoaDon)}"><i class="bi bi-printer me-1"></i>In</button>
        <button class="btn btn-soft flex-fill" data-pdf="${esc(hd.maHoaDon)}"><i class="bi bi-filetype-pdf me-1"></i>Xuất PDF A4</button></div>`;
  }

  /**
   * Gắn khung chi tiết vào el. Trả { mo(ma), hienTai() }. sauThanhToan(hd) chạy khi thu tiền xong.
   * Nút thanh toán khóa ngay khi bấm; backend vẫn chặn bấm lặp (409).
   * api: tiền tố API đọc hóa đơn ('quan-tri' cho trang quản trị); chiXem: ẩn nút thu tiền.
   */
  function khungChiTiet(el, { sauThanhToan, rong = 'Chọn một hóa đơn để xem chi tiết.', api = 'thu-ngan', chiXem = false } = {}) {
    let hd = null, dangThu = false;
    const veRong = () => { el.innerHTML = `<div class="empty-state"><i class="bi bi-receipt"></i>${esc(rong)}</div>`; };
    async function mo(ma) {
      el.innerHTML = '<div class="skeleton" style="height:280px"></div>';
      try { hd = await Api.get(api + '/hoa-don/' + encodeURIComponent(ma)); el.innerHTML = chiTiet(hd, chiXem); }
      catch (e) { hd = null; el.innerHTML = `<div class="empty-state"><i class="bi bi-exclamation-circle"></i>${esc(e.message)}</div>`; }
    }
    async function thanhToan(b) {
      const ok = await Ui.xacNhan({
        tieuDe: `Xác nhận đã thu ${Ui.fmtTien(hd.tongTien)}?`, nutDongY: 'Đã thu tiền', kieu: 'success', icon: 'cash-coin',
        noiDung: `Hóa đơn ${hd.maHoaDon} của ${hd.tenKhach} sẽ chuyển sang Đã thanh toán và thuốc được trừ khỏi kho trong cùng một lần lưu. Không hoàn tác được.`
      });
      if (!ok) return;
      try {
        hd = await Ui.dangXuLy(b, () => Api.post(`thu-ngan/hoa-don/${encodeURIComponent(hd.maHoaDon)}/thanh-toan`));
        el.innerHTML = chiTiet(hd);
        Ui.toast(`Đã thu ${Ui.fmtTien(hd.tongTien)} cho hóa đơn ${hd.maHoaDon}.`);
        sauThanhToan?.(hd);
      } catch (err) {
        Ui.loi(err);
        if (err.status === 409 || err.status === 400) { await mo(hd.maHoaDon); sauThanhToan?.(hd); }
      }
    }
    el.addEventListener('click', async e => {
      const b = e.target.closest('button');
      if (!b || !hd) return;
      if (b.dataset.thanhToan && !chiXem) {
        if (dangThu) return; // bấm lần hai khi hộp xác nhận hoặc yêu cầu trước chưa xong
        dangThu = true;
        try { await thanhToan(b); } finally { dangThu = false; }
      } else if (b.dataset.in) {
        const w = window.open('', '_blank'); // mở ngay trong lúc bấm để không bị chặn cửa sổ bật lên
        try { const doc = await taoPdf(hd); doc.autoPrint(); if (w) w.location.href = doc.output('bloburl'); else doc.save(tenFile(hd)); }
        catch (err) { w?.close(); Ui.loi(err); }
      } else if (b.dataset.pdf) {
        try { (await Ui.dangXuLy(b, () => taoPdf(hd))).save(tenFile(hd)); } catch (err) { Ui.loi(err); }
      }
    });
    veRong();
    return { mo, hienTai: () => hd, veRong };
  }

  /* ---------- PDF A4 ---------- */
  const tenFile = hd => `hoa-don-${hd.maHoaDon}.pdf`;
  let phong = null;
  async function taiPhong() {
    if (phong) return phong;
    const doc1 = async ten => {
      const res = await fetch(ROOT + 'assets/vendor/jspdf/' + ten);
      if (!res.ok) throw new Error('Không tải được phông chữ cho PDF.');
      const buf = new Uint8Array(await res.arrayBuffer());
      let s = '';
      for (let i = 0; i < buf.length; i += 0x8000) s += String.fromCharCode.apply(null, buf.subarray(i, i + 0x8000));
      return btoa(s);
    };
    phong = Promise.all([doc1('Inter-Regular.ttf'), doc1('Inter-Bold.ttf')]).catch(e => { phong = null; throw e; });
    return phong;
  }

  async function taoPdf(hd) {
    if (!window.jspdf) throw new Error('Thiếu thư viện jsPDF trên trang này.');
    const [thuong, dam] = await taiPhong();
    const doc = new window.jspdf.jsPDF({ unit: 'mm', format: 'a4' });
    doc.addFileToVFS('Inter-Regular.ttf', thuong); doc.addFont('Inter-Regular.ttf', 'Inter', 'normal');
    doc.addFileToVFS('Inter-Bold.ttf', dam); doc.addFont('Inter-Bold.ttf', 'Inter', 'bold');
    const L = 15, R = 195, XANH = [31, 111, 209], XAM = [107, 118, 134], DEN = [22, 38, 66];
    const chu = (kieu, co, mau = DEN) => { doc.setFont('Inter', kieu); doc.setFontSize(co); doc.setTextColor(...mau); };

    // Đầu trang: thông tin phòng khám (từ SITE trong layout.js)
    chu('bold', 15, XANH); doc.text(SITE.fullName, L, 18);
    chu('normal', 9, XAM);
    doc.text(SITE.address, L, 24);
    doc.text(`Điện thoại: ${SITE.phone} · Hotline: ${SITE.hotline} · ${SITE.email}`, L, 29);
    doc.setDrawColor(220, 228, 238); doc.line(L, 33, R, 33);

    chu('bold', 17); doc.text('HÓA ĐƠN THANH TOÁN', 105, 44, { align: 'center' });
    chu('normal', 10, XAM); doc.text(`Số: ${hd.maHoaDon}`, 105, 50, { align: 'center' });

    // Thông tin khách và hồ sơ: 2 cột
    const dong = (x, y, nhan, giaTri, rong = 85) => {
      chu('normal', 9.5, XAM); doc.text(nhan, x, y);
      chu('normal', 9.5); const v = doc.splitTextToSize(giaTri || '—', rong - 26); doc.text(v, x + 26, y);
      return y + 5.2 * v.length;
    };
    let y1 = 60, y2 = 60;
    y1 = dong(L, y1, 'Khách hàng', `${hd.tenKhach} (${hd.maKh})`);
    y1 = dong(L, y1, 'Điện thoại', hd.sdtKhach);
    y1 = dong(L, y1, 'Giới tính', `${hd.gioiTinh} · Sinh ngày ${Ui.fmtNgay(hd.ngaySinh)}`);
    y1 = dong(L, y1, 'Địa chỉ', hd.diaChi);
    y2 = dong(110, y2, 'Hồ sơ khám', `${hd.maHoSo} · ${Ui.fmtNgayGio(hd.ngayGioKham)}`);
    y2 = dong(110, y2, 'Bác sĩ', hd.tenBacSi);
    y2 = dong(110, y2, 'Chuyên khoa', hd.tenKhoa);
    y2 = dong(110, y2, 'Chẩn đoán', hd.chanDoanBenh);

    const than = [['1', 'Tiền công khám', '', '1', so(hd.tienCongKham), so(hd.tienCongKham)],
      ...hd.thuoc.map((t, i) => [String(i + 2), `${tenThuoc(t)}\n${t.lieuDung}`, t.donViTinh, String(t.soLuong), so(t.donGia), so(t.thanhTien)])];
    doc.autoTable({
      startY: Math.max(y1, y2) + 4, margin: { left: L, right: 210 - R },
      head: [['STT', 'Khoản mục', 'ĐVT', 'SL', 'Đơn giá (đ)', 'Thành tiền (đ)']], body: than,
      foot: [['', 'Tổng cộng', '', '', '', so(hd.tongTien)]],
      styles: { font: 'Inter', fontSize: 9.5, cellPadding: 2.2, textColor: DEN, lineColor: [226, 232, 240], lineWidth: 0.2 },
      headStyles: { fillColor: [234, 243, 252], textColor: XANH, fontStyle: 'bold' },
      footStyles: { fillColor: [255, 255, 255], textColor: DEN, fontStyle: 'bold', fontSize: 11 },
      columnStyles: { 0: { halign: 'center', cellWidth: 12 }, 2: { cellWidth: 16 }, 3: { halign: 'center', cellWidth: 12 },
        4: { halign: 'right', cellWidth: 28 }, 5: { halign: 'right', cellWidth: 32 } },
      didParseCell: d => { if (d.column.index >= 4) d.cell.styles.halign = 'right'; else if (d.column.index === 3 || (d.section === 'head' && d.column.index === 0)) d.cell.styles.halign = 'center'; }
    });
    let y = doc.lastAutoTable.finalY + 7;
    chu('normal', 9.5, XAM); doc.text('Bằng chữ:', L, y);
    chu('bold', 9.5); doc.text(doc.splitTextToSize(docTien(hd.tongTien), R - L - 20), L + 20, y);
    y += 7;
    if (daThu(hd)) {
      chu('normal', 9.5, XAM); doc.text('Trạng thái:', L, y);
      chu('bold', 9.5, [47, 125, 71]); doc.text(`Đã thanh toán lúc ${Ui.fmtNgayGio(hd.ngayGioThanhToan)}`, L + 20, y);
    } else {
      chu('normal', 9.5, XAM); doc.text('Trạng thái:', L, y);
      chu('bold', 9.5, [180, 53, 46]); doc.text('Chưa thanh toán', L + 20, y);
    }
    if (hd.loiDan) { y += 6; chu('normal', 9.5, XAM); doc.text('Lời dặn:', L, y); chu('normal', 9.5); const v = doc.splitTextToSize(hd.loiDan, R - L - 20); doc.text(v, L + 20, y); y += 5 * (v.length - 1); }

    // Chữ ký
    y = Math.min(y + 16, 250);
    chu('bold', 10); doc.text('Khách hàng', 55, y, { align: 'center' }); doc.text('Thu ngân', 155, y, { align: 'center' });
    chu('normal', 8.5, XAM); doc.text('(Ký, ghi rõ họ tên)', 55, y + 5, { align: 'center' }); doc.text('(Ký, ghi rõ họ tên)', 155, y + 5, { align: 'center' });
    if (hd.tenNvThu) { chu('normal', 10); doc.text(hd.tenNvThu, 155, y + 24, { align: 'center' }); }

    chu('normal', 8, XAM);
    const n = new Date();
    doc.text(`In lúc ${Ui.fmtNgay(n)} ${pad(n.getHours())}:${pad(n.getMinutes())} · ${SITE.name}`, 105, 287, { align: 'center' });
    doc.setProperties({ title: `Hóa đơn ${hd.maHoaDon}` });
    return doc;
  }

  window.ThuNgan = { esc, homNay, so, docTien, chiTiet, khungChiTiet, taoPdf };
})();
