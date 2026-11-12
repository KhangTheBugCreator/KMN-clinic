/* Quản lý khoa (M1) – quản trị viên. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  let dsHienTai = [];

  const bang = DanhSach.bangPhanTrang({
    url: 'khoa', tbody: $('bang'), phanTrang: $('phanTrang'), soCot: 6, rong: 'Chưa có khoa nào.',
    thamSo: () => ({ q: $('oTim').value.trim() }),
    sauKhiVe: items => { dsHienTai = items; },
    dong: k => `<tr>
      <td class="mono">${Ui.esc(k.maKhoa)}</td>
      <td class="fw-semibold text-navy">${Ui.esc(k.tenKhoa)}</td>
      <td class="small text-muted-kmn">${Ui.esc(k.moTa || '—')}</td>
      <td class="text-center">${k.soPhong}</td><td class="text-center">${k.soBacSi}</td>
      <td class="text-end text-nowrap">
        <button class="btn btn-sm btn-soft" data-sua="${Ui.esc(k.maKhoa)}"><i class="bi bi-pencil"></i> Sửa</button>
        <button class="btn btn-sm btn-outline-danger" data-xoa="${Ui.esc(k.maKhoa)}" ${k.soPhong || k.soBacSi ? 'disabled title="Khoa còn phòng hoặc bác sĩ"' : ''}><i class="bi bi-trash"></i></button>
      </td></tr>`
  });

  function moForm(k) {
    DanhSach.moForm({
      tieuDe: k ? `Sửa khoa ${k.maKhoa}` : 'Thêm khoa',
      noiDung: `
        <label class="form-label" for="tenKhoa">Tên khoa</label>
        <input id="tenKhoa" name="tenKhoa" class="form-control mb-3" maxlength="50" required>
        <label class="form-label" for="moTa">Mô tả <small class="text-muted-kmn">(không bắt buộc)</small></label>
        <textarea id="moTa" name="moTa" class="form-control" rows="3" maxlength="255"></textarea>`,
      khiMo: f => { if (k) { f.tenKhoa.value = k.tenKhoa; f.moTa.value = k.moTa || ''; } },
      thuThap: f => {
        const body = { tenKhoa: f.tenKhoa.value.trim(), moTa: f.moTa.value.trim() || null };
        return body.tenKhoa ? body : 'Vui lòng nhập tên khoa.';
      },
      gui: async body => {
        await (k ? Api.put('khoa/' + encodeURIComponent(k.maKhoa), body) : Api.post('khoa', body));
        Ui.toast(k ? 'Đã cập nhật khoa.' : 'Đã thêm khoa.');
        bang.tai(k ? undefined : 1);
      }
    });
  }

  $('btnThem').addEventListener('click', () => moForm(null));
  $('oTim').addEventListener('input', Ui.treo(() => bang.tai(1)));
  $('bang').addEventListener('click', e => {
    const sua = e.target.closest('[data-sua]'), xoa = e.target.closest('[data-xoa]');
    if (sua) moForm(dsHienTai.find(k => k.maKhoa === sua.dataset.sua));
    if (xoa) {
      const k = dsHienTai.find(x => x.maKhoa === xoa.dataset.xoa);
      DanhSach.xoa({ tieuDe: `Xóa khoa ${k.tenKhoa}?`, noiDung: 'Thao tác này không hoàn tác được.',
        url: 'khoa/' + encodeURIComponent(k.maKhoa), sauKhiXoa: () => bang.tai() });
    }
  });
  bang.tai(1);
})();
