/* =========================================================
   thong-ke.js – dùng chung cho dashboard quản trị và trang báo cáo (M9):
   chọn kỳ (khoảng ngày), định nghĩa 5 báo cáo, xuất Excel bằng ExcelJS.
   ========================================================= */
(function () {
  'use strict';
  const esc = Ui.esc;
  const pad = n => String(n).padStart(2, '0');
  const iso = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  const TOI_DA_NGAY = 366;

  /** Các kỳ chọn nhanh. Trả { tu, den } dạng yyyy-MM-dd. */
  function kyNhanh(ma) {
    const n = new Date(), y = n.getFullYear(), m = n.getMonth();
    switch (ma) {
      case 'thang-truoc': return { tu: iso(new Date(y, m - 1, 1)), den: iso(new Date(y, m, 0)) };
      case '7-ngay': return { tu: iso(new Date(y, m, n.getDate() - 6)), den: iso(n) };
      case '30-ngay': return { tu: iso(new Date(y, m, n.getDate() - 29)), den: iso(n) };
      case 'nam-nay': return { tu: `${y}-01-01`, den: `${y}-12-31` };
      case 'hom-nay': return { tu: iso(n), den: iso(n) };
      default: return { tu: iso(new Date(y, m, 1)), den: iso(new Date(y, m + 1, 0)) }; // tháng này
    }
  }

  const soNgay = (tu, den) => Math.round((new Date(den + 'T00:00') - new Date(tu + 'T00:00')) / 864e5) + 1;
  /** Kiểm tra kỳ ở trình duyệt (API cũng kiểm tra). Trả câu lỗi hoặc null. */
  function loiKy(tu, den) {
    if (!tu || !den) return 'Vui lòng chọn đủ từ ngày và đến ngày.';
    if (tu > den) return 'Từ ngày phải trước hoặc bằng đến ngày.';
    if (soNgay(tu, den) > TOI_DA_NGAY) return `Khoảng thời gian tối đa ${TOI_DA_NGAY} ngày.`;
    return null;
  }
  const moTaKy = (tu, den) => tu === den ? Ui.fmtNgay(tu) : `${Ui.fmtNgay(tu)} – ${Ui.fmtNgay(den)}`;

  /* ---------- 5 báo cáo ----------
     kieu: tien (#,##0 đ), so (#,##0), ngay, ngaygio, gio, chu. tong: cộng cột ở dòng Tổng. */
  const gioCa = x => `${Ui.fmtGio(x.gioBatDau)}–${Ui.fmtGio(x.gioKetThuc)}`;
  const BAO_CAO = {
    'doanh-thu': {
      ten: 'Doanh thu', icon: 'cash-stack', moTa: 'Hóa đơn đã thanh toán trong kỳ, theo ngày thanh toán.',
      cot: [
        { nhan: 'Mã hóa đơn', lay: x => x.maHoaDon, rong: 13 },
        { nhan: 'Ngày thanh toán', lay: x => x.ngayGioThanhToan, kieu: 'ngaygio', rong: 18 },
        { nhan: 'Khách hàng', lay: x => `${x.tenKhach} (${x.maKh})`, rong: 30 },
        { nhan: 'Bác sĩ', lay: x => x.tenBacSi, rong: 24 },
        { nhan: 'Khoa', lay: x => x.tenKhoa, rong: 20 },
        { nhan: 'Tiền công khám', lay: x => x.tienCongKham, kieu: 'tien', tong: true, rong: 16 },
        { nhan: 'Tiền thuốc', lay: x => x.tienThuoc, kieu: 'tien', tong: true, rong: 16 },
        { nhan: 'Tổng tiền', lay: x => x.tongTien, kieu: 'tien', tong: true, rong: 16 },
        { nhan: 'Thu ngân', lay: x => x.tenThuNgan, rong: 22 }],
      nhanTong: 'Tổng doanh thu'
    },
    'luot-kham': {
      ten: 'Lượt khám', icon: 'journal-medical', moTa: 'Hồ sơ khám theo ngày, khoa và bác sĩ; tách có lịch hẹn và vãng lai.',
      cot: [
        { nhan: 'Ngày', lay: x => x.ngay, kieu: 'ngay', rong: 13 },
        { nhan: 'Khoa', lay: x => x.tenKhoa, rong: 22 },
        { nhan: 'Bác sĩ', lay: x => `${x.tenBacSi} (${x.maNv})`, rong: 30 },
        { nhan: 'Có lịch hẹn', lay: x => x.coHen, kieu: 'so', tong: true, rong: 13 },
        { nhan: 'Vãng lai', lay: x => x.vangLai, kieu: 'so', tong: true, rong: 12 },
        { nhan: 'Tổng lượt', lay: x => x.tong, kieu: 'so', tong: true, rong: 12 }],
      nhanTong: 'Tổng'
    },
    'lich-hen': {
      ten: 'Lịch hẹn', icon: 'calendar-check', moTa: 'Lịch hẹn có ngày khám trong kỳ, kèm số lượng từng trạng thái.',
      cot: [
        { nhan: 'Mã lịch hẹn', lay: x => x.maLichHen, rong: 13 },
        { nhan: 'Đặt lúc', lay: x => x.ngayGioDat, kieu: 'ngaygio', rong: 18 },
        { nhan: 'Khách hàng', lay: x => x.tenKhach, rong: 26 },
        { nhan: 'Số điện thoại', lay: x => x.sdtKhach, rong: 14 },
        { nhan: 'Bác sĩ', lay: x => x.tenBacSi, rong: 24 },
        { nhan: 'Khoa', lay: x => x.tenKhoa, rong: 20 },
        { nhan: 'Ngày khám', lay: x => x.ngayKham, kieu: 'ngay', rong: 13 },
        { nhan: 'Ca', lay: x => `${x.tenCa} ${gioCa(x)}`, rong: 22 },
        { nhan: 'Trạng thái', lay: x => x.trangThai, rong: 14 }],
      demTheo: { nhan: 'Số lượng theo trạng thái', lay: x => x.trangThai, thuTu: ['Đã đặt', 'Đã xác nhận', 'Đã khám', 'Đã hủy'] }
    },
    'ton-kho': {
      ten: 'Thuốc tồn kho', icon: 'capsule', moTa: 'Tồn kho tại thời điểm xuất, ít nhất ở đầu. Không theo kỳ.', khongKy: true,
      cot: [
        { nhan: 'Mã thuốc', lay: x => x.maThuoc, rong: 12 },
        { nhan: 'Tên thuốc', lay: x => x.tenThuoc, rong: 28 },
        { nhan: 'Nhà sản xuất', lay: x => x.nsx, rong: 26 },
        { nhan: 'Hàm lượng', lay: x => Number(x.hamLuong), kieu: 'thapphan', rong: 12 },
        { nhan: 'Đơn vị tính', lay: x => x.donViTinh, rong: 12 },
        { nhan: 'Đơn giá', lay: x => x.donGia, kieu: 'tien', rong: 14 },
        { nhan: 'Tồn kho', lay: x => x.soLuongTon, kieu: 'so', rong: 11 },
        { nhan: 'Cảnh báo', lay: x => x.canhBao, rong: 12, canhBao: true }]
    },
    'su-dung-thuoc': {
      ten: 'Sử dụng thuốc', icon: 'prescription2', moTa: 'Thuốc đã xuất theo đơn của hóa đơn đã thanh toán trong kỳ, đơn giá lúc kê.',
      cot: [
        { nhan: 'Mã thuốc', lay: x => x.maThuoc, rong: 12 },
        { nhan: 'Tên thuốc', lay: x => `${x.tenThuoc} ${Number(x.hamLuong).toLocaleString('vi-VN')}`, rong: 30 },
        { nhan: 'Đơn vị tính', lay: x => x.donViTinh, rong: 12 },
        { nhan: 'Số đơn', lay: x => x.soDon, kieu: 'so', tong: true, rong: 10 },
        { nhan: 'Tổng số lượng xuất', lay: x => x.tongSoLuong, kieu: 'so', rong: 18 },
        { nhan: 'Thành tiền', lay: x => x.thanhTien, kieu: 'tien', tong: true, rong: 16 }],
      nhanTong: 'Tổng'
    }
  };

  /** Định dạng một ô cho bảng xem trước trên trang. */
  function oHtml(c, x) {
    const v = c.lay(x);
    switch (c.kieu) {
      case 'tien': return Ui.fmtTien(v);
      case 'so': return Number(v).toLocaleString('vi-VN');
      case 'thapphan': return Number(v).toLocaleString('vi-VN');
      case 'ngay': return Ui.fmtNgay(v);
      case 'ngaygio': return Ui.fmtNgayGio(v);
      default: return c.canhBao && v ? `<span class="badge-st ${v === 'Hết hàng' ? 'st-chua-tt' : 'st-dat'}">${esc(v)}</span>` : esc(v);
    }
  }
  const canPhai = c => ['tien', 'so', 'thapphan'].includes(c.kieu);
  const tong = (dinhNghia, dong) => dinhNghia.cot.map(c => c.tong ? dong.reduce((s, x) => s + Number(c.lay(x) || 0), 0) : null);
  const demTheo = (dinhNghia, dong) => {
    const d = dinhNghia.demTheo;
    if (!d) return null;
    const m = new Map(d.thuTu.map(t => [t, 0]));
    dong.forEach(x => m.set(d.lay(x), (m.get(d.lay(x)) || 0) + 1));
    return [...m.entries()];
  };

  /** Đổi chuỗi ngày (giờ) từ API thành Date giữ đúng giờ địa phương cho Excel. */
  function ngayExcel(v, coGio) {
    if (!v) return null;
    const d = coGio ? new Date(v) : new Date(v + 'T00:00:00');
    // ExcelJS ghi Date theo UTC: dời múi giờ để Excel hiện đúng giờ đã thấy trên web
    return new Date(d.getTime() - d.getTimezoneOffset() * 60000);
  }

  /** Dựng và tải file .xlsx. bc = { tieuDe, tuNgay, denNgay, lapLuc, dong }. */
  async function xuatExcel(loai, bc) {
    if (!window.ExcelJS) throw new Error('Thiếu thư viện ExcelJS trên trang này.');
    const dn = BAO_CAO[loai], soCot = dn.cot.length;
    const wb = new ExcelJS.Workbook();
    wb.creator = SITE.name; wb.created = new Date();
    const ws = wb.addWorksheet(dn.ten, { views: [{ state: 'frozen', ySplit: 5 }], pageSetup: { paperSize: 9, orientation: soCot > 6 ? 'landscape' : 'portrait', fitToPage: true, fitToWidth: 1, fitToHeight: 0 } });
    ws.columns = dn.cot.map(c => ({ width: c.rong || 14 }));
    const XANH = 'FF1F6FD1', NEN = 'FFEAF3FC', VIEN = { style: 'thin', color: { argb: 'FFD9E2EC' } };
    const tron = (dongSo, giaTri, kieu) => {
      ws.mergeCells(dongSo, 1, dongSo, soCot);
      const o = ws.getCell(dongSo, 1); o.value = giaTri; Object.assign(o, kieu);
    };
    tron(1, SITE.fullName, { font: { bold: true, size: 12, color: { argb: XANH } } });
    tron(2, bc.tieuDe.toUpperCase(), { font: { bold: true, size: 15 }, alignment: { horizontal: 'center' } });
    tron(3, (bc.tuNgay ? `Kỳ: ${moTaKy(bc.tuNgay, bc.denNgay)} · ` : '') + `Lập lúc ${Ui.fmtNgayGio(bc.lapLuc)}`, { font: { italic: true, color: { argb: 'FF6B7686' } }, alignment: { horizontal: 'center' } });

    const dau = ws.getRow(5);
    dn.cot.forEach((c, i) => {
      const o = dau.getCell(i + 1);
      o.value = c.nhan;
      o.font = { bold: true, color: { argb: XANH } };
      o.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: NEN } };
      o.alignment = { horizontal: canPhai(c) ? 'right' : 'left', vertical: 'middle', wrapText: true };
      o.border = { top: VIEN, bottom: VIEN, left: VIEN, right: VIEN };
    });
    dau.height = 22;
    const dinhDang = c => ({ tien: '#,##0" đ"', so: '#,##0', thapphan: '#,##0.##', ngay: 'dd/mm/yyyy', ngaygio: 'dd/mm/yyyy hh:mm' }[c.kieu]);
    bc.dong.forEach(x => {
      const r = ws.addRow(dn.cot.map(c => c.kieu === 'ngay' ? ngayExcel(c.lay(x), false) : c.kieu === 'ngaygio' ? ngayExcel(c.lay(x), true) : c.lay(x)));
      dn.cot.forEach((c, i) => {
        const o = r.getCell(i + 1);
        if (dinhDang(c)) o.numFmt = dinhDang(c);
        o.border = { bottom: VIEN };
        if (c.canhBao && o.value) o.font = { bold: true, color: { argb: o.value === 'Hết hàng' ? 'FFB4352E' : 'FFB06D0D' } };
      });
    });
    if (!bc.dong.length) tron(6, 'Không có dữ liệu trong kỳ.', { font: { italic: true }, alignment: { horizontal: 'center' } });

    const tongCot = tong(dn, bc.dong);
    if (tongCot.some(v => v != null) && bc.dong.length) {
      const r = ws.addRow(tongCot.map((v, i) => i === 0 ? (dn.nhanTong || 'Tổng') : v));
      r.font = { bold: true };
      dn.cot.forEach((c, i) => { const o = r.getCell(i + 1); if (dinhDang(c)) o.numFmt = dinhDang(c); o.border = { top: { style: 'medium', color: { argb: XANH } } }; });
    }
    const dem = demTheo(dn, bc.dong);
    if (dem) {
      ws.addRow([]);
      const t = ws.addRow([dn.demTheo.nhan]); t.font = { bold: true, color: { argb: XANH } };
      dem.forEach(([k, v]) => ws.addRow([k, v]));
      ws.addRow(['Tổng', bc.dong.length]).font = { bold: true };
    }
    ws.addRow([]);
    ws.addRow([`Số dòng: ${bc.dong.length}. ${dn.moTa}`]).font = { italic: true, size: 9, color: { argb: 'FF6B7686' } };

    const buf = await wb.xlsx.writeBuffer();
    const a = document.createElement('a');
    a.href = URL.createObjectURL(new Blob([buf], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }));
    a.download = `bao-cao-${loai}${bc.tuNgay ? `-${bc.tuNgay}-den-${bc.denNgay}` : '-' + iso(new Date())}.xlsx`;
    document.body.appendChild(a); a.click(); a.remove();
    setTimeout(() => URL.revokeObjectURL(a.href), 10000);
  }

  window.ThongKe = { iso, kyNhanh, loiKy, moTaKy, BAO_CAO, oHtml, canPhai, tong, demTheo, xuatExcel };
})();
