/* Báo cáo Excel (M9): chọn 1 trong 5 báo cáo và kỳ, xem trước 100 dòng đầu, xuất .xlsx bằng ExcelJS. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const esc = Ui.esc;
  const { BAO_CAO, kyNhanh, loiKy, moTaKy, oHtml, canPhai, tong, demTheo, xuatExcel } = ThongKe;
  const XEM_TOI_DA = 100;
  let loai = Ui.thamSo('loai') in BAO_CAO ? Ui.thamSo('loai') : 'doanh-thu';

  $('dsBaoCao').innerHTML = Object.entries(BAO_CAO).map(([ma, b]) => `<div class="col-sm-6 col-xl">
      <div class="pick-card h-100" data-loai="${ma}" role="button" tabindex="0">
        <div class="d-flex gap-2 align-items-center mb-1"><span class="icon-circle ic-blue" style="width:34px;height:34px"><i class="bi bi-${b.icon}"></i></span>
          <b class="text-navy">${esc(b.ten)}</b></div>
        <div class="small text-muted-kmn">${esc(b.moTa)}</div></div></div>`).join('');

  function chonLoai(ma) {
    loai = ma;
    document.querySelectorAll('[data-loai]').forEach(x => x.classList.toggle('selected', x.dataset.loai === ma));
    const khongKy = !!BAO_CAO[ma].khongKy;
    ['kyNhanh', 'tuNgay', 'denNgay'].forEach(id => { $(id).disabled = khongKy; });
    xem();
  }

  const ky = () => ({ tu: $('tuNgay').value, den: $('denNgay').value });
  function kiemTra() {
    const loi = BAO_CAO[loai].khongKy ? null : loiKy(ky().tu, ky().den);
    $('loiKy').classList.toggle('d-none', !loi);
    $('loiKy').textContent = loi || '';
    return !loi;
  }
  const url = () => 'bao-cao/' + loai + (BAO_CAO[loai].khongKy ? '' : `?tuNgay=${ky().tu}&denNgay=${ky().den}`);

  async function xem() {
    if (!kiemTra()) return;
    const dn = BAO_CAO[loai];
    $('dauBang').innerHTML = `<tr><th>#</th>${dn.cot.map(c => `<th class="${canPhai(c) ? 'text-end' : ''}">${esc(c.nhan)}</th>`).join('')}</tr>`;
    $('bang').innerHTML = Ui.dongDangTai(dn.cot.length + 1);
    $('chanBang').innerHTML = ''; $('ghiChu').textContent = '';
    try {
      const bc = await Api.get(url());
      $('tieuDeXem').textContent = bc.tieuDe;
      $('moTaXem').textContent = (bc.tuNgay ? `Kỳ ${moTaKy(bc.tuNgay, bc.denNgay)} · ` : 'Tại thời điểm xem · ') + `${bc.dong.length} dòng`;
      $('bang').innerHTML = bc.dong.length
        ? bc.dong.slice(0, XEM_TOI_DA).map((x, i) => `<tr><td class="text-muted-kmn">${i + 1}</td>${dn.cot.map(c => `<td class="${canPhai(c) ? 'text-end text-nowrap' : ''}">${oHtml(c, x)}</td>`).join('')}</tr>`).join('')
        : Ui.dongTrong(dn.cot.length + 1, 'Không có dữ liệu trong kỳ.');
      const t = tong(dn, bc.dong);
      if (bc.dong.length && t.some(v => v != null))
        $('chanBang').innerHTML = `<tr><th></th>${dn.cot.map((c, i) => i === 0 ? `<th class="text-nowrap">${esc(dn.nhanTong || 'Tổng')}</th>`
          : `<th class="text-end text-nowrap">${t[i] == null ? '' : c.kieu === 'tien' ? Ui.fmtTien(t[i]) : t[i].toLocaleString('vi-VN')}</th>`).join('')}</tr>`;
      const dem = demTheo(dn, bc.dong);
      $('ghiChu').innerHTML = [
        bc.dong.length > XEM_TOI_DA ? `Đang xem ${XEM_TOI_DA} dòng đầu; file Excel có đủ ${bc.dong.length} dòng.` : '',
        dem ? dem.map(([k, v]) => `${esc(k)}: <b>${v}</b>`).join(' · ') : ''].filter(Boolean).join('<br>');
    } catch (e) { $('bang').innerHTML = Ui.dongTrong(dn.cot.length + 1, e.message, 'exclamation-circle'); }
  }

  async function xuat() {
    if (!kiemTra()) return;
    try {
      await Ui.dangXuLy($('btnXuat'), async () => xuatExcel(loai, await Api.get(url())));
      Ui.toast('Đã tạo file Excel.');
    } catch (e) { Ui.loi(e); }
  }

  const chon = e => {
    const c = e.target.closest('[data-loai]');
    if (!c || (e.type === 'keydown' && e.key !== 'Enter' && e.key !== ' ')) return;
    e.preventDefault();
    chonLoai(c.dataset.loai);
  };
  $('dsBaoCao').addEventListener('click', chon);
  $('dsBaoCao').addEventListener('keydown', chon);
  $('kyNhanh').addEventListener('change', () => {
    if ($('kyNhanh').value === 'tuy-chon') return;
    const k = kyNhanh($('kyNhanh').value); $('tuNgay').value = k.tu; $('denNgay').value = k.den; xem();
  });
  ['tuNgay', 'denNgay'].forEach(id => $(id).addEventListener('change', () => { $('kyNhanh').value = 'tuy-chon'; xem(); }));
  $('btnXem').addEventListener('click', xem);
  $('btnXuat').addEventListener('click', xuat);

  if (Ui.thamSo('tuNgay') && Ui.thamSo('denNgay')) { $('tuNgay').value = Ui.thamSo('tuNgay'); $('denNgay').value = Ui.thamSo('denNgay'); $('kyNhanh').value = 'tuy-chon'; }
  else { const k = kyNhanh('thang-nay'); $('tuNgay').value = k.tu; $('denNgay').value = k.den; }
  chonLoai(loai);
})();
