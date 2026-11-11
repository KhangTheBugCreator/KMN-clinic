/* =====================================================================
   DỮ LIỆU MẪU ĐỂ PHÁT TRIỂN VÀ KIỂM THỬ – KHÔNG DÙNG CHO PHÒNG KHÁM THẬT
   ---------------------------------------------------------------------
   - Chỉ INSERT dữ liệu vào các bảng có sẵn, không đổi cấu trúc database.
   - Tên người, số điện thoại, thuốc đều là dữ liệu giả để chạy thử.
   - Mật khẩu đang lưu dạng chuỗi gốc theo quyết định Q5 (tạm thời).
   - Chạy được nhiều lần: bản ghi đã có (theo mã) sẽ được bỏ qua.
   Tài khoản thử (mật khẩu chung: matkhau123):
     Quản trị viên 0900000001 · Lễ tân 0900000002 · Bác sĩ 0900000003, 0900000004
     Thu ngân 0900000005 · Khách hàng 0911111111 (có tài khoản)
     Khách vãng lai 0922222222 (không có mật khẩu)
   ===================================================================== */
USE PHONGKHAM;
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

/* Khoa */
INSERT INTO Khoa (ma_khoa, ten_khoa, mo_ta)
SELECT v.ma, v.ten, v.mo_ta FROM (VALUES
  ('K0001', N'Nội tổng quát', N'Khám và điều trị các bệnh nội khoa thường gặp.'),
  ('K0002', N'Nhi',           N'Khám bệnh cho trẻ em.'),
  ('K0003', N'Tai Mũi Họng',  N'Khám các bệnh về tai, mũi, họng.')
) v(ma, ten, mo_ta) WHERE NOT EXISTS (SELECT 1 FROM Khoa k WHERE k.ma_khoa = v.ma);

/* Phòng làm việc */
INSERT INTO Phong_lam_viec (ma_phong, ten_phong, ma_khoa)
SELECT v.ma, v.ten, v.khoa FROM (VALUES
  ('P0001', N'Phòng 101', 'K0001'),
  ('P0002', N'Phòng 102', 'K0001'),
  ('P0003', N'Phòng 201', 'K0002'),
  ('P0004', N'Phòng 301', 'K0003')
) v(ma, ten, khoa) WHERE NOT EXISTS (SELECT 1 FROM Phong_lam_viec p WHERE p.ma_phong = v.ma);

/* Ca làm việc */
INSERT INTO Ca_lam_viec (ma_ca, ten_ca, gio_bat_dau, gio_ket_thuc)
SELECT v.ma, v.ten, v.bd, v.kt FROM (VALUES
  ('CA0001', N'Ca sáng',  CAST('07:00' AS time), CAST('11:30' AS time)),
  ('CA0002', N'Ca chiều', CAST('13:00' AS time), CAST('17:00' AS time))
) v(ma, ten, bd, kt) WHERE NOT EXISTS (SELECT 1 FROM Ca_lam_viec c WHERE c.ma_ca = v.ma);

/* Nhân viên: mỗi chức vụ ít nhất một người; bác sĩ phải có khoa (CK_Nhan_vien_khoa) */
INSERT INTO Nhan_vien (ma_nv, ho_ten_nv, ngay_sinh_nv, sdt_nv, chuc_vu, mat_khau_nv, ma_khoa, duong_dan_anh)
SELECT v.ma, v.ten, v.ns, v.sdt, v.cv, 'matkhau123', v.khoa, NULL FROM (VALUES
  ('NV0001', N'Quản trị Mẫu',  CAST('1985-03-10' AS date), '0900000001', N'Quản trị viên', NULL),
  ('NV0002', N'Lễ tân Mẫu',    CAST('1995-07-21' AS date), '0900000002', N'Lễ tân',        NULL),
  ('NV0003', N'Bác sĩ Mẫu A',  CAST('1980-11-02' AS date), '0900000003', N'Bác sĩ',        'K0001'),
  ('NV0004', N'Bác sĩ Mẫu B',  CAST('1988-05-14' AS date), '0900000004', N'Bác sĩ',        'K0002'),
  ('NV0005', N'Thu ngân Mẫu',  CAST('1993-09-30' AS date), '0900000005', N'Thu ngân',      NULL)
) v(ma, ten, ns, sdt, cv, khoa) WHERE NOT EXISTS (SELECT 1 FROM Nhan_vien n WHERE n.ma_nv = v.ma);

/* Khách hàng: một người có tài khoản, một khách vãng lai (mat_khau_kh NULL) */
INSERT INTO Khach_hang (ma_kh, ho_ten_kh, ngay_sinh_kh, gioi_tinh_kh, sdt_kh, dia_chi_kh, mat_khau_kh)
SELECT v.ma, v.ten, v.ns, v.gt, v.sdt, v.dc, v.mk FROM (VALUES
  ('KH0001', N'Khách hàng Mẫu', CAST('1992-01-15' AS date), N'Nam', '0911111111', N'Vĩnh Long', 'matkhau123'),
  ('KH0002', N'Khách vãng lai Mẫu', CAST('1970-06-01' AS date), N'Nữ', '0922222222', NULL, NULL)
) v(ma, ten, ns, gt, sdt, dc, mk) WHERE NOT EXISTS (SELECT 1 FROM Khach_hang k WHERE k.ma_kh = v.ma);

/* Thuốc */
INSERT INTO Thuoc (ma_thuoc, ten_thuoc, nsx, ham_luong, don_vi_tinh, don_gia_thuoc, so_luong_ton, mo_ta)
SELECT v.ma, v.ten, v.nsx, v.hl, v.dvt, v.gia, v.ton, NULL FROM (VALUES
  ('TH0001', N'Paracetamol', N'Nhà sản xuất mẫu A', 500, N'Viên', 2000,  200),
  ('TH0002', N'Amoxicillin', N'Nhà sản xuất mẫu B', 500, N'Viên', 5000,  150),
  ('TH0003', N'Vitamin C',   N'Nhà sản xuất mẫu A', 500, N'Viên', 1500,  18),
  ('TH0004', N'Oresol',      N'Nhà sản xuất mẫu C', 4.1, N'Gói',  3000,  60)
) v(ma, ten, nsx, hl, dvt, gia, ton) WHERE NOT EXISTS (SELECT 1 FROM Thuoc t WHERE t.ma_thuoc = v.ma);

COMMIT;
PRINT N'Đã nạp dữ liệu mẫu phát triển.';
