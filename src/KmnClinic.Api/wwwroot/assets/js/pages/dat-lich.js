/* Đặt lịch khám (M5): chuyên khoa → bác sĩ → ngày & ca → triệu chứng → xác nhận.
   Nhận sẵn lựa chọn từ URL: ?maKhoa=, ?maNv=, ?maNv=&maLich= (từ trang chủ, chuyên khoa, bác sĩ). */
(function () {
  const { esc, homNay, congNgay, ngayNgan, ngayDai, gio, veAnh, hopLoi } = CongKhai;
  const $ = id => document.getElementById(id);
  const SO_NGAY = 14;
  const THU = ['CN', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7'];

  const chon = { khoa: null, bacSi: null, ngay: null, lich: null };
  let buoc = 1, dsKhoa = [], dsBacSi = [], lichBacSi = [], modal, nd;

  /* ---------- Điều hướng giữa các bước ---------- */
  const duocToi = b => b === 1 || (b === 2 && chon.khoa) || (b === 3 && chon.bacSi) || (b >= 4 && chon.lich);

  function denBuoc(b) {
    if (!duocToi(b)) return;
    buoc = b;
    document.querySelectorAll('.buoc').forEach(el => el.classList.toggle('d-none', +el.dataset.buoc !== b));
    document.querySelectorAll('#stepper .st').forEach(el => {
      const n = +el.dataset.buoc;
      el.classList.toggle('active', n === b);
      el.classList.toggle('done', n < b);
      el.querySelector('.dot').innerHTML = n < b ? '<i class="bi bi-check-lg"></i>' : n;
      el.style.cursor = n < b ? 'pointer' : '';
    });
    $('nutLui').style.visibility = b > 1 ? 'visible' : 'hidden';
    $('nutTiep').innerHTML = b === 4 ? 'Kiểm tra thông tin <i class="bi bi-arrow-right ms-1"></i>' : 'Tiếp tục <i class="bi bi-arrow-right ms-1"></i>';
    $('nutTiep').disabled = !duocToi(b + 1);
    veTomTat();
    if (b === 2) veBacSi();
    if (b === 3) veNgayCa();
  }

  /* ---------- Tóm tắt bên phải ---------- */
  function veTomTat() {
    const dong = (nhan, gt) => `<div class="summary-row"><span>${nhan}</span><span>${gt || '<span class="text-muted-kmn fw-normal">Chưa chọn</span>'}</span></div>`;
    const l = chon.lich;
    $('tomTat').innerHTML =
      dong('Chuyên khoa', chon.khoa && esc(chon.khoa.tenKhoa)) +
      dong('Bác sĩ', chon.bacSi && esc(chon.bacSi.hoTen)) +
      dong('Ngày', l && ngayDai(l.ngay)) +
      dong('Ca', l && `${esc(l.tenCa)} · ${gio(l)}`) +
      dong('Phòng', l && esc(l.tenPhong)) +
      dong('Khách hàng', `${esc(nd.hoTen)}<br><small class="fw-normal text-muted-kmn">${esc(nd.soDienThoai || '')}</small>`);
  }

  /* ---------- Bước 1: chuyên khoa ---------- */
  function veKhoa() {
    $('dsKhoa').innerHTML = dsKhoa.length ? dsKhoa.map(k => `<div class="col-md-6">
        <div class="pick-card ${chon.khoa?.maKhoa === k.maKhoa ? 'selected' : ''} ${k.soBacSi ? '' : 'disabled'}" data-khoa="${esc(k.maKhoa)}"
          role="button" tabindex="0" ${k.soBacSi ? '' : 'aria-disabled="true" title="Khoa chưa có bác sĩ"'}>
          <div class="d-flex justify-content-between"><b class="text-navy">${esc(k.tenKhoa)}</b>
            ${chon.khoa?.maKhoa === k.maKhoa ? '<i class="bi bi-check-circle-fill text-primary"></i>' : ''}</div>
          <div class="small text-muted-kmn mt-1">${k.moTa ? esc(k.moTa) : 'Chưa có mô tả.'}</div>
          <div class="small mt-2"><i class="bi bi-person-badge me-1"></i>${k.soBacSi} bác sĩ</div></div></div>`).join('')
      : '<div class="col-12"><div class="empty-state"><i class="bi bi-diagram-3"></i>Chưa có chuyên khoa.</div></div>';
  }

  function chonKhoa(ma) {
    const k = dsKhoa.find(x => x.maKhoa === ma);
    if (!k || !k.soBacSi) return;
    if (chon.khoa?.maKhoa !== ma) { chon.bacSi = null; chon.ngay = null; chon.lich = null; dsBacSi = []; }
    chon.khoa = k;
    veKhoa();
    denBuoc(2);
  }

  /* ---------- Bước 2: bác sĩ ---------- */
  async function veBacSi() {
    const el = $('dsBacSi');
    if (!dsBacSi.length || dsBacSi[0].maKhoa !== chon.khoa.maKhoa) {
      el.innerHTML = '<div class="col-12"><div class="skeleton" style="height:120px"></div></div>';
      try {
        dsBacSi = (await Api.get(`cong-khai/bac-si?maKhoa=${encodeURIComponent(chon.khoa.maKhoa)}&pageSize=24`)).items;
      } catch (e) {
        el.innerHTML = hopLoi(e, true);
        el.querySelector('[data-thu-lai]').addEventListener('click', veBacSi);
        return;
      }
    }
    el.innerHTML = dsBacSi.length ? dsBacSi.map(b => {
      const sel = chon.bacSi?.maNv === b.maNv, gan = b.lichSapToi[0];
      return `<div class="col-md-6"><div class="pick-card ${sel ? 'selected' : ''}" data-bac-si="${esc(b.maNv)}" role="button" tabindex="0">
        <div class="d-flex gap-3 align-items-center"><img class="avatar" alt="" data-avatar="${esc(b.duongDanAnh || '')}">
          <div class="flex-grow-1"><b class="text-navy">${esc(b.hoTen)}</b><br><span class="small text-muted-kmn">${esc(b.tenKhoa)}</span></div>
          ${sel ? '<i class="bi bi-check-circle-fill text-primary"></i>' : ''}</div>
        <div class="small mt-2 ${gan ? '' : 'text-muted-kmn'}">${gan ? `<i class="bi bi-calendar-check me-1 text-primary"></i>Lịch gần nhất: ${ngayNgan(gan.ngay)} · ${esc(gan.tenCa)}`
          : '<i class="bi bi-calendar-x me-1"></i>Chưa có lịch trong 7 ngày tới'}</div></div></div>`;
    }).join('') : '<div class="col-12"><div class="empty-state"><i class="bi bi-person-badge"></i>Khoa này chưa có bác sĩ.</div></div>';
    veAnh(el);
  }

  function chonBacSi(b) {
    if (chon.bacSi?.maNv !== b.maNv) { chon.ngay = null; chon.lich = null; lichBacSi = null; }
    chon.bacSi = b;
    denBuoc(3);
  }

  /* ---------- Bước 3: ngày & ca ---------- */
  async function taiLichBacSi() {
    const b = await Api.get('cong-khai/bac-si/' + encodeURIComponent(chon.bacSi.maNv));
    lichBacSi = b.lichSapToi;
    return b;
  }

  async function veNgayCa() {
    if (!lichBacSi) {
      $('dsNgay').innerHTML = '<div class="skeleton w-100" style="height:70px"></div>';
      $('dsCa').innerHTML = '';
      try { await taiLichBacSi(); } catch (e) {
        $('dsNgay').innerHTML = hopLoi(e, true);
        $('dsNgay').querySelector('[data-thu-lai]').addEventListener('click', veNgayCa);
        return;
      }
    }
    if (!lichBacSi.length) {
      $('dsNgay').innerHTML = `<div class="empty-state w-100"><i class="bi bi-calendar-x"></i>Bác sĩ chưa có lịch còn đặt được trong 14 ngày tới.
        <div class="mt-2"><button type="button" class="btn btn-soft btn-sm" data-ve-buoc="2">Chọn bác sĩ khác</button></div></div>`;
      $('dsCa').innerHTML = '';
      return;
    }
    const coLich = new Set(lichBacSi.map(l => l.ngay));
    if (!chon.ngay || !coLich.has(chon.ngay)) chon.ngay = lichBacSi[0].ngay;
    const ngay = Array.from({ length: SO_NGAY }, (_, i) => congNgay(homNay(), i));
    $('dsNgay').innerHTML = ngay.map(d => {
      const [y, m, dd] = d.split('-').map(Number);
      const co = coLich.has(d);
      return `<div class="date-pill ${d === chon.ngay ? 'selected' : ''} ${co ? '' : 'off'}" ${co ? `data-ngay="${d}" role="button" tabindex="0"` : 'title="Bác sĩ không có lịch còn đặt được"'}>
        <small>${d === homNay() ? 'Hôm nay' : THU[new Date(y, m - 1, dd).getDay()]}</small><div class="d">${dd}</div><small>Th${m}</small></div>`;
    }).join('');
    $('dsNgay').querySelector('.selected')?.scrollIntoView({ block: 'nearest', inline: 'nearest' });

    const ca = lichBacSi.filter(l => l.ngay === chon.ngay);
    $('dsCa').innerHTML = ca.map(l => {
      const sel = chon.lich?.maLichLamViec === l.maLichLamViec;
      const icon = l.gioBatDau < '12:00' ? 'brightness-high text-warning' : l.gioBatDau < '17:00' ? 'sunset text-primary' : 'moon-stars text-primary';
      return `<div class="col-md-6"><div class="pick-card ${sel ? 'selected' : ''}" data-lich="${esc(l.maLichLamViec)}" role="button" tabindex="0">
        <div class="d-flex justify-content-between"><b class="text-navy"><i class="bi bi-${icon} me-1"></i>${esc(l.tenCa)}</b>
          ${sel ? '<i class="bi bi-check-circle-fill text-primary"></i>' : ''}</div>
        <div class="small text-muted-kmn mt-1">${gio(l)} · ${esc(l.tenPhong)}</div></div></div>`;
    }).join('');
  }

  function chonLich(ma) {
    chon.lich = lichBacSi.find(l => l.maLichLamViec === ma) || null;
    veNgayCa();
    veTomTat();
    $('nutTiep').disabled = !chon.lich;
  }

  /* ---------- Bước 5: xác nhận và gửi ---------- */
  function moXacNhan() {
    const l = chon.lich;
    $('xnMoTa').textContent = `${chon.bacSi.hoTen} · ${ngayDai(l.ngay)} · ${l.tenCa} ${gio(l)} · ${l.tenPhong}`;
    modal.show();
  }

  async function datLich() {
    try {
      const kq = await Ui.dangXuLy($('nutDatLich'), () => Api.post('lich-hen-cua-toi', {
        maLichLamViec: chon.lich.maLichLamViec,
        trieuChungBanDau: $('trieuChung').value.trim() || null
      }));
      modal.hide();
      document.querySelectorAll('.buoc').forEach(el => el.classList.add('d-none'));
      $('nutDieuHuong').classList.add('d-none');
      document.querySelectorAll('#stepper .st').forEach(el => { el.classList.remove('active'); el.classList.add('done'); el.querySelector('.dot').innerHTML = '<i class="bi bi-check-lg"></i>'; });
      $('tcMa').textContent = kq.maLichHen;
      $('tcMoTa').textContent = `${kq.tenBacSi} · ${ngayDai(kq.ngayKham)} · ${kq.tenCa} ${Ui.fmtGio(kq.gioBatDau)}–${Ui.fmtGio(kq.gioKetThuc)} · ${kq.tenPhong}`;
      $('thanhCong').classList.remove('d-none');
      history.replaceState(null, '', location.pathname);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    } catch (e) {
      modal.hide();
      Ui.loi(e);
      // Ca vừa bắt đầu hoặc lịch làm việc không còn: tải lại lịch của bác sĩ và cho chọn lại
      if (e.status === 400 || e.status === 404) {
        chon.lich = null; lichBacSi = null;
        denBuoc(3);
      }
    }
  }

  /* ---------- Khởi tạo ---------- */
  async function apDungThamSo() {
    const maNv = Ui.thamSo('maNv'), maLich = Ui.thamSo('maLich'), maKhoa = Ui.thamSo('maKhoa');
    if (maNv) {
      try {
        const b = await Api.get('cong-khai/bac-si/' + encodeURIComponent(maNv));
        chon.khoa = dsKhoa.find(k => k.maKhoa === b.maKhoa) || null;
        if (chon.khoa) {
          chon.bacSi = b;
          lichBacSi = b.lichSapToi;
          const l = maLich && lichBacSi.find(x => x.maLichLamViec === maLich);
          if (l) { chon.lich = l; chon.ngay = l.ngay; }
          else if (maLich) Ui.toast('Ca đã chọn không còn đặt được. Vui lòng chọn ca khác.', 'warning');
          veKhoa();
          denBuoc(l ? 4 : 3);
          return;
        }
      } catch (e) { Ui.toast('Không tìm thấy bác sĩ đã chọn. Vui lòng chọn lại.', 'warning'); }
    }
    if (maKhoa && dsKhoa.some(k => k.maKhoa === maKhoa && k.soBacSi)) { chonKhoa(maKhoa); return; }
    denBuoc(1);
  }

  async function taiKhoa() {
    try {
      dsKhoa = await Api.get('cong-khai/khoa');
    } catch (e) {
      $('dsKhoa').innerHTML = hopLoi(e, true);
      $('dsKhoa').querySelector('[data-thu-lai]').addEventListener('click', taiKhoa);
      return;
    }
    veKhoa();
    await apDungThamSo();
  }

  /** Bấm chuột hoặc Enter/Space trên thẻ chọn. */
  function khiChon(el, chonDuoc) {
    const xuLy = e => {
      const t = chonDuoc(e.target);
      if (t) { e.preventDefault(); t(); }
    };
    el.addEventListener('click', xuLy);
    el.addEventListener('keydown', e => { if (e.key === 'Enter' || e.key === ' ') xuLy(e); });
  }

  document.addEventListener('DOMContentLoaded', () => {
    nd = Auth.nguoiDung();
    if (!nd || nd.vaiTro !== 'KhachHang') {
      const quayLai = encodeURIComponent(location.pathname + location.search);
      $('linkChonVaiTro').href = `chon-vai-tro.html?vaiTro=KhachHang&quayLai=${quayLai}`;
      if (nd) $('canVaiTroMoTa').textContent = `Bạn đang giả lập vai trò ${nd.chucVu || 'nhân viên'} (${nd.hoTen}). Lịch hẹn chỉ đặt được bằng hồ sơ khách hàng.`;
      $('canVaiTro').classList.remove('d-none');
      return;
    }
    $('loiChao').innerHTML = `Xin chào <b class="text-navy">${esc(nd.hoTen)}</b>. Lịch hẹn sẽ ở trạng thái <span class="badge-st st-dat">Đã đặt</span> cho tới khi lễ tân gọi xác nhận.`;
    $('khungDatLich').classList.remove('d-none');
    modal = new bootstrap.Modal($('confirmModal'));

    khiChon($('dsKhoa'), t => { const c = t.closest('[data-khoa]'); return c && (() => chonKhoa(c.dataset.khoa)); });
    khiChon($('dsBacSi'), t => {
      const c = t.closest('[data-bac-si]');
      return c && (() => chonBacSi(dsBacSi.find(b => b.maNv === c.dataset.bacSi)));
    });
    khiChon($('dsNgay'), t => {
      const c = t.closest('[data-ngay]'), v = t.closest('[data-ve-buoc]');
      if (v) return () => denBuoc(+v.dataset.veBuoc);
      return c && (() => { chon.ngay = c.dataset.ngay; if (chon.lich?.ngay !== chon.ngay) chon.lich = null; chonLich(chon.lich?.maLichLamViec); });
    });
    khiChon($('dsCa'), t => { const c = t.closest('[data-lich]'); return c && (() => chonLich(c.dataset.lich)); });
    $('stepper').addEventListener('click', e => { const s = e.target.closest('.st.done'); if (s) denBuoc(+s.dataset.buoc); });

    $('nutLui').addEventListener('click', () => denBuoc(buoc - 1));
    $('nutTiep').addEventListener('click', () => buoc === 4 ? moXacNhan() : denBuoc(buoc + 1));
    $('nutDatLich').addEventListener('click', datLich);
    $('trieuChung').addEventListener('input', () => { $('demKyTu').textContent = $('trieuChung').value.length; });

    veTomTat();
    taiKhoa();
  });

})();
