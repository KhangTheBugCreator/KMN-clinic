/* Quản lý ca làm việc (M1) – quản trị viên. */
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  let ds = [];

  async function tai() {
    $('bang').innerHTML = Ui.dongDangTai(6);
    try {
      ds = await Api.get('ca');
      $('bang').innerHTML = ds.length ? ds.map(c => `<tr>
        <td class="mono">${Ui.esc(c.maCa)}</td>
        <td class="fw-semibold text-navy">${Ui.esc(c.tenCa)}</td>
        <td>${Ui.fmtGio(c.gioBatDau)}</td><td>${Ui.fmtGio(c.gioKetThuc)}</td>
        <td class="text-center">${c.soLichLamViec}</td>
        <td class="text-end text-nowrap">
          <button class="btn btn-sm btn-soft" data-sua="${Ui.esc(c.maCa)}"><i class="bi bi-pencil"></i> Sửa</button>
          <button class="btn btn-sm btn-outline-danger" data-xoa="${Ui.esc(c.maCa)}" ${c.soLichLamViec ? 'disabled title="Ca đã được xếp lịch"' : ''}><i class="bi bi-trash"></i></button>
        </td></tr>`).join('') : Ui.dongTrong(6, 'Chưa có ca làm việc nào.');
    } catch (err) { $('bang').innerHTML = Ui.dongTrong(6, err.message, 'exclamation-circle'); }
  }

  function moForm(c) {
    const khoaGio = c && c.soLichLamViec > 0;
    DanhSach.moForm({
      tieuDe: c ? `Sửa ca ${c.maCa}` : 'Thêm ca làm việc',
      noiDung: `
        <label class="form-label" for="tenCa">Tên ca</label>
        <input id="tenCa" name="tenCa" class="form-control mb-3" maxlength="20" placeholder="Ví dụ Ca sáng" required>
        <div class="row g-3">
          <div class="col-6"><label class="form-label" for="gioBatDau">Giờ bắt đầu</label>
            <input id="gioBatDau" name="gioBatDau" type="time" class="form-control" ${khoaGio ? 'disabled' : ''} required></div>
          <div class="col-6"><label class="form-label" for="gioKetThuc">Giờ kết thúc</label>
            <input id="gioKetThuc" name="gioKetThuc" type="time" class="form-control" ${khoaGio ? 'disabled' : ''} required></div>
        </div>
        ${khoaGio ? `<div class="form-text"><i class="bi bi-lock me-1"></i>Ca đã dùng trong ${c.soLichLamViec} lịch làm việc nên chỉ đổi được tên. Cần giờ khác thì tạo ca mới.</div>` : ''}`,
      khiMo: f => { if (c) { f.tenCa.value = c.tenCa; f.gioBatDau.value = c.gioBatDau; f.gioKetThuc.value = c.gioKetThuc; } },
      thuThap: f => {
        const body = { tenCa: f.tenCa.value.trim(), gioBatDau: f.gioBatDau.value, gioKetThuc: f.gioKetThuc.value };
        if (!body.tenCa) return 'Vui lòng nhập tên ca.';
        if (!body.gioBatDau || !body.gioKetThuc) return 'Vui lòng nhập đủ giờ bắt đầu và giờ kết thúc.';
        if (body.gioBatDau >= body.gioKetThuc) return 'Giờ bắt đầu ca phải nhỏ hơn giờ kết thúc.';
        return body;
      },
      gui: async body => {
        await (c ? Api.put('ca/' + encodeURIComponent(c.maCa), body) : Api.post('ca', body));
        Ui.toast(c ? 'Đã cập nhật ca.' : 'Đã thêm ca.');
        tai();
      }
    });
  }

  $('btnThem').addEventListener('click', () => moForm(null));
  $('bang').addEventListener('click', e => {
    const sua = e.target.closest('[data-sua]'), xoa = e.target.closest('[data-xoa]');
    if (sua) moForm(ds.find(c => c.maCa === sua.dataset.sua));
    if (xoa) {
      const c = ds.find(x => x.maCa === xoa.dataset.xoa);
      DanhSach.xoa({ tieuDe: `Xóa ${c.tenCa}?`, noiDung: 'Thao tác này không hoàn tác được.', url: 'ca/' + encodeURIComponent(c.maCa), sauKhiXoa: tai });
    }
  });
  tai();
})();
