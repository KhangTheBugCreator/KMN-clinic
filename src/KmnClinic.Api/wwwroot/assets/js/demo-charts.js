/* =========================================================
   demo-charts.js – biểu đồ mẫu cho bản thiết kế.
   Số liệu ở đây là DỮ LIỆU MẪU. Ở Giai đoạn 4 số liệu lấy
   từ API /api/thong-ke/... (truy vấn database thật).
   ========================================================= */
(function () {
  if (!window.Chart) return;
  const css = getComputedStyle(document.documentElement);
  const BLUE = css.getPropertyValue('--kmn-blue').trim();
  const CYAN = css.getPropertyValue('--kmn-cyan').trim();
  const GREEN = css.getPropertyValue('--kmn-green').trim();
  Chart.defaults.font.family = "'Inter', sans-serif";
  Chart.defaults.color = '#6b7686';
  Chart.defaults.plugins.legend.display = false;
  const grid = { color: '#eef2f7' };
  const vnd = v => (v / 1e6).toLocaleString('vi-VN') + ' tr';

  const el = id => document.getElementById(id);

  if (el('chartDoanhThu')) {
    const days = Array.from({ length: 14 }, (_, i) => `${i + 29 > 31 ? i - 2 : i + 29}/${i + 29 > 31 ? 11 : 10}`);
    const data = [2.1, 1.8, 2.6, 3.1, 1.2, 0.9, 2.8, 3.4, 2.2, 2.9, 3.6, 1.4, 1.1, 2.5].map(x => x * 1e6);
    new Chart(el('chartDoanhThu'), {
      type: 'line',
      data: { labels: days, datasets: [{ data, borderColor: BLUE, backgroundColor: 'rgba(31,111,209,.10)', fill: true, tension: .35, pointRadius: 3, pointBackgroundColor: BLUE }] },
      options: { maintainAspectRatio: false, scales: { x: { grid: { display: false } }, y: { grid, ticks: { callback: vnd } } },
        plugins: { tooltip: { callbacks: { label: c => c.parsed.y.toLocaleString('vi-VN') + ' đ' } } } }
    });
  }

  if (el('chartTrangThai')) {
    new Chart(el('chartTrangThai'), {
      type: 'doughnut',
      data: { labels: ['Đã đặt', 'Đã xác nhận', 'Đã khám', 'Đã hủy'],
        datasets: [{ data: [6, 21, 166, 19], backgroundColor: ['#f2a33a', BLUE, GREEN, '#c5ccd6'], borderWidth: 0 }] },
      options: { maintainAspectRatio: false, cutout: '68%' }
    });
  }

  if (el('chartLuotKham')) {
    new Chart(el('chartLuotKham'), {
      type: 'bar',
      data: { labels: ['T5', 'T6', 'T7', 'CN', 'T2', 'T3', 'T4'],
        datasets: [
          { label: 'Có lịch hẹn', data: [9, 11, 7, 4, 12, 10, 8], backgroundColor: BLUE, borderRadius: 6, stack: 's' },
          { label: 'Vãng lai', data: [3, 2, 4, 2, 3, 1, 2], backgroundColor: CYAN, borderRadius: 6, stack: 's' }] },
      options: { maintainAspectRatio: false, plugins: { legend: { display: true, position: 'bottom', labels: { boxWidth: 10 } } },
        scales: { x: { grid: { display: false } }, y: { grid, ticks: { precision: 0 } } } }
    });
  }

  if (el('chartThuNgan')) {
    new Chart(el('chartThuNgan'), {
      type: 'bar',
      data: { labels: ['7h', '8h', '9h', '10h', '11h', '13h', '14h', '15h', '16h'],
        datasets: [{ data: [0, 2, 4, 3, 2, 1, 3, 2, 0], backgroundColor: GREEN, borderRadius: 6 }] },
      options: { maintainAspectRatio: false, scales: { x: { grid: { display: false } }, y: { grid, ticks: { precision: 0 } } } }
    });
  }
})();
