/* Quản lý phòng làm việc (M1) – quản trị viên. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  let dsHienTai = [], dsKhoa = [];

  const bang = DanhSach.bangPhanTrang({
    url: 'phong', tbody: $('bang'), phanTrang: $('phanTrang'), soCot: 5, rong: 'Chưa có phòng nào.',
    thamSo: () => ({ q: $('oTim').value.trim(), maKhoa: $('locKhoa').value }),
    sauKhiVe: items => { dsHienTai = items; },
    dong: p => `<tr>
      <td class="mono">${Ui.esc(p.maPhong)}</td>
      <td class="fw-semibold text-navy">${Ui.esc(p.tenPhong)}</td>
      <td>${Ui.esc(p.tenKhoa)}</td>
      <td class="text-center">${p.soLichLamViec}</td>
      <td class="text-end text-nowrap">
        <button class="btn btn-sm btn-soft" data-lich-su="${Ui.esc(p.maPhong)}"><i class="bi bi-clock-history"></i> Lịch sử</button>
        <button class="btn btn-sm btn-soft" data-sua="${Ui.esc(p.maPhong)}"><i class="bi bi-pencil"></i> Sửa</button>
        <button class="btn btn-sm btn-outline-danger" data-xoa="${Ui.esc(p.maPhong)}" ${p.soLichLamViec ? 'disabled title="Phòng đã có lịch làm việc"' : ''}><i class="bi bi-trash"></i></button>
      </td></tr>`
  });

  async function taiKhoa() {
    const kq = await Api.get('khoa?pageSize=100');
    dsKhoa = kq.items;
    $('locKhoa').insertAdjacentHTML('beforeend', dsKhoa.map(k => `<option value="${Ui.esc(k.maKhoa)}">${Ui.esc(k.tenKhoa)}</option>`).join(''));
  }

  function moForm(p) {
    if (!dsKhoa.length) { Ui.toast('Cần tạo khoa trước khi thêm phòng.', 'warning'); return; }
    const khoaKhoa = p && p.soLichLamViec > 0;
    DanhSach.moForm({
      tieuDe: p ? `Sửa phòng ${p.maPhong}` : 'Thêm phòng',
      noiDung: `
        <label class="form-label" for="tenPhong">Tên phòng</label>
        <input id="tenPhong" name="tenPhong" class="form-control mb-3" maxlength="50" placeholder="Ví dụ Phòng 101" required>
        <label class="form-label" for="maKhoa">Khoa</label>
        <select id="maKhoa" name="maKhoa" class="form-select" ${khoaKhoa ? 'disabled' : ''} required>
          <option value="">Chọn khoa</option>${dsKhoa.map(k => `<option value="${Ui.esc(k.maKhoa)}">${Ui.esc(k.tenKhoa)}</option>`).join('')}</select>
        ${khoaKhoa ? `<div class="form-text"><i class="bi bi-lock me-1"></i>Phòng đã có ${p.soLichLamViec} lịch làm việc nên không đổi khoa được (RB11).</div>` : ''}`,
      khiMo: f => { if (p) { f.tenPhong.value = p.tenPhong; f.maKhoa.value = p.maKhoa; } },
      thuThap: f => {
        const body = { tenPhong: f.tenPhong.value.trim(), maKhoa: f.maKhoa.value };
        if (!body.tenPhong) return 'Vui lòng nhập tên phòng.';
        if (!body.maKhoa) return 'Vui lòng chọn khoa.';
        return body;
      },
      gui: async body => {
        await (p ? Api.put('phong/' + encodeURIComponent(p.maPhong), body) : Api.post('phong', body));
        Ui.toast(p ? 'Đã cập nhật phòng.' : 'Đã thêm phòng.');
        bang.tai(p ? undefined : 1);
      }
    });
  }

  /** Modal lịch sử sử dụng phòng: các lịch làm việc đã xếp vào phòng, mới nhất trước. */
  function moLichSu(p) {
    const wrap = document.createElement('div');
    wrap.className = 'modal fade'; wrap.tabIndex = -1;
    wrap.innerHTML = `<div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable"><div class="modal-content rounded-4 border-0">
      <div class="modal-header border-0"><h2 class="modal-title h5">Lịch sử sử dụng · ${Ui.esc(p.tenPhong)}</h2>
        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Đóng"></button></div>
      <div class="modal-body pt-0"><div class="table-responsive"><table class="table table-sm table-kmn mb-2">
        <thead><tr><th>Ngày</th><th>Ca</th><th>Bác sĩ</th><th class="text-center">Lịch hẹn</th></tr></thead><tbody></tbody></table></div>
        <div class="pt"></div></div></div></div>`;
    document.body.appendChild(wrap);
    const ls = DanhSach.bangPhanTrang({
      url: 'phong/' + encodeURIComponent(p.maPhong) + '/lich-su-dung', tbody: wrap.querySelector('tbody'),
      phanTrang: wrap.querySelector('.pt'), soCot: 4, rong: 'Phòng chưa được xếp lịch làm việc nào.',
      dong: l => `<tr><td>${Ui.fmtNgay(l.ngayLamViec)}</td><td>${Ui.esc(l.tenCa)} <small class="text-muted-kmn">${Ui.fmtGio(l.gioBatDau)}–${Ui.fmtGio(l.gioKetThuc)}</small></td>
        <td>${Ui.esc(l.hoTenNv)} <small class="text-muted-kmn mono">${Ui.esc(l.maNv)}</small></td><td class="text-center">${l.soLichHen}</td></tr>`
    });
    wrap.addEventListener('hidden.bs.modal', () => wrap.remove());
    bootstrap.Modal.getOrCreateInstance(wrap).show();
    ls.tai(1);
  }

  $('btnThem').addEventListener('click', () => moForm(null));
  $('oTim').addEventListener('input', Ui.treo(() => bang.tai(1)));
  $('locKhoa').addEventListener('change', () => bang.tai(1));
  $('bang').addEventListener('click', e => {
    const tim = attr => { const b = e.target.closest(`[data-${attr}]`); return b && dsHienTai.find(p => p.maPhong === b.dataset[attr.replace(/-(\w)/, (_, c) => c.toUpperCase())]); };
    const sua = tim('sua'), xoa = tim('xoa'), ls = tim('lich-su');
    if (sua) moForm(sua);
    if (ls) moLichSu(ls);
    if (xoa) DanhSach.xoa({ tieuDe: `Xóa ${xoa.tenPhong}?`, noiDung: 'Thao tác này không hoàn tác được.',
      url: 'phong/' + encodeURIComponent(xoa.maPhong), sauKhiXoa: () => bang.tai() });
  });

  taiKhoa().catch(Ui.loi).finally(() => bang.tai(1));
})();
