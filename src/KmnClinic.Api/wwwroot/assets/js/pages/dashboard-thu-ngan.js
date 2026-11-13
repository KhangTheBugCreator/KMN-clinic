/* Dashboard thu ngân (M8): số liệu hôm nay, hóa đơn chờ thu, chi tiết và thu tiền, biểu đồ số hóa đơn đã thu theo giờ. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const { esc } = ThuNgan;
  const nd = Auth.nguoiDung();
  let bieuDo = null, maChon = null;

  const h = new Date().getHours();
  $('loiChao').textContent = `${h < 11 ? 'Chào buổi sáng' : h < 18 ? 'Chào buổi chiều' : 'Chào buổi tối'}, ${nd.hoTen}`;
  $('homNay').textContent = new Date().toLocaleDateString('vi-VN', { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' });

  const chiTiet = ThuNgan.khungChiTiet($('chiTiet'), { rong: 'Chọn một hóa đơn để xem và thu tiền.', sauThanhToan: () => { taiTongQuan(); taiCho(); } });

  const trieu = n => n >= 1e6 ? (n / 1e6).toLocaleString('vi-VN', { maximumFractionDigits: 2 }) + ' tr' : Ui.fmtTien(n);

  async function taiTongQuan() {
    try {
      const t = await Api.get('thu-ngan/tong-quan');
      $('tqCho').textContent = t.choThanhToan;
      $('tqTienCho').textContent = `Tổng ${Ui.fmtTien(t.tienChoThanhToan)}`;
      $('tqDaThu').textContent = t.daThuHomNay;
      $('tqDoanhThu').textContent = trieu(t.doanhThuHomNay);
      $('tqDoanhThu').title = Ui.fmtTien(t.doanhThuHomNay);
      $('tqCuaToi').textContent = t.doToiThuHomNay;
      veBieuDo(t.theoGio);
    } catch (e) { Ui.loi(e); }
  }

  /** Cột theo giờ: luôn hiện 6h–20h, mở rộng nếu có hóa đơn ngoài khung đó. */
  function veBieuDo(theoGio) {
    if (!window.Chart) return;
    const coDuLieu = theoGio.map((n, i) => n ? i : -1).filter(i => i >= 0);
    const tu = Math.min(6, ...coDuLieu), den = Math.max(20, ...coDuLieu);
    const nhan = [], so = [];
    for (let g = tu; g <= den; g++) { nhan.push(`${g}h`); so.push(theoGio[g]); }
    const xanh = getComputedStyle(document.documentElement).getPropertyValue('--kmn-blue').trim() || '#1f6fd1';
    if (bieuDo) { bieuDo.data.labels = nhan; bieuDo.data.datasets[0].data = so; bieuDo.update(); return; }
    Chart.defaults.font.family = "'Inter', sans-serif";
    bieuDo = new Chart($('chartThuNgan'), {
      type: 'bar',
      data: { labels: nhan, datasets: [{ data: so, backgroundColor: xanh, borderRadius: 6, maxBarThickness: 28 }] },
      options: { maintainAspectRatio: false, plugins: { legend: { display: false }, tooltip: { callbacks: { label: c => `${c.parsed.y} hóa đơn` } } },
        scales: { x: { grid: { display: false } }, y: { beginAtZero: true, ticks: { precision: 0 }, grid: { color: '#eef2f7' } } } }
    });
  }

  async function taiCho() {
    try {
      const kq = await Api.get('thu-ngan/hoa-don?trangThai=' + encodeURIComponent('Chưa thanh toán') + '&pageSize=8');
      $('bang').innerHTML = kq.items.length ? kq.items.map(x => `<tr class="${x.maHoaDon === maChon ? 'table-active' : ''}" data-chon="${esc(x.maHoaDon)}" role="button">
          <td class="mono">${esc(x.maHoaDon)}</td>
          <td><b class="text-navy">${esc(x.tenKhach)}</b><br><small class="text-muted-kmn"><span class="mono">${esc(x.maHoSo)}</span> · ${Ui.fmtNgayGio(x.ngayGioKham)}</small></td>
          <td>${esc(x.tenBacSi)}</td><td class="text-end fw-semibold text-nowrap">${Ui.fmtTien(x.tongTien)}</td>
          <td class="text-end"><button class="btn btn-sm btn-success" data-chon="${esc(x.maHoaDon)}">Thu tiền</button></td></tr>`).join('')
          + (kq.total > kq.items.length ? `<tr><td colspan="5" class="text-center small"><a href="hoa-don-cho-thu.html">Còn ${kq.total - kq.items.length} hóa đơn nữa</a></td></tr>` : '')
        : Ui.dongTrong(5, 'Không có hóa đơn nào chờ thu.', 'check2-circle');
      if (!maChon && kq.items.length) chon(kq.items[0].maHoaDon);
    } catch (e) { $('bang').innerHTML = Ui.dongTrong(5, e.message, 'exclamation-circle'); }
  }

  function chon(ma) {
    maChon = ma;
    $('bang').querySelectorAll('tr[data-chon]').forEach(tr => tr.classList.toggle('table-active', tr.dataset.chon === ma));
    chiTiet.mo(ma);
  }

  $('bang').addEventListener('click', e => {
    const t = e.target.closest('[data-chon]');
    if (!t) return;
    chon(t.dataset.chon);
    if (window.innerWidth < 1200) $('chiTiet').scrollIntoView({ behavior: 'smooth', block: 'start' }); // màn hình hẹp: chi tiết nằm dưới danh sách
  });
  taiTongQuan();
  taiCho();
})();
