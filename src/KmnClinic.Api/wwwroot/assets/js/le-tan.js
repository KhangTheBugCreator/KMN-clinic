/* =========================================================
   le-tan.js – hàm dùng chung cho các trang lễ tân (M6):
   vẽ dòng lịch hẹn, xác nhận, hủy, tiếp nhận khách có hẹn.
   Nạp sau api.js, ui.js, layout.js, danh-sach.js.
   ========================================================= */
(function () {
  'use strict';
  const esc = Ui.esc;
  const pad = n => String(n).padStart(2, '0');
  const homNay = () => { const d = new Date(); return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`; };
  const gio = h => `${Ui.fmtGio(h.gioBatDau)}–${Ui.fmtGio(h.gioKetThuc)}`;

  /** Nút thao tác theo trạng thái. Tiếp nhận chỉ trong đúng ngày hẹn (Q10). */
  function nutThaoTac(h) {
    const ma = esc(h.maLichHen), dungNgay = h.ngayKham === homNay();
    const huy = `<button class="btn btn-sm btn-outline-danger" data-lh-huy="${ma}" title="Hủy lịch hẹn"><i class="bi bi-x-lg"></i></button>`;
    switch (h.trangThai) {
      case 'Đã đặt':
        return `<button class="btn btn-sm btn-soft" data-lh-xac-nhan="${ma}">Xác nhận</button>
          ${dungNgay ? `<button class="btn btn-sm btn-primary" data-lh-tiep-nhan="${ma}" title="Xác nhận rồi tạo hồ sơ khám trong một thao tác">Tiếp nhận</button>` : ''} ${huy}`;
      case 'Đã xác nhận':
        return `${dungNgay ? `<button class="btn btn-sm btn-primary" data-lh-tiep-nhan="${ma}">Tiếp nhận</button>`
          : '<span class="small text-muted-kmn me-1">Tiếp nhận vào ngày hẹn</span>'} ${huy}`;
      case 'Đã khám':
        return h.maHoSo ? `<span class="small text-muted-kmn">Hồ sơ <b class="mono">${esc(h.maHoSo)}</b></span>` : '';
      default: return '<span class="small text-muted-kmn">—</span>';
    }
  }

  /**
   * Một dòng lịch hẹn. hienNgay: thêm ngày khám vào cột bác sĩ (khi xem nhiều ngày).
   * gon: bỏ cột triệu chứng, ghi triệu chứng dưới tên khách (bảng hẹp ở dashboard).
   */
  function dongLichHen(h, hienNgay, gon) {
    const trieuChung = h.trieuChungBanDau ? esc(h.trieuChungBanDau) : '<span class="text-muted-kmn">—</span>';
    return `<tr>
      <td class="mono">${esc(h.maLichHen)}</td>
      <td><div class="person"><div><b>${esc(h.tenKhach)}</b><small>${esc(h.sdtKhach)}</small>
        ${gon && h.trieuChungBanDau ? `<small class="text-muted-kmn d-block"><i class="bi bi-chat-left-text me-1"></i>${trieuChung}</small>` : ''}</div></div></td>
      <td>${esc(h.tenBacSi)}<br><small class="text-muted-kmn">${hienNgay ? Ui.fmtNgay(h.ngayKham) + ' · ' : ''}${esc(h.tenCa)} ${gio(h)} · ${esc(h.tenPhong)}${gon ? '' : ' · ' + esc(h.tenKhoa)}</small></td>
      ${gon ? '' : `<td class="small">${trieuChung}</td>`}
      <td>${Ui.badgeTrangThai(h.trangThai)}${h.tenNvXacNhan ? `<br><small class="text-muted-kmn">LT: ${esc(h.tenNvXacNhan)}</small>` : ''}</td>
      <td class="text-end text-nowrap">${nutThaoTac(h)}</td></tr>`;
  }

  /** Form tiếp nhận khách có hẹn: triệu chứng lấy sẵn từ triệu chứng ban đầu, lễ tân sửa nếu cần. */
  function moTiepNhan(h, sauKhi) {
    DanhSach.moForm({
      tieuDe: (h.trangThai === 'Đã đặt' ? 'Xác nhận và tiếp nhận ' : 'Tiếp nhận ') + h.maLichHen,
      nutLuu: 'Tạo hồ sơ khám',
      noiDung: `<div class="summary-box mb-3 p-3" style="position:static">
          <div class="d-flex justify-content-between small"><span class="text-muted-kmn">Khách hàng</span><b class="text-navy">${esc(h.tenKhach)} · ${esc(h.sdtKhach)}</b></div>
          <div class="d-flex justify-content-between small mt-1"><span class="text-muted-kmn">Bác sĩ</span><b class="text-navy">${esc(h.tenBacSi)} · ${esc(h.tenKhoa)}</b></div>
          <div class="d-flex justify-content-between small mt-1"><span class="text-muted-kmn">Ca, phòng</span><b class="text-navy">${esc(h.tenCa)} ${gio(h)} · ${esc(h.tenPhong)}</b></div></div>
        ${h.trangThai === 'Đã đặt' ? '<div class="alert alert-info small border-0 rounded-3"><i class="bi bi-info-circle me-1"></i>Lịch hẹn chưa xác nhận: hệ thống sẽ xác nhận rồi tạo hồ sơ trong cùng một thao tác.</div>' : ''}
        <label class="form-label" for="trieuChung">Triệu chứng</label>
        <textarea id="trieuChung" name="trieuChung" class="form-control" rows="3" maxlength="255" required></textarea>
        <div class="form-text">Bác sĩ sẽ ghi chẩn đoán, lời dặn và tiền công khám khi khám.</div>`,
      khiMo: f => { f.trieuChung.value = h.trieuChungBanDau || ''; },
      thuThap: f => {
        const trieuChung = f.trieuChung.value.trim();
        return trieuChung ? { trieuChung } : 'Vui lòng nhập triệu chứng.';
      },
      gui: async body => {
        const hs = await Api.post(`le-tan/lich-hen/${encodeURIComponent(h.maLichHen)}/tiep-nhan`, body);
        Ui.toast(`Đã tạo hồ sơ ${hs.maHoSo} cho ${hs.tenKhach}, chuyển tới ${hs.tenBacSi}.`);
        sauKhi?.();
      }
    });
  }

  /** Gắn xử lý nút cho bảng lịch hẹn. layDs(): danh sách đang hiện; taiLai(): vẽ lại. */
  function ganThaoTac(tbody, layDs, taiLai) {
    tbody.addEventListener('click', async e => {
      const b = e.target.closest('[data-lh-xac-nhan],[data-lh-huy],[data-lh-tiep-nhan]');
      if (!b) return;
      const ma = b.dataset.lhXacNhan || b.dataset.lhHuy || b.dataset.lhTiepNhan;
      const h = layDs().find(x => x.maLichHen === ma);
      if (!h) return;
      if (b.dataset.lhTiepNhan) return moTiepNhan(h, taiLai);
      if (b.dataset.lhXacNhan) {
        try {
          await Ui.dangXuLy(b, () => Api.post(`le-tan/lich-hen/${encodeURIComponent(ma)}/xac-nhan`));
          Ui.toast(`Đã xác nhận lịch hẹn ${ma}.`);
        } catch (err) { Ui.loi(err); }
        return taiLai();
      }
      const dongY = await Ui.xacNhan({
        tieuDe: `Hủy lịch hẹn ${ma}?`, kieu: 'danger', icon: 'calendar-x', nutDongY: 'Hủy lịch hẹn',
        noiDung: `${h.tenKhach} · ${h.tenBacSi} · ${Ui.fmtNgay(h.ngayKham)} ${h.tenCa}. Thao tác này không hoàn tác được.`
      });
      if (!dongY) return;
      try {
        await Api.post(`le-tan/lich-hen/${encodeURIComponent(ma)}/huy`);
        Ui.toast(`Đã hủy lịch hẹn ${ma}.`);
      } catch (err) { Ui.loi(err); }
      taiLai();
    });
  }

  /** Thẻ bác sĩ đang trực. */
  const theBacSiTruc = b => `<div class="queue-item"><img class="avatar" alt="" data-avatar="${esc(b.duongDanAnh || '')}" style="width:40px;height:40px">
    <div class="flex-grow-1"><b class="text-navy">${esc(b.hoTen)}</b><br><small class="text-muted-kmn">${esc(b.tenKhoa)} · ${esc(b.tenPhong)} · ${esc(b.tenCa)} ${gio(b)}</small></div>
    <span class="small text-muted-kmn text-nowrap">${b.soChoKham} chờ khám</span></div>`;

  const veAnh = el => el.querySelectorAll('img[data-avatar]').forEach(img => renderAvatar(img, img.dataset.avatar));

  window.LeTan = { homNay, dongLichHen, ganThaoTac, moTiepNhan, theBacSiTruc, veAnh, gio };
})();
