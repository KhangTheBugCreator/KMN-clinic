/* Dashboard quản trị (M9): 8 chỉ số, doanh thu theo ngày/tháng, lịch hẹn theo trạng thái, lượt khám, thuốc sắp hết, thuốc xuất nhiều. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const esc = Ui.esc;
  const { kyNhanh, loiKy, moTaKy } = ThongKe;
  const bieuDo = {};
  let kieuDoanhThu = 'ngay';

  const css = getComputedStyle(document.documentElement);
  const MAU = { xanh: css.getPropertyValue('--kmn-blue').trim() || '#1f6fd1', lam: css.getPropertyValue('--kmn-cyan').trim() || '#19a7c6',
    la: css.getPropertyValue('--kmn-green').trim() || '#3a9a5b' };
  Chart.defaults.font.family = "'Inter', sans-serif";
  Chart.defaults.color = '#6b7686';
  const luoi = { color: '#eef2f7' };
  const trieu = n => n >= 1e6 ? (n / 1e6).toLocaleString('vi-VN', { maximumFractionDigits: 2 }) + ' tr' : Number(n).toLocaleString('vi-VN') + ' đ';
  const nhanNgay = s => s.length === 7 ? `${s.slice(5)}/${s.slice(0, 4)}` : `${s.slice(8)}/${s.slice(5, 7)}`;

  function ve(ten, cauHinh) {
    if (bieuDo[ten]) bieuDo[ten].destroy();
    bieuDo[ten] = new Chart($(ten), cauHinh);
  }

  /* ---------- Kỳ ---------- */
  function datKy(ma) {
    if (ma === 'tuy-chon') return;
    const k = kyNhanh(ma);
    $('tuNgay').value = k.tu; $('denNgay').value = k.den;
  }
  const ky = () => ({ tu: $('tuNgay').value, den: $('denNgay').value });
  const thamSo = () => `tuNgay=${ky().tu}&denNgay=${ky().den}`;

  async function taiTatCa() {
    const loi = loiKy(ky().tu, ky().den);
    $('loiKy').classList.toggle('d-none', !loi);
    if (loi) { $('loiKy').textContent = loi; return; }
    $('moTaKy').textContent = 'Kỳ: ' + moTaKy(ky().tu, ky().den);
    $('lienKetBaoCao').href = `bao-cao.html?${thamSo()}`;
    taiTongQuan(); taiDoanhThu(); taiLichHen(); taiLuotKham(); taiThuocKeNhieu();
  }

  async function taiTongQuan() {
    try {
      const t = await Api.get('thong-ke/tong-quan?' + thamSo());
      const dat = (id, v, phu) => { $(id).textContent = v; $(id + 'Phu').textContent = phu; };
      dat('tqNv', t.soNhanVien, `${t.soBacSi} bác sĩ · ${t.soNhanVien - t.soBacSi} nhân viên khác`);
      dat('tqKh', t.soKhachHang, `${t.khachCoKhamTrongKy} khách có khám trong kỳ`);
      dat('tqLh', t.lichHenTrongKy, `${t.lichHenChoXacNhan} lịch đang chờ xác nhận`);
      dat('tqHs', t.hoSoTrongKy, `${t.vangLaiTrongKy} khách vãng lai`);
      dat('tqHd', t.hoaDonDaThanhToan, `${t.hoaDonChoThu} hóa đơn đang chờ thu (${Ui.fmtTien(t.tienChoThu)})`);
      dat('tqDt', trieu(t.doanhThu), 'Chỉ tính hóa đơn đã thanh toán');
      $('tqDt').title = Ui.fmtTien(t.doanhThu);
      const pt = n => t.lichHenTrongKy ? Math.round(n * 100 / t.lichHenTrongKy) + '%' : '—';
      dat('tqTl', pt(t.lichHenDaKham), t.lichHenTrongKy ? `Hủy: ${pt(t.lichHenDaHuy)} · trên ${t.lichHenTrongKy} lịch hẹn` : 'Chưa có lịch hẹn trong kỳ');
      dat('tqTh', t.thuocSapHet, `Tồn ≤ ${t.nguongSapHet}`);
    } catch (e) { Ui.loi(e); }
  }

  async function taiDoanhThu() {
    try {
      const ds = await Api.get(`thong-ke/doanh-thu?kieu=${kieuDoanhThu}&${thamSo()}`);
      ve('chartDoanhThu', {
        type: ds.length > 1 ? 'line' : 'bar',
        data: { labels: ds.map(x => nhanNgay(x.nhan)), datasets: [{ data: ds.map(x => x.doanhThu), borderColor: MAU.xanh, backgroundColor: 'rgba(31,111,209,.12)',
          fill: true, tension: .3, pointRadius: ds.length > 40 ? 0 : 3, pointBackgroundColor: MAU.xanh, borderRadius: 6, maxBarThickness: 48 }] },
        options: { maintainAspectRatio: false, plugins: { legend: { display: false },
            tooltip: { callbacks: { label: c => `${Number(c.parsed.y).toLocaleString('vi-VN')} đ · ${ds[c.dataIndex].soHoaDon} hóa đơn` } } },
          scales: { x: { grid: { display: false } }, y: { beginAtZero: true, grid: luoi, ticks: { callback: trieu } } } }
      });
    } catch (e) { Ui.loi(e); }
  }

  async function taiLichHen() {
    try {
      const d = await Api.get('thong-ke/lich-hen-theo-trang-thai?' + thamSo());
      const nhan = ['Đã đặt', 'Đã xác nhận', 'Đã khám', 'Đã hủy'], so = nhan.map(n => d[n] || 0);
      const tong = so.reduce((a, b) => a + b, 0);
      ve('chartTrangThai', {
        type: 'doughnut',
        data: { labels: nhan, datasets: [{ data: tong ? so : [1], backgroundColor: tong ? ['#f2a33a', MAU.xanh, MAU.la, '#c5ccd6'] : ['#eef2f7'], borderWidth: 0 }] },
        options: { maintainAspectRatio: false, cutout: '68%', plugins: { legend: { display: false }, tooltip: { enabled: !!tong } } }
      });
      $('nhanTrangThai').innerHTML = tong ? nhan.map((n, i) => Ui.badgeTrangThai(n).replace(`>${n}<`, `>${n} ${so[i]}<`)).join('')
        : '<span class="small text-muted-kmn">Chưa có lịch hẹn trong kỳ.</span>';
    } catch (e) { Ui.loi(e); }
  }

  async function taiLuotKham() {
    try {
      const ds = await Api.get('thong-ke/luot-kham?' + thamSo());
      ve('chartLuotKham', {
        type: 'bar',
        data: { labels: ds.map(x => nhanNgay(x.ngay)), datasets: [
          { label: 'Có lịch hẹn', data: ds.map(x => x.coHen), backgroundColor: MAU.xanh, borderRadius: 4, stack: 's' },
          { label: 'Vãng lai', data: ds.map(x => x.vangLai), backgroundColor: MAU.lam, borderRadius: 4, stack: 's' }] },
        options: { maintainAspectRatio: false, plugins: { legend: { display: true, position: 'bottom', labels: { boxWidth: 10 } } },
          scales: { x: { grid: { display: false } }, y: { beginAtZero: true, grid: luoi, ticks: { precision: 0 } } } }
      });
    } catch (e) { Ui.loi(e); }
  }

  async function taiThuocSapHet() {
    try {
      const ds = (await Api.get('thuoc/sap-het?pageSize=5')).items.sort((a, b) => a.soLuongTon - b.soLuongTon);
      const nguong = 20;
      $('thuocSapHet').innerHTML = ds.length ? ds.map(t => `<div class="mb-3"><div class="d-flex justify-content-between small gap-2">
          <b class="text-navy text-truncate">${esc(t.tenThuoc)} ${Number(t.hamLuong).toLocaleString('vi-VN')}</b><span class="text-nowrap">${t.soLuongTon} ${esc(t.donViTinh.toLowerCase())}</span></div>
          <div class="stock-bar"><span style="width:${Math.min(100, Math.round(t.soLuongTon * 100 / nguong))}%"></span></div></div>`).join('')
        : '<div class="empty-state py-3"><i class="bi bi-check2-circle"></i>Không có thuốc sắp hết.</div>';
    } catch (e) { $('thuocSapHet').innerHTML = `<div class="small text-danger">${esc(e.message)}</div>`; }
  }

  async function taiThuocKeNhieu() {
    try {
      const ds = await Api.get('thong-ke/thuoc-ke-nhieu?top=5&' + thamSo());
      $('thuocKeNhieu').innerHTML = ds.length ? ds.map(t => `<tr><td>${esc(t.tenThuoc)} ${Number(t.hamLuong).toLocaleString('vi-VN')}</td>
          <td class="text-end fw-semibold">${t.soLuong.toLocaleString('vi-VN')} <small class="text-muted-kmn fw-normal">${esc(t.donViTinh.toLowerCase())}</small></td></tr>`).join('')
        : Ui.dongTrong(2, 'Chưa có thuốc xuất trong kỳ.');
    } catch (e) { $('thuocKeNhieu').innerHTML = Ui.dongTrong(2, e.message, 'exclamation-circle'); }
  }

  $('kyNhanh').addEventListener('change', () => { datKy($('kyNhanh').value); if ($('kyNhanh').value !== 'tuy-chon') taiTatCa(); });
  ['tuNgay', 'denNgay'].forEach(id => $(id).addEventListener('change', () => { $('kyNhanh').value = 'tuy-chon'; taiTatCa(); }));
  document.querySelectorAll('[data-kieu]').forEach(b => b.addEventListener('click', () => {
    kieuDoanhThu = b.dataset.kieu;
    document.querySelectorAll('[data-kieu]').forEach(x => { x.classList.toggle('btn-primary', x === b); x.classList.toggle('btn-soft', x !== b); });
    taiDoanhThu();
  }));
  datKy('thang-nay');
  taiTatCa();
  taiThuocSapHet();
})();
