-- Cấu trúc database PHONGKHAM, sao từ PHONGKHAM-SCRIPT.sql của đồ án (SQL Server 2025, mức tương thích 170).
-- Khác bản gốc DUY NHẤT ở lệnh CREATE DATABASE: bỏ đường dẫn file .mdf/.ldf trên ổ C: để SQL Server tự đặt
-- (chạy được cả trên Linux/Docker). Bảng, khóa, CHECK, UNIQUE, trigger giữ nguyên.
USE [master]
GO
/****** Object:  Database [PHONGKHAM]    Script Date: 11/11/2026 9:21:57 AM ******/
CREATE DATABASE [PHONGKHAM]
GO
ALTER DATABASE [PHONGKHAM] SET COMPATIBILITY_LEVEL = 170
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [PHONGKHAM].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [PHONGKHAM] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [PHONGKHAM] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [PHONGKHAM] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [PHONGKHAM] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [PHONGKHAM] SET ARITHABORT OFF 
GO
ALTER DATABASE [PHONGKHAM] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [PHONGKHAM] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [PHONGKHAM] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [PHONGKHAM] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [PHONGKHAM] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [PHONGKHAM] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [PHONGKHAM] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [PHONGKHAM] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [PHONGKHAM] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [PHONGKHAM] SET  DISABLE_BROKER 
GO
ALTER DATABASE [PHONGKHAM] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [PHONGKHAM] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [PHONGKHAM] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [PHONGKHAM] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [PHONGKHAM] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [PHONGKHAM] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [PHONGKHAM] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [PHONGKHAM] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [PHONGKHAM] SET  MULTI_USER 
GO
ALTER DATABASE [PHONGKHAM] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [PHONGKHAM] SET DB_CHAINING OFF 
GO
ALTER DATABASE [PHONGKHAM] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [PHONGKHAM] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [PHONGKHAM] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [PHONGKHAM] SET OPTIMIZED_LOCKING = OFF 
GO
ALTER DATABASE [PHONGKHAM] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [PHONGKHAM] SET QUERY_STORE = ON
GO
ALTER DATABASE [PHONGKHAM] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [PHONGKHAM]
GO
/****** Object:  Table [dbo].[Ca_lam_viec]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Ca_lam_viec](
	[ma_ca] [varchar](10) NOT NULL,
	[ten_ca] [nvarchar](20) NOT NULL,
	[gio_bat_dau] [time](7) NOT NULL,
	[gio_ket_thuc] [time](7) NOT NULL,
 CONSTRAINT [PK_Ca_lam_viec] PRIMARY KEY CLUSTERED 
(
	[ma_ca] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Chi_tiet_don_thuoc]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Chi_tiet_don_thuoc](
	[ma_ho_so] [varchar](10) NOT NULL,
	[ma_thuoc] [varchar](10) NOT NULL,
	[so_luong_thuoc] [int] NOT NULL,
	[lieu_dung] [nvarchar](255) NOT NULL,
	[don_gia_thuoc] [decimal](12, 0) NOT NULL,
 CONSTRAINT [PK_Chi_tiet_don_thuoc] PRIMARY KEY CLUSTERED 
(
	[ma_ho_so] ASC,
	[ma_thuoc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Ho_so_kham]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Ho_so_kham](
	[ma_ho_so] [varchar](10) NOT NULL,
	[ngay_gio_kham] [datetime] NOT NULL,
	[trieu_chung] [nvarchar](255) NOT NULL,
	[chan_doan_benh] [nvarchar](255) NULL,
	[loi_dan] [nvarchar](255) NULL,
	[tien_cong_kham] [decimal](12, 0) NOT NULL,
	[ma_lich_hen] [varchar](10) NULL,
	[ma_kh] [varchar](10) NOT NULL,
	[ma_nv_kham] [varchar](10) NOT NULL,
	[ma_nv_tiep_nhan] [varchar](10) NOT NULL,
 CONSTRAINT [PK_Ho_so_kham] PRIMARY KEY CLUSTERED 
(
	[ma_ho_so] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Hoa_don]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Hoa_don](
	[ma_hoa_don] [varchar](10) NOT NULL,
	[tong_tien] [decimal](12, 0) NOT NULL,
	[ngay_gio_thanh_toan] [datetime] NULL,
	[trang_thai_thanh_toan] [nvarchar](20) NOT NULL,
	[ma_ho_so] [varchar](10) NOT NULL,
	[ma_nv] [varchar](10) NULL,
 CONSTRAINT [PK_Hoa_don] PRIMARY KEY CLUSTERED 
(
	[ma_hoa_don] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Khach_hang]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Khach_hang](
	[ma_kh] [varchar](10) NOT NULL,
	[ho_ten_kh] [nvarchar](50) NOT NULL,
	[ngay_sinh_kh] [date] NOT NULL,
	[gioi_tinh_kh] [nvarchar](5) NOT NULL,
	[sdt_kh] [varchar](10) NOT NULL,
	[dia_chi_kh] [nvarchar](100) NULL,
	[mat_khau_kh] [varchar](255) NULL,
 CONSTRAINT [PK_Khach_hang] PRIMARY KEY CLUSTERED 
(
	[ma_kh] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Khoa]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Khoa](
	[ma_khoa] [varchar](10) NOT NULL,
	[ten_khoa] [nvarchar](50) NOT NULL,
	[mo_ta] [nvarchar](255) NULL,
 CONSTRAINT [PK_Khoa] PRIMARY KEY CLUSTERED 
(
	[ma_khoa] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Lich_hen]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lich_hen](
	[ma_lich_hen] [varchar](10) NOT NULL,
	[ngay_gio_dat] [datetime] NOT NULL,
	[trieu_chung_ban_dau] [nvarchar](255) NULL,
	[trang_thai] [nvarchar](20) NOT NULL,
	[ma_kh] [varchar](10) NOT NULL,
	[ma_lich_lam_viec] [varchar](10) NOT NULL,
	[ma_nv] [varchar](10) NULL,
 CONSTRAINT [PK_LICH_HEN] PRIMARY KEY CLUSTERED 
(
	[ma_lich_hen] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Lich_lam_viec]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lich_lam_viec](
	[ma_lich_lam_viec] [varchar](10) NOT NULL,
	[ngay_lam_viec] [date] NOT NULL,
	[ma_nv] [varchar](10) NOT NULL,
	[ma_phong] [varchar](10) NOT NULL,
	[ma_ca] [varchar](10) NOT NULL,
 CONSTRAINT [PK_LICH_LAM_VIEC] PRIMARY KEY CLUSTERED 
(
	[ma_lich_lam_viec] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Nhan_vien]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Nhan_vien](
	[ma_nv] [varchar](10) NOT NULL,
	[ho_ten_nv] [nvarchar](50) NOT NULL,
	[ngay_sinh_nv] [date] NULL,
	[sdt_nv] [varchar](10) NOT NULL,
	[chuc_vu] [nvarchar](20) NOT NULL,
	[mat_khau_nv] [varchar](255) NOT NULL,
	[ma_khoa] [varchar](10) NULL,
	[duong_dan_anh] [nvarchar](1000) NULL,
 CONSTRAINT [PK_Nhan_vien] PRIMARY KEY CLUSTERED 
(
	[ma_nv] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Phong_lam_viec]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Phong_lam_viec](
	[ma_phong] [varchar](10) NOT NULL,
	[ten_phong] [nvarchar](50) NOT NULL,
	[ma_khoa] [varchar](10) NOT NULL,
 CONSTRAINT [PK_PhongLamViec] PRIMARY KEY CLUSTERED 
(
	[ma_phong] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Thuoc]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Thuoc](
	[ma_thuoc] [varchar](10) NOT NULL,
	[ten_thuoc] [nvarchar](100) NOT NULL,
	[nsx] [nvarchar](200) NOT NULL,
	[ham_luong] [decimal](10, 2) NOT NULL,
	[don_vi_tinh] [nvarchar](20) NOT NULL,
	[don_gia_thuoc] [decimal](12, 0) NOT NULL,
	[so_luong_ton] [int] NOT NULL,
	[mo_ta] [nvarchar](255) NULL,
 CONSTRAINT [PK_Thuoc] PRIMARY KEY CLUSTERED 
(
	[ma_thuoc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_Ho_so_kham_ma_lich_hen]    Script Date: 11/11/2026 9:21:58 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_Ho_so_kham_ma_lich_hen] ON [dbo].[Ho_so_kham]
(
	[ma_lich_hen] ASC
)
WHERE ([ma_lich_hen] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_Hoa_don_ma_ho_so]    Script Date: 11/11/2026 9:21:58 AM ******/
ALTER TABLE [dbo].[Hoa_don] ADD  CONSTRAINT [UQ_Hoa_don_ma_ho_so] UNIQUE NONCLUSTERED 
(
	[ma_ho_so] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_Khach_hang_sdt_kh]    Script Date: 11/11/2026 9:21:58 AM ******/
ALTER TABLE [dbo].[Khach_hang] ADD  CONSTRAINT [UQ_Khach_hang_sdt_kh] UNIQUE NONCLUSTERED 
(
	[sdt_kh] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_Khoa_ten_khoa]    Script Date: 11/11/2026 9:21:58 AM ******/
ALTER TABLE [dbo].[Khoa] ADD  CONSTRAINT [UQ_Khoa_ten_khoa] UNIQUE NONCLUSTERED 
(
	[ten_khoa] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_LICH_LAM_VIEC_nv_ca_ngay]    Script Date: 11/11/2026 9:21:58 AM ******/
ALTER TABLE [dbo].[Lich_lam_viec] ADD  CONSTRAINT [UQ_LICH_LAM_VIEC_nv_ca_ngay] UNIQUE NONCLUSTERED 
(
	[ma_nv] ASC,
	[ma_ca] ASC,
	[ngay_lam_viec] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_LICH_LAM_VIEC_phong_ca_ngay]    Script Date: 11/11/2026 9:21:58 AM ******/
ALTER TABLE [dbo].[Lich_lam_viec] ADD  CONSTRAINT [UQ_LICH_LAM_VIEC_phong_ca_ngay] UNIQUE NONCLUSTERED 
(
	[ma_phong] ASC,
	[ma_ca] ASC,
	[ngay_lam_viec] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_Nhan_vien_sdt_nv]    Script Date: 11/11/2026 9:21:58 AM ******/
ALTER TABLE [dbo].[Nhan_vien] ADD  CONSTRAINT [UQ_Nhan_vien_sdt_nv] UNIQUE NONCLUSTERED 
(
	[sdt_nv] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_PhongLamViec_ten_phong]    Script Date: 11/11/2026 9:21:58 AM ******/
ALTER TABLE [dbo].[Phong_lam_viec] ADD  CONSTRAINT [UQ_PhongLamViec_ten_phong] UNIQUE NONCLUSTERED 
(
	[ten_phong] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_Thuoc_ten_nsx_ham_luong]    Script Date: 11/11/2026 9:21:58 AM ******/
ALTER TABLE [dbo].[Thuoc] ADD  CONSTRAINT [UQ_Thuoc_ten_nsx_ham_luong] UNIQUE NONCLUSTERED 
(
	[ten_thuoc] ASC,
	[nsx] ASC,
	[ham_luong] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Ho_so_kham] ADD  DEFAULT (getdate()) FOR [ngay_gio_kham]
GO
ALTER TABLE [dbo].[Hoa_don] ADD  DEFAULT ((0)) FOR [tong_tien]
GO
ALTER TABLE [dbo].[Hoa_don] ADD  DEFAULT (N'Chưa thanh toán') FOR [trang_thai_thanh_toan]
GO
ALTER TABLE [dbo].[Lich_hen] ADD  DEFAULT (getdate()) FOR [ngay_gio_dat]
GO
ALTER TABLE [dbo].[Lich_hen] ADD  DEFAULT (N'Đã đặt') FOR [trang_thai]
GO
ALTER TABLE [dbo].[Thuoc] ADD  DEFAULT ((0)) FOR [so_luong_ton]
GO
ALTER TABLE [dbo].[Chi_tiet_don_thuoc]  WITH CHECK ADD  CONSTRAINT [FK_CTDT_Ho_so_kham] FOREIGN KEY([ma_ho_so])
REFERENCES [dbo].[Ho_so_kham] ([ma_ho_so])
GO
ALTER TABLE [dbo].[Chi_tiet_don_thuoc] CHECK CONSTRAINT [FK_CTDT_Ho_so_kham]
GO
ALTER TABLE [dbo].[Chi_tiet_don_thuoc]  WITH CHECK ADD  CONSTRAINT [FK_CTDT_Thuoc] FOREIGN KEY([ma_thuoc])
REFERENCES [dbo].[Thuoc] ([ma_thuoc])
GO
ALTER TABLE [dbo].[Chi_tiet_don_thuoc] CHECK CONSTRAINT [FK_CTDT_Thuoc]
GO
ALTER TABLE [dbo].[Ho_so_kham]  WITH CHECK ADD  CONSTRAINT [FK_Ho_so_kham_Khach_hang] FOREIGN KEY([ma_kh])
REFERENCES [dbo].[Khach_hang] ([ma_kh])
GO
ALTER TABLE [dbo].[Ho_so_kham] CHECK CONSTRAINT [FK_Ho_so_kham_Khach_hang]
GO
ALTER TABLE [dbo].[Ho_so_kham]  WITH CHECK ADD  CONSTRAINT [FK_Ho_so_kham_LICH_HEN] FOREIGN KEY([ma_lich_hen])
REFERENCES [dbo].[Lich_hen] ([ma_lich_hen])
GO
ALTER TABLE [dbo].[Ho_so_kham] CHECK CONSTRAINT [FK_Ho_so_kham_LICH_HEN]
GO
ALTER TABLE [dbo].[Ho_so_kham]  WITH CHECK ADD  CONSTRAINT [FK_Ho_so_kham_NV_kham] FOREIGN KEY([ma_nv_kham])
REFERENCES [dbo].[Nhan_vien] ([ma_nv])
GO
ALTER TABLE [dbo].[Ho_so_kham] CHECK CONSTRAINT [FK_Ho_so_kham_NV_kham]
GO
ALTER TABLE [dbo].[Ho_so_kham]  WITH CHECK ADD  CONSTRAINT [FK_Ho_so_kham_NV_tiep_nhan] FOREIGN KEY([ma_nv_tiep_nhan])
REFERENCES [dbo].[Nhan_vien] ([ma_nv])
GO
ALTER TABLE [dbo].[Ho_so_kham] CHECK CONSTRAINT [FK_Ho_so_kham_NV_tiep_nhan]
GO
ALTER TABLE [dbo].[Hoa_don]  WITH CHECK ADD  CONSTRAINT [FK_Hoa_don_Ho_so_kham] FOREIGN KEY([ma_ho_so])
REFERENCES [dbo].[Ho_so_kham] ([ma_ho_so])
GO
ALTER TABLE [dbo].[Hoa_don] CHECK CONSTRAINT [FK_Hoa_don_Ho_so_kham]
GO
ALTER TABLE [dbo].[Hoa_don]  WITH CHECK ADD  CONSTRAINT [FK_Hoa_don_Nhan_vien] FOREIGN KEY([ma_nv])
REFERENCES [dbo].[Nhan_vien] ([ma_nv])
GO
ALTER TABLE [dbo].[Hoa_don] CHECK CONSTRAINT [FK_Hoa_don_Nhan_vien]
GO
ALTER TABLE [dbo].[Lich_hen]  WITH CHECK ADD  CONSTRAINT [FK_LICH_HEN_Khach_hang] FOREIGN KEY([ma_kh])
REFERENCES [dbo].[Khach_hang] ([ma_kh])
GO
ALTER TABLE [dbo].[Lich_hen] CHECK CONSTRAINT [FK_LICH_HEN_Khach_hang]
GO
ALTER TABLE [dbo].[Lich_hen]  WITH CHECK ADD  CONSTRAINT [FK_LICH_HEN_LICH_LAM_VIEC] FOREIGN KEY([ma_lich_lam_viec])
REFERENCES [dbo].[Lich_lam_viec] ([ma_lich_lam_viec])
GO
ALTER TABLE [dbo].[Lich_hen] CHECK CONSTRAINT [FK_LICH_HEN_LICH_LAM_VIEC]
GO
ALTER TABLE [dbo].[Lich_hen]  WITH CHECK ADD  CONSTRAINT [FK_LICH_HEN_Nhan_vien] FOREIGN KEY([ma_nv])
REFERENCES [dbo].[Nhan_vien] ([ma_nv])
GO
ALTER TABLE [dbo].[Lich_hen] CHECK CONSTRAINT [FK_LICH_HEN_Nhan_vien]
GO
ALTER TABLE [dbo].[Lich_lam_viec]  WITH CHECK ADD  CONSTRAINT [FK_LICH_LAM_VIEC_Ca] FOREIGN KEY([ma_ca])
REFERENCES [dbo].[Ca_lam_viec] ([ma_ca])
GO
ALTER TABLE [dbo].[Lich_lam_viec] CHECK CONSTRAINT [FK_LICH_LAM_VIEC_Ca]
GO
ALTER TABLE [dbo].[Lich_lam_viec]  WITH CHECK ADD  CONSTRAINT [FK_LICH_LAM_VIEC_Nhan_vien] FOREIGN KEY([ma_nv])
REFERENCES [dbo].[Nhan_vien] ([ma_nv])
GO
ALTER TABLE [dbo].[Lich_lam_viec] CHECK CONSTRAINT [FK_LICH_LAM_VIEC_Nhan_vien]
GO
ALTER TABLE [dbo].[Lich_lam_viec]  WITH CHECK ADD  CONSTRAINT [FK_LICH_LAM_VIEC_Phong] FOREIGN KEY([ma_phong])
REFERENCES [dbo].[Phong_lam_viec] ([ma_phong])
GO
ALTER TABLE [dbo].[Lich_lam_viec] CHECK CONSTRAINT [FK_LICH_LAM_VIEC_Phong]
GO
ALTER TABLE [dbo].[Nhan_vien]  WITH CHECK ADD  CONSTRAINT [FK_Nhan_vien_Khoa] FOREIGN KEY([ma_khoa])
REFERENCES [dbo].[Khoa] ([ma_khoa])
GO
ALTER TABLE [dbo].[Nhan_vien] CHECK CONSTRAINT [FK_Nhan_vien_Khoa]
GO
ALTER TABLE [dbo].[Phong_lam_viec]  WITH CHECK ADD  CONSTRAINT [FK_PhongLamViec_Khoa] FOREIGN KEY([ma_khoa])
REFERENCES [dbo].[Khoa] ([ma_khoa])
GO
ALTER TABLE [dbo].[Phong_lam_viec] CHECK CONSTRAINT [FK_PhongLamViec_Khoa]
GO
ALTER TABLE [dbo].[Ca_lam_viec]  WITH CHECK ADD  CONSTRAINT [CK_Ca_lam_viec_gio] CHECK  (([gio_bat_dau]<[gio_ket_thuc]))
GO
ALTER TABLE [dbo].[Ca_lam_viec] CHECK CONSTRAINT [CK_Ca_lam_viec_gio]
GO
ALTER TABLE [dbo].[Chi_tiet_don_thuoc]  WITH CHECK ADD  CONSTRAINT [CK_CTDT_don_gia] CHECK  (([don_gia_thuoc]>(0)))
GO
ALTER TABLE [dbo].[Chi_tiet_don_thuoc] CHECK CONSTRAINT [CK_CTDT_don_gia]
GO
ALTER TABLE [dbo].[Chi_tiet_don_thuoc]  WITH CHECK ADD  CONSTRAINT [CK_CTDT_so_luong] CHECK  (([so_luong_thuoc]>(0)))
GO
ALTER TABLE [dbo].[Chi_tiet_don_thuoc] CHECK CONSTRAINT [CK_CTDT_so_luong]
GO
ALTER TABLE [dbo].[Ho_so_kham]  WITH CHECK ADD  CONSTRAINT [CK_Ho_so_kham_tien_cong] CHECK  (([tien_cong_kham]>=(0)))
GO
ALTER TABLE [dbo].[Ho_so_kham] CHECK CONSTRAINT [CK_Ho_so_kham_tien_cong]
GO
ALTER TABLE [dbo].[Hoa_don]  WITH CHECK ADD  CONSTRAINT [CK_Hoa_don_thanh_toan] CHECK  (([trang_thai_thanh_toan]=N'Chưa thanh toán' AND [ngay_gio_thanh_toan] IS NULL AND [ma_nv] IS NULL OR [trang_thai_thanh_toan]=N'Đã thanh toán' AND [ngay_gio_thanh_toan] IS NOT NULL AND [ma_nv] IS NOT NULL))
GO
ALTER TABLE [dbo].[Hoa_don] CHECK CONSTRAINT [CK_Hoa_don_thanh_toan]
GO
ALTER TABLE [dbo].[Hoa_don]  WITH CHECK ADD  CONSTRAINT [CK_Hoa_don_tong_tien] CHECK  (([tong_tien]>=(0)))
GO
ALTER TABLE [dbo].[Hoa_don] CHECK CONSTRAINT [CK_Hoa_don_tong_tien]
GO
ALTER TABLE [dbo].[Hoa_don]  WITH CHECK ADD  CONSTRAINT [CK_Hoa_don_trang_thai] CHECK  (([trang_thai_thanh_toan]=N'Đã thanh toán' OR [trang_thai_thanh_toan]=N'Chưa thanh toán'))
GO
ALTER TABLE [dbo].[Hoa_don] CHECK CONSTRAINT [CK_Hoa_don_trang_thai]
GO
ALTER TABLE [dbo].[Khach_hang]  WITH CHECK ADD  CONSTRAINT [CK_Khach_hang_gioi_tinh] CHECK  (([gioi_tinh_kh]=N'Nữ' OR [gioi_tinh_kh]=N'Nam'))
GO
ALTER TABLE [dbo].[Khach_hang] CHECK CONSTRAINT [CK_Khach_hang_gioi_tinh]
GO
ALTER TABLE [dbo].[Lich_hen]  WITH CHECK ADD  CONSTRAINT [CK_LICH_HEN_le_tan] CHECK  (([trang_thai]=N'Đã đặt' AND [ma_nv] IS NULL OR ([trang_thai]=N'Đã khám' OR [trang_thai]=N'Đã xác nhận') AND [ma_nv] IS NOT NULL OR [trang_thai]=N'Đã hủy'))
GO
ALTER TABLE [dbo].[Lich_hen] CHECK CONSTRAINT [CK_LICH_HEN_le_tan]
GO
ALTER TABLE [dbo].[Lich_hen]  WITH CHECK ADD  CONSTRAINT [CK_LICH_HEN_trang_thai] CHECK  (([trang_thai]=N'Đã hủy' OR [trang_thai]=N'Đã khám' OR [trang_thai]=N'Đã xác nhận' OR [trang_thai]=N'Đã đặt'))
GO
ALTER TABLE [dbo].[Lich_hen] CHECK CONSTRAINT [CK_LICH_HEN_trang_thai]
GO
ALTER TABLE [dbo].[Nhan_vien]  WITH CHECK ADD  CONSTRAINT [CK_Nhan_vien_chuc_vu] CHECK  (([chuc_vu]=N'Thu ngân' OR [chuc_vu]=N'Bác sĩ' OR [chuc_vu]=N'Lễ tân' OR [chuc_vu]=N'Quản trị viên'))
GO
ALTER TABLE [dbo].[Nhan_vien] CHECK CONSTRAINT [CK_Nhan_vien_chuc_vu]
GO
ALTER TABLE [dbo].[Nhan_vien]  WITH CHECK ADD  CONSTRAINT [CK_Nhan_vien_khoa] CHECK  (([chuc_vu]=N'Bác sĩ' AND [ma_khoa] IS NOT NULL OR [chuc_vu]<>N'Bác sĩ' AND [ma_khoa] IS NULL))
GO
ALTER TABLE [dbo].[Nhan_vien] CHECK CONSTRAINT [CK_Nhan_vien_khoa]
GO
ALTER TABLE [dbo].[Thuoc]  WITH CHECK ADD  CONSTRAINT [CK_Thuoc_don_gia] CHECK  (([don_gia_thuoc]>(0)))
GO
ALTER TABLE [dbo].[Thuoc] CHECK CONSTRAINT [CK_Thuoc_don_gia]
GO
ALTER TABLE [dbo].[Thuoc]  WITH CHECK ADD  CONSTRAINT [CK_Thuoc_so_luong_ton] CHECK  (([so_luong_ton]>=(0)))
GO
ALTER TABLE [dbo].[Thuoc] CHECK CONSTRAINT [CK_Thuoc_so_luong_ton]
GO
/****** Object:  Trigger [dbo].[TG_CTDT_CapNhat]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[TG_CTDT_CapNhat]
ON [dbo].[Chi_tiet_don_thuoc]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Không sửa đơn thuốc khi hóa đơn đã thanh toán (kho đã bị trừ theo RB33)
    IF EXISTS (SELECT 1 FROM Hoa_don hd
               WHERE hd.trang_thai_thanh_toan = N'Đã thanh toán'
                 AND hd.ma_ho_so IN (SELECT ma_ho_so FROM inserted
                                     UNION SELECT ma_ho_so FROM deleted))
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50033, N'RB33: Hóa đơn đã thanh toán, không được sửa đơn thuốc.', 1;
    END;

    -- RB27 khi sửa số lượng
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN deleted d ON d.ma_ho_so = i.ma_ho_so AND d.ma_thuoc = i.ma_thuoc
               JOIN Thuoc t   ON t.ma_thuoc = i.ma_thuoc
               WHERE i.so_luong_thuoc > t.so_luong_ton)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50027, N'RB27: Số lượng kê vượt quá số lượng tồn kho.', 1;
    END;

    -- RB28: đơn giá đã chốt lúc kê thì không được sửa
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN deleted d ON d.ma_ho_so = i.ma_ho_so AND d.ma_thuoc = i.ma_thuoc
               WHERE i.don_gia_thuoc <> d.don_gia_thuoc)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50028, N'RB28: Không được sửa đơn giá đã chốt lúc kê.', 1;
    END;

    -- RB29: tính lại tổng tiền của hóa đơn chưa thanh toán (nếu hóa đơn đã được lập)
    UPDATE hd
    SET    tong_tien = hs.tien_cong_kham
                     + ISNULL((SELECT SUM(ct.so_luong_thuoc * ct.don_gia_thuoc)
                               FROM Chi_tiet_don_thuoc ct
                               WHERE ct.ma_ho_so = hd.ma_ho_so), 0)
    FROM   Hoa_don hd
    JOIN   Ho_so_kham hs ON hs.ma_ho_so = hd.ma_ho_so
    WHERE  hd.trang_thai_thanh_toan = N'Chưa thanh toán'
      AND  hd.ma_ho_so IN (SELECT ma_ho_so FROM inserted
                           UNION SELECT ma_ho_so FROM deleted);
END;

GO
ALTER TABLE [dbo].[Chi_tiet_don_thuoc] ENABLE TRIGGER [TG_CTDT_CapNhat]
GO
/****** Object:  Trigger [dbo].[TG_CTDT_KeDon]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Trigger 1: khi kê thuốc, kiểm tra tồn kho và tự lấy đơn giá từ danh mục
CREATE TRIGGER [dbo].[TG_CTDT_KeDon]
ON [dbo].[Chi_tiet_don_thuoc]
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- RB27: số lượng kê không vượt quá tồn kho tại thời điểm kê
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN Thuoc t ON t.ma_thuoc = i.ma_thuoc
               WHERE i.so_luong_thuoc > t.so_luong_ton)
    BEGIN
        THROW 50027, N'RB27: Số lượng kê vượt quá số lượng tồn kho.', 1;
    END;

    -- RB28: đơn giá lấy từ Thuoc.don_gia_thuoc lúc kê, bỏ qua giá truyền vào
    INSERT INTO Chi_tiet_don_thuoc (ma_ho_so, ma_thuoc, so_luong_thuoc, lieu_dung, don_gia_thuoc)
    SELECT i.ma_ho_so, i.ma_thuoc, i.so_luong_thuoc, i.lieu_dung, t.don_gia_thuoc
    FROM inserted i
    JOIN Thuoc t ON t.ma_thuoc = i.ma_thuoc;
END;

GO
ALTER TABLE [dbo].[Chi_tiet_don_thuoc] ENABLE TRIGGER [TG_CTDT_KeDon]
GO
/****** Object:  Trigger [dbo].[TG_Ho_so_kham_KiemTra]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE    TRIGGER [dbo].[TG_Ho_so_kham_KiemTra]
ON [dbo].[Ho_so_kham]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- RB20: bác sĩ khám phải có chức vụ Bác sĩ
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN Nhan_vien nv ON nv.ma_nv = i.ma_nv_kham
               WHERE nv.chuc_vu <> N'Bác sĩ')
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50020, N'RB20: Người khám phải là bác sĩ.', 1;
    END;

    -- RB34: người lập hồ sơ phải có chức vụ Lễ tân
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN Nhan_vien nv ON nv.ma_nv = i.ma_nv_tiep_nhan
               WHERE nv.chuc_vu <> N'Lễ tân')
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50034, N'RB34: Người lập hồ sơ khám phải là lễ tân.', 1;
    END;

    -- RB19 (cả thêm và sửa): hồ sơ sinh từ lịch hẹn phải cùng khách hàng và do bác sĩ của lịch làm việc đó khám
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN LICH_HEN      h ON h.ma_lich_hen = i.ma_lich_hen
               JOIN LICH_LAM_VIEC l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec
               WHERE i.ma_kh <> h.ma_kh OR i.ma_nv_kham <> l.ma_nv)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50019, N'RB19: Hồ sơ phải cùng khách hàng và cùng bác sĩ với lịch hẹn.', 1;
    END;

    -- RB19 (khi tạo mới): lịch hẹn phải đang Đã xác nhận, rồi tự chuyển sang Đã khám
    IF NOT EXISTS (SELECT 1 FROM deleted)
    BEGIN
        IF EXISTS (SELECT 1 FROM inserted i
                   JOIN LICH_HEN h ON h.ma_lich_hen = i.ma_lich_hen
                   WHERE h.trang_thai <> N'Đã xác nhận')
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50019, N'RB19: Chỉ tạo hồ sơ từ lịch hẹn đang ở trạng thái Đã xác nhận.', 1;
        END;

        UPDATE h
        SET    trang_thai = N'Đã khám'
        FROM   LICH_HEN h
        JOIN   inserted i ON i.ma_lich_hen = h.ma_lich_hen;
    END;
     -- RB41: khách vãng lai thì bác sĩ khám phải đang trong ca trực lúc tiếp nhận
    IF EXISTS (SELECT 1 FROM inserted i
               WHERE i.ma_lich_hen IS NULL
                 AND NOT EXISTS (
                     SELECT 1
                     FROM LICH_LAM_VIEC l
                     JOIN Ca_lam_viec   c ON c.ma_ca = l.ma_ca
                     WHERE l.ma_nv = i.ma_nv_kham
                       AND l.ngay_lam_viec = CAST(i.ngay_gio_kham AS DATE)
                       AND CAST(i.ngay_gio_kham AS TIME)
                           BETWEEN DATEADD(MINUTE, -30, c.gio_bat_dau) AND c.gio_ket_thuc))
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50041, N'RB41: Bác sĩ được chọn không trực ca này.', 1;
    END;
END;

GO
ALTER TABLE [dbo].[Ho_so_kham] ENABLE TRIGGER [TG_Ho_so_kham_KiemTra]
GO
/****** Object:  Trigger [dbo].[TG_Hoa_don_KiemTra]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[TG_Hoa_don_KiemTra]
ON [dbo].[Hoa_don]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- RB22: hồ sơ phải có chẩn đoán trước khi lập hóa đơn
    IF NOT EXISTS (SELECT 1 FROM deleted)
       AND EXISTS (SELECT 1 FROM inserted i
                   JOIN Ho_so_kham hs ON hs.ma_ho_so = i.ma_ho_so
                   WHERE hs.chan_doan_benh IS NULL OR hs.chan_doan_benh = N'')
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50022, N'RB22: Hồ sơ chưa có chẩn đoán, chưa lập được hóa đơn.', 1;
    END;

    -- RB32: người thu tiền phải có chức vụ Thu ngân
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN Nhan_vien nv ON nv.ma_nv = i.ma_nv
               WHERE nv.chuc_vu <> N'Thu ngân')
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50032, N'RB32: Người thu tiền phải là thu ngân.', 1;
    END;

    -- Hóa đơn đã thanh toán thì không được chuyển ngược về Chưa thanh toán (giữ đúng tồn kho theo RB33)
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN deleted d ON d.ma_hoa_don = i.ma_hoa_don
               WHERE d.trang_thai_thanh_toan = N'Đã thanh toán'
                 AND i.trang_thai_thanh_toan = N'Chưa thanh toán')
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50033, N'RB33: Hóa đơn đã thanh toán không được chuyển về Chưa thanh toán.', 1;
    END;

    -- RB29: tự tính tổng tiền = tiền công khám + tiền thuốc (chỉ khi chưa thanh toán)
    UPDATE hd
    SET    tong_tien = hs.tien_cong_kham
                     + ISNULL((SELECT SUM(ct.so_luong_thuoc * ct.don_gia_thuoc)
                               FROM Chi_tiet_don_thuoc ct
                               WHERE ct.ma_ho_so = hd.ma_ho_so), 0)
    FROM   Hoa_don hd
    JOIN   inserted   i  ON i.ma_hoa_don = hd.ma_hoa_don
    JOIN   Ho_so_kham hs ON hs.ma_ho_so  = hd.ma_ho_so
    WHERE  hd.trang_thai_thanh_toan = N'Chưa thanh toán';

    -- RB33: khi chuyển sang Đã thanh toán thì trừ tồn kho theo số lượng đã kê
    UPDATE t
    SET    so_luong_ton = t.so_luong_ton - x.sl
    FROM   Thuoc t
    JOIN  (SELECT ct.ma_thuoc, SUM(ct.so_luong_thuoc) AS sl
           FROM inserted i
           JOIN deleted d ON d.ma_hoa_don = i.ma_hoa_don
           JOIN Chi_tiet_don_thuoc ct ON ct.ma_ho_so = i.ma_ho_so
           WHERE d.trang_thai_thanh_toan = N'Chưa thanh toán'
             AND i.trang_thai_thanh_toan = N'Đã thanh toán'
           GROUP BY ct.ma_thuoc) x ON x.ma_thuoc = t.ma_thuoc;
END;

GO
ALTER TABLE [dbo].[Hoa_don] ENABLE TRIGGER [TG_Hoa_don_KiemTra]
GO
/****** Object:  Trigger [dbo].[TG_LICH_HEN_KiemTra]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER [dbo].[TG_LICH_HEN_KiemTra]
ON [dbo].[Lich_hen]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- RB14: khi khách vừa đặt, trạng thái phải là Đã đặt
    IF NOT EXISTS (SELECT 1 FROM deleted)
       AND EXISTS (SELECT 1 FROM inserted WHERE trang_thai <> N'Đã đặt')
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50014, N'RB14: Lịch hẹn mới phải có trạng thái Đã đặt.', 1;
    END;

    -- RB15: chỉ chuyển Đã đặt -> Đã xác nhận -> Đã khám; chỉ hủy khi đang Đã đặt hoặc Đã xác nhận
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN deleted d ON d.ma_lich_hen = i.ma_lich_hen
               WHERE i.trang_thai <> d.trang_thai
                 AND NOT (   (d.trang_thai = N'Đã đặt'      AND i.trang_thai IN (N'Đã xác nhận', N'Đã hủy'))
                          OR (d.trang_thai = N'Đã xác nhận' AND i.trang_thai IN (N'Đã khám', N'Đã hủy'))))
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50015, N'RB15: Chuyển trạng thái lịch hẹn không hợp lệ.', 1;
    END;

    -- RB16: ngay_gio_dat phải trước giờ bắt đầu ca của lịch làm việc được chọn
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN LICH_LAM_VIEC l ON l.ma_lich_lam_viec = i.ma_lich_lam_viec
               JOIN Ca_lam_viec  c ON c.ma_ca = l.ma_ca
               WHERE i.ngay_gio_dat >= CAST(l.ngay_lam_viec AS DATETIME) + CAST(c.gio_bat_dau AS DATETIME))
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50016, N'RB16: Phải đặt hẹn trước giờ bắt đầu ca.', 1;
    END;

    -- RB17 (phần tra bảng khác): người xác nhận phải có chức vụ Lễ tân
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN Nhan_vien nv ON nv.ma_nv = i.ma_nv
               WHERE nv.chuc_vu <> N'Lễ tân')
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50017, N'RB17: Người xác nhận lịch hẹn phải là lễ tân.', 1;
    END;
END;

GO
ALTER TABLE [dbo].[Lich_hen] ENABLE TRIGGER [TG_LICH_HEN_KiemTra]
GO
/****** Object:  Trigger [dbo].[TG_LICH_LAM_VIEC_KiemTra]    Script Date: 11/11/2026 9:21:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   TRIGGER [dbo].[TG_LICH_LAM_VIEC_KiemTra]
ON [dbo].[Lich_lam_viec]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- RB9: ngay_lam_viec không được là ngày trong quá khứ khi tạo mới
    IF NOT EXISTS (SELECT 1 FROM deleted)
       AND EXISTS (SELECT 1 FROM inserted WHERE ngay_lam_viec < CAST(GETDATE() AS DATE))
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50009, N'RB9: Không được tạo lịch làm việc cho ngày trong quá khứ.', 1;
    END;

    -- RB10: nhân viên trong lịch làm việc phải có chức vụ Bác sĩ
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN Nhan_vien nv ON nv.ma_nv = i.ma_nv
               WHERE nv.chuc_vu <> N'Bác sĩ')
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50010, N'RB10: Nhân viên được xếp lịch phải là bác sĩ.', 1;
    END;

    -- RB11: phòng của lịch làm việc phải thuộc khoa của bác sĩ
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN Nhan_vien nv ON nv.ma_nv   = i.ma_nv
               JOIN Phong_lam_viec p  ON p.ma_phong = i.ma_phong
               WHERE p.ma_khoa <> nv.ma_khoa)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50011, N'RB11: Phòng phải thuộc khoa của bác sĩ.', 1;
    END;
END;

GO
ALTER TABLE [dbo].[Lich_lam_viec] ENABLE TRIGGER [TG_LICH_LAM_VIEC_KiemTra]
GO
USE [master]
GO
ALTER DATABASE [PHONGKHAM] SET  READ_WRITE 
GO
