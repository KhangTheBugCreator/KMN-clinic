/* =====================================================================
   DỮ LIỆU MẪU ĐẦY ĐỦ ĐỂ TRÌNH DIỄN WEBSITE – KHÔNG PHẢI DỮ LIỆU THẬT
   ---------------------------------------------------------------------
   - Chỉ INSERT/UPDATE vào các bảng có sẵn, không đổi cấu trúc database.
   - Họ tên, số điện thoại, địa chỉ là dữ liệu tự đặt, không phải người thật.
   - Chạy trên database PHONGKHAM CÒN TRỐNG (vừa tạo từ cau-truc-phongkham.sql).
     Database đã có nhân viên thì script dừng, không ghi gì.
   - Nội dung:
       4 khoa · 8 phòng · 2 ca (sáng 07:00–11:30, chiều 13:00–17:00)
       2 quản trị viên · 5 lễ tân · 5 thu ngân · 20 bác sĩ (5 bác sĩ mỗi khoa)
       10 khách hàng · 30 loại thuốc
       Lịch làm việc từ hôm nay đến 60 ngày tới, mỗi khoa mỗi ca có 2 bác sĩ trực
       2 lượt khám đã thanh toán sáng hôm nay (có đơn thuốc, kho đã trừ)
   - Mật khẩu: website không dùng (giả lập vai trò); cột NOT NULL nên điền giá trị tạm.
   ===================================================================== */
USE PHONGKHAM;
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM Nhan_vien)
BEGIN
    RAISERROR(N'Database đã có dữ liệu nhân viên. Script này chỉ chạy trên database trống, không ghi gì.', 16, 1);
    SET NOEXEC ON;
END;

BEGIN TRAN;

/* ---------- Khoa ---------- */
INSERT INTO Khoa (ma_khoa, ten_khoa, mo_ta) VALUES
  ('K0001', N'Nội tổng quát', N'Khám, tư vấn và điều trị các bệnh nội khoa thường gặp ở người lớn: tim mạch, hô hấp, tiêu hóa, nội tiết.'),
  ('K0002', N'Nhi',           N'Khám bệnh, theo dõi tăng trưởng và tư vấn dinh dưỡng cho trẻ từ sơ sinh đến 15 tuổi.'),
  ('K0003', N'Tai Mũi Họng',  N'Khám và điều trị viêm mũi, viêm xoang, viêm họng, viêm tai và các bệnh vùng tai mũi họng.'),
  ('K0004', N'Da liễu',       N'Khám và điều trị mụn, viêm da, dị ứng da, nấm da và các bệnh lý về da.');

/* ---------- Phòng: mỗi khoa 2 phòng ---------- */
INSERT INTO Phong_lam_viec (ma_phong, ten_phong, ma_khoa) VALUES
  ('P0001', N'Phòng 101', 'K0001'), ('P0002', N'Phòng 102', 'K0001'),
  ('P0003', N'Phòng 201', 'K0002'), ('P0004', N'Phòng 202', 'K0002'),
  ('P0005', N'Phòng 301', 'K0003'), ('P0006', N'Phòng 302', 'K0003'),
  ('P0007', N'Phòng 401', 'K0004'), ('P0008', N'Phòng 402', 'K0004');

/* ---------- Ca làm việc ---------- */
INSERT INTO Ca_lam_viec (ma_ca, ten_ca, gio_bat_dau, gio_ket_thuc) VALUES
  ('CA0001', N'Ca sáng',  '07:00', '11:30'),
  ('CA0002', N'Ca chiều', '13:00', '17:00');

/* ---------- Nhân viên ---------- */
INSERT INTO Nhan_vien (ma_nv, ho_ten_nv, ngay_sinh_nv, sdt_nv, chuc_vu, mat_khau_nv, ma_khoa, duong_dan_anh) VALUES
  -- Quản trị viên
  ('NV0001', N'Trần Minh Khang',     '1988-04-12', '0907112233', N'Quản trị viên', 'matkhau123', NULL, NULL),
  ('NV0002', N'Lê Thị Ngọc Hân',     '1991-09-03', '0918224455', N'Quản trị viên', 'matkhau123', NULL, NULL),
  -- Lễ tân
  ('NV0003', N'Nguyễn Thị Thu Trang','1996-02-18', '0932145678', N'Lễ tân', 'matkhau123', NULL, NULL),
  ('NV0004', N'Phạm Ngọc Bích',      '1998-07-25', '0945236789', N'Lễ tân', 'matkhau123', NULL, NULL),
  ('NV0005', N'Võ Hoàng Yến',        '1997-11-09', '0976347890', N'Lễ tân', 'matkhau123', NULL, NULL),
  ('NV0006', N'Huỳnh Thanh Tâm',     '1995-05-30', '0398458901', N'Lễ tân', 'matkhau123', NULL, NULL),
  ('NV0007', N'Đặng Mỹ Linh',        '1999-01-14', '0367569012', N'Lễ tân', 'matkhau123', NULL, NULL),
  -- Thu ngân
  ('NV0008', N'Bùi Văn Lộc',         '1993-03-22', '0708671234', N'Thu ngân', 'matkhau123', NULL, NULL),
  ('NV0009', N'Trương Thị Kim Ngân', '1994-08-07', '0789782345', N'Thu ngân', 'matkhau123', NULL, NULL),
  ('NV0010', N'Lý Quốc Huy',         '1992-12-16', '0823893456', N'Thu ngân', 'matkhau123', NULL, NULL),
  ('NV0011', N'Phan Thị Diễm My',    '1997-06-01', '0834904567', N'Thu ngân', 'matkhau123', NULL, NULL),
  ('NV0012', N'Dương Tấn Phát',      '1996-10-27', '0856015678', N'Thu ngân', 'matkhau123', NULL, NULL),
  -- Bác sĩ khoa Nội tổng quát
  ('NV0013', N'Nguyễn Văn Hùng',     '1975-02-10', '0903126789', N'Bác sĩ', 'matkhau123', 'K0001', NULL),
  ('NV0014', N'Trần Thị Mai Phương', '1982-06-21', '0913237890', N'Bác sĩ', 'matkhau123', 'K0001', NULL),
  ('NV0015', N'Lê Quang Vinh',       '1979-09-14', '0923348901', N'Bác sĩ', 'matkhau123', 'K0001', NULL),
  ('NV0016', N'Hồ Thị Thanh Xuân',   '1985-12-05', '0933459012', N'Bác sĩ', 'matkhau123', 'K0001', NULL),
  ('NV0017', N'Châu Minh Đức',       '1987-03-28', '0943560123', N'Bác sĩ', 'matkhau123', 'K0001', NULL),
  -- Bác sĩ khoa Nhi
  ('NV0018', N'Phạm Thị Hồng Nhung', '1980-05-17', '0953671234', N'Bác sĩ', 'matkhau123', 'K0002', NULL),
  ('NV0019', N'Đỗ Văn Thắng',        '1978-08-02', '0963782345', N'Bác sĩ', 'matkhau123', 'K0002', NULL),
  ('NV0020', N'Ngô Thị Lan Anh',     '1986-01-23', '0973893456', N'Bác sĩ', 'matkhau123', 'K0002', NULL),
  ('NV0021', N'Vũ Hoàng Nam',        '1984-04-11', '0983904567', N'Bác sĩ', 'matkhau123', 'K0002', NULL),
  ('NV0022', N'Tạ Thị Bảo Ngọc',     '1989-10-30', '0993015678', N'Bác sĩ', 'matkhau123', 'K0002', NULL),
  -- Bác sĩ khoa Tai Mũi Họng
  ('NV0023', N'Huỳnh Công Danh',     '1976-07-19', '0383126789', N'Bác sĩ', 'matkhau123', 'K0003', NULL),
  ('NV0024', N'Lâm Thị Kiều Oanh',   '1983-11-08', '0393237890', N'Bác sĩ', 'matkhau123', 'K0003', NULL),
  ('NV0025', N'Mai Xuân Trường',     '1981-02-27', '0703348901', N'Bác sĩ', 'matkhau123', 'K0003', NULL),
  ('NV0026', N'Kiều Thị Thùy Dung',  '1988-06-15', '0763459012', N'Bác sĩ', 'matkhau123', 'K0003', NULL),
  ('NV0027', N'Tôn Thất Bảo',        '1985-09-04', '0773560123', N'Bác sĩ', 'matkhau123', 'K0003', NULL),
  -- Bác sĩ khoa Da liễu
  ('NV0028', N'Quách Thị Minh Thư',  '1984-03-09', '0783671234', N'Bác sĩ', 'matkhau123', 'K0004', NULL),
  ('NV0029', N'Đinh Gia Bảo',        '1987-12-20', '0793782345', N'Bác sĩ', 'matkhau123', 'K0004', NULL),
  ('NV0030', N'La Thị Phương Thảo',  '1990-05-26', '0813893456', N'Bác sĩ', 'matkhau123', 'K0004', NULL),
  ('NV0031', N'Từ Minh Trí',         '1982-08-13', '0843904567', N'Bác sĩ', 'matkhau123', 'K0004', NULL),
  ('NV0032', N'Văn Thị Ánh Tuyết',   '1986-10-02', '0853015678', N'Bác sĩ', 'matkhau123', 'K0004', NULL);

/* ---------- Khách hàng ---------- */
INSERT INTO Khach_hang (ma_kh, ho_ten_kh, ngay_sinh_kh, gioi_tinh_kh, sdt_kh, dia_chi_kh, mat_khau_kh) VALUES
  ('KH0001', N'Nguyễn Hoàng Long',  '1990-03-15', N'Nam', '0901234567', N'45 Phạm Thái Bường, Phường 4, Vĩnh Long', 'matkhau123'),
  ('KH0002', N'Trần Thị Bích Ngọc', '1985-07-22', N'Nữ',  '0912345678', N'12 Nguyễn Huệ, Phường 2, Vĩnh Long',     'matkhau123'),
  ('KH0003', N'Lê Văn Tài',         '1972-11-05', N'Nam', '0923456789', N'88 Trần Phú, Phường 4, Vĩnh Long',       'matkhau123'),
  ('KH0004', N'Phạm Thị Hoa',       '1995-01-30', N'Nữ',  '0934567890', N'210 Lê Thái Tổ, Phường 2, Vĩnh Long',    'matkhau123'),
  ('KH0005', N'Võ Minh Tuấn',       '2001-09-12', N'Nam', '0945678901', N'7 Đinh Tiên Hoàng, Phường 8, Vĩnh Long', 'matkhau123'),
  ('KH0006', N'Huỳnh Thị Cẩm Tú',   '1998-04-08', N'Nữ',  '0356789012', N'33 Hưng Đạo Vương, Phường 1, Vĩnh Long', 'matkhau123'),
  ('KH0007', N'Đặng Quốc Bảo',      '2018-06-19', N'Nam', '0367890123', N'15 Nguyễn Văn Thiệt, Phường 3, Vĩnh Long', NULL),
  ('KH0008', N'Bùi Thị Thanh Hương','1968-12-01', N'Nữ',  '0778901234', N'Ấp Phước Yên, Xã Phú Quới, Long Hồ, Vĩnh Long', NULL),
  ('KH0009', N'Ngô Văn Phúc',       '1955-02-14', N'Nam', '0789012345', N'102 Mậu Thân, Phường 3, Vĩnh Long',      NULL),
  ('KH0010', N'Lý Thị Mỹ Duyên',    '2020-10-25', N'Nữ',  '0890123456', N'56 Lưu Văn Liệt, Phường 2, Vĩnh Long',   NULL);

/* ---------- Thuốc ---------- */
INSERT INTO Thuoc (ma_thuoc, ten_thuoc, nsx, ham_luong, don_vi_tinh, don_gia_thuoc, so_luong_ton, mo_ta) VALUES
  ('TH0001', N'Paracetamol',          N'Dược Hậu Giang',  500,   N'Viên',  1000, 1000, N'Hạ sốt, giảm đau.'),
  ('TH0002', N'Paracetamol (siro trẻ em)', N'Dược Hậu Giang', 120, N'Chai', 28000,  60, N'Hạ sốt, giảm đau cho trẻ em. Hàm lượng tính trên 5 ml.'),
  ('TH0003', N'Ibuprofen',            N'Stada Việt Nam',  400,   N'Viên',  1500,  500, N'Giảm đau, kháng viêm, hạ sốt.'),
  ('TH0004', N'Amoxicillin',          N'Imexpharm',       500,   N'Viên',  1800,  800, N'Kháng sinh nhóm penicillin.'),
  ('TH0005', N'Amoxicillin + Acid clavulanic', N'Imexpharm', 625, N'Viên', 6500,  400, N'Kháng sinh phổ rộng.'),
  ('TH0006', N'Cefuroxim',            N'Pymepharco',      500,   N'Viên',  7000,  300, N'Kháng sinh nhóm cephalosporin.'),
  ('TH0007', N'Azithromycin',         N'Pymepharco',      250,   N'Viên',  5500,  300, N'Kháng sinh nhóm macrolid.'),
  ('TH0008', N'Cetirizin',            N'Traphaco',        10,    N'Viên',  800,   600, N'Kháng histamin, chống dị ứng.'),
  ('TH0009', N'Loratadin',            N'Domesco',         10,    N'Viên',  900,   600, N'Kháng histamin, ít gây buồn ngủ.'),
  ('TH0010', N'Desloratadin (siro)',  N'Domesco',         2.5,   N'Chai',  45000,  40, N'Chống dị ứng cho trẻ em. Hàm lượng tính trên 5 ml.'),
  ('TH0011', N'Omeprazol',            N'Dược Hậu Giang',  20,    N'Viên',  1200,  700, N'Giảm tiết acid dạ dày.'),
  ('TH0012', N'Domperidon',           N'Mekophar',        10,    N'Viên',  700,   500, N'Chống nôn, đầy bụng.'),
  ('TH0013', N'Oresol',               N'Bidiphar',        4.1,   N'Gói',   2500,  400, N'Bù nước và điện giải. Hàm lượng tính theo gam bột.'),
  ('TH0014', N'Men vi sinh Bacillus clausii', N'Sanofi Việt Nam', 2, N'Ống', 8000, 300, N'Cân bằng hệ vi khuẩn đường ruột. Hàm lượng: tỷ bào tử/ống.'),
  ('TH0015', N'Metformin',            N'Stada Việt Nam',  500,   N'Viên',  900,   800, N'Điều trị đái tháo đường type 2.'),
  ('TH0016', N'Amlodipin',            N'Domesco',         5,     N'Viên',  600,   800, N'Điều trị tăng huyết áp.'),
  ('TH0017', N'Losartan',             N'Stada Việt Nam',  50,    N'Viên',  2000,  600, N'Điều trị tăng huyết áp.'),
  ('TH0018', N'Atorvastatin',         N'Pymepharco',      20,    N'Viên',  2500,  600, N'Giảm mỡ máu.'),
  ('TH0019', N'Ambroxol',             N'Mekophar',        30,    N'Viên',  700,   500, N'Long đờm.'),
  ('TH0020', N'Acetylcystein',        N'Traphaco',        200,   N'Gói',   1500,  500, N'Tiêu đờm.'),
  ('TH0021', N'Dextromethorphan',     N'Mekophar',        15,    N'Viên',  900,   400, N'Giảm ho khan.'),
  ('TH0022', N'Natri clorid 0,9% (nhỏ mũi)', N'Bidiphar', 0.9,   N'Chai',  4000,  300, N'Rửa và nhỏ mũi. Hàm lượng tính theo %.'),
  ('TH0023', N'Xylometazolin (xịt mũi)', N'Traphaco',     0.1,   N'Chai',  25000, 100, N'Giảm nghẹt mũi. Hàm lượng tính theo %.'),
  ('TH0024', N'Prednisolon',          N'Dược Hậu Giang',  5,     N'Viên',  500,   600, N'Corticoid kháng viêm.'),
  ('TH0025', N'Vitamin C',            N'Dược Hậu Giang',  500,   N'Viên',  1000,  800, N'Bổ sung vitamin C.'),
  ('TH0026', N'Kẽm gluconat',         N'Traphaco',        10,    N'Viên',  1500,  400, N'Bổ sung kẽm. Hàm lượng tính theo kẽm nguyên tố.'),
  ('TH0027', N'Clotrimazol (kem bôi)', N'Medipharco',     1,     N'Tuýp',  15000, 150, N'Điều trị nấm da. Hàm lượng tính theo %.'),
  ('TH0028', N'Fusidic acid (kem bôi)', N'Medipharco',    2,     N'Tuýp',  35000, 100, N'Kháng sinh bôi ngoài da. Hàm lượng tính theo %.'),
  ('TH0029', N'Hydrocortison (kem bôi)', N'Medipharco',   1,     N'Tuýp',  18000, 120, N'Giảm viêm, ngứa da. Hàm lượng tính theo %.'),
  ('TH0030', N'Isotretinoin',         N'Stada Việt Nam',  10,    N'Viên',  9000,  200, N'Điều trị mụn trứng cá nặng, dùng theo chỉ định bác sĩ.');

/* ---------- Lịch làm việc: hôm nay đến +60 ngày ----------
   Mỗi khoa có 5 bác sĩ và 2 phòng. Mỗi ngày mỗi khoa có 4 suất (2 phòng × 2 ca),
   xoay vòng 5 bác sĩ nên mỗi ngày có 1 người nghỉ, không ai trực 2 suất cùng lúc. */
DECLARE @homNay date = CAST(GETDATE() AS date);

;WITH Ngay AS (
    SELECT TOP (61) DATEADD(DAY, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, @homNay) AS ngay,
                    ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n
    FROM sys.all_objects
),
Khoa_ AS (
    SELECT ma_khoa, ROW_NUMBER() OVER (ORDER BY ma_khoa) - 1 AS k FROM Khoa
),
BacSi AS (
    SELECT ma_nv, ma_khoa, ROW_NUMBER() OVER (PARTITION BY ma_khoa ORDER BY ma_nv) - 1 AS b
    FROM Nhan_vien WHERE chuc_vu = N'Bác sĩ'
),
Phong AS (
    SELECT ma_phong, ma_khoa, ROW_NUMBER() OVER (PARTITION BY ma_khoa ORDER BY ma_phong) - 1 AS p
    FROM Phong_lam_viec
),
Suat AS (   -- s = 0..3: (phòng p, ca c)
    SELECT p.ma_khoa, p.ma_phong, c.ma_ca, p.p * 2 + c.c AS s
    FROM Phong p
    CROSS JOIN (SELECT ma_ca, ROW_NUMBER() OVER (ORDER BY ma_ca) - 1 AS c FROM Ca_lam_viec) c
),
Xep AS (
    SELECT n.ngay, s.ma_phong, s.ma_ca, bs.ma_nv
    FROM Ngay n
    JOIN Suat s ON 1 = 1
    JOIN BacSi bs ON bs.ma_khoa = s.ma_khoa AND bs.b = (n.n + s.s) % 5
)
INSERT INTO Lich_lam_viec (ma_lich_lam_viec, ngay_lam_viec, ma_nv, ma_phong, ma_ca)
SELECT 'LV' + RIGHT('0000' + CAST(ROW_NUMBER() OVER (ORDER BY ngay, ma_ca, ma_phong) AS varchar(6)), 4),
       ngay, ma_nv, ma_phong, ma_ca
FROM Xep;

/* ---------- 2 lượt khám đã thanh toán sáng hôm nay ----------
   Đi đúng quy trình: khách đặt lịch (tối hôm qua) → lễ tân xác nhận → lễ tân lập hồ sơ
   → bác sĩ chẩn đoán, kê đơn → lập hóa đơn → thu ngân thu tiền (trigger tự tính tổng và trừ kho). */
DECLARE @lv1 varchar(10), @lv2 varchar(10), @bs1 varchar(10), @bs2 varchar(10);
SELECT @lv1 = l.ma_lich_lam_viec, @bs1 = l.ma_nv FROM Lich_lam_viec l
WHERE l.ngay_lam_viec = @homNay AND l.ma_ca = 'CA0001' AND l.ma_phong = 'P0001';   -- Nội tổng quát, ca sáng
SELECT @lv2 = l.ma_lich_lam_viec, @bs2 = l.ma_nv FROM Lich_lam_viec l
WHERE l.ngay_lam_viec = @homNay AND l.ma_ca = 'CA0001' AND l.ma_phong = 'P0003';   -- Nhi, ca sáng

DECLARE @homQua datetime = DATEADD(DAY, -1, CAST(@homNay AS datetime));
DECLARE @sang   datetime = CAST(@homNay AS datetime);

INSERT INTO Lich_hen (ma_lich_hen, ngay_gio_dat, trieu_chung_ban_dau, trang_thai, ma_kh, ma_lich_lam_viec, ma_nv) VALUES
  ('LH0001', DATEADD(MINUTE, 20*60+15, @homQua), N'Sốt nhẹ, đau họng, ho khan 3 ngày', N'Đã đặt', 'KH0001', @lv1, NULL),
  ('LH0002', DATEADD(MINUTE, 21*60+40, @homQua), N'Bé bị tiêu chảy, đi ngoài 4 lần/ngày từ hôm qua', N'Đã đặt', 'KH0007', @lv2, NULL);

UPDATE Lich_hen SET trang_thai = N'Đã xác nhận', ma_nv = 'NV0003' WHERE ma_lich_hen IN ('LH0001', 'LH0002');

INSERT INTO Ho_so_kham (ma_ho_so, ngay_gio_kham, trieu_chung, chan_doan_benh, loi_dan, tien_cong_kham, ma_lich_hen, ma_kh, ma_nv_kham, ma_nv_tiep_nhan) VALUES
  ('HS0001', DATEADD(MINUTE, 7*60+30, @sang), N'Sốt 38 độ, đau rát họng, ho khan, mệt mỏi',
             N'Viêm họng cấp', N'Uống nhiều nước ấm, súc miệng nước muối, tái khám sau 5 ngày nếu còn sốt.',
             150000, 'LH0001', 'KH0001', @bs1, 'NV0003'),
  ('HS0002', DATEADD(MINUTE, 8*60+10, @sang), N'Tiêu chảy 4 lần/ngày, phân lỏng, không sốt, còn bú ăn được',
             N'Tiêu chảy cấp, chưa mất nước', N'Cho bé uống Oresol sau mỗi lần đi ngoài, ăn cháo loãng, theo dõi dấu hiệu mất nước.',
             150000, 'LH0002', 'KH0007', @bs2, 'NV0003');

-- Đơn giá do trigger lấy từ danh mục thuốc (giá truyền vào chỉ để thỏa cú pháp)
INSERT INTO Chi_tiet_don_thuoc (ma_ho_so, ma_thuoc, so_luong_thuoc, lieu_dung, don_gia_thuoc) VALUES
  ('HS0001', 'TH0005', 14, N'Uống 1 viên/lần, 2 lần/ngày sau ăn, trong 7 ngày', 1),
  ('HS0001', 'TH0001', 10, N'Uống 1 viên khi sốt trên 38,5 độ, cách nhau ít nhất 4 giờ', 1),
  ('HS0001', 'TH0021', 10, N'Uống 1 viên/lần, 2 lần/ngày', 1),
  ('HS0002', 'TH0013', 10, N'Pha 1 gói với 200 ml nước, uống sau mỗi lần đi ngoài', 1),
  ('HS0002', 'TH0014', 10, N'Uống 1 ống/lần, 2 lần/ngày', 1);

INSERT INTO Hoa_don (ma_hoa_don, tong_tien, ngay_gio_thanh_toan, trang_thai_thanh_toan, ma_ho_so, ma_nv) VALUES
  ('HD0001', 0, NULL, N'Chưa thanh toán', 'HS0001', NULL),
  ('HD0002', 0, NULL, N'Chưa thanh toán', 'HS0002', NULL);

UPDATE Hoa_don SET trang_thai_thanh_toan = N'Đã thanh toán', ma_nv = 'NV0008',
       ngay_gio_thanh_toan = DATEADD(MINUTE, 8*60+5, @sang)  WHERE ma_hoa_don = 'HD0001';
UPDATE Hoa_don SET trang_thai_thanh_toan = N'Đã thanh toán', ma_nv = 'NV0009',
       ngay_gio_thanh_toan = DATEADD(MINUTE, 8*60+40, @sang) WHERE ma_hoa_don = 'HD0002';

COMMIT;
PRINT N'Đã nạp dữ liệu mẫu đầy đủ.';
SET NOEXEC OFF;
