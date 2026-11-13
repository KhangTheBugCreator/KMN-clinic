using System.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Api.Data;

/// <summary>
/// Thống kê và báo cáo (M9), chỉ đọc. Khoảng ngày truyền vào là [tu, den] (cả hai đầu), đổi thành [tu, den + 1 ngày) khi lọc datetime.
/// Doanh thu = tổng tong_tien của hóa đơn Đã thanh toán theo ngay_gio_thanh_toan.
/// </summary>
public class ThongKeRepository
{
    private readonly Db _db;
    private readonly int _nguongSapHet;

    public ThongKeRepository(Db db, IConfiguration config)
    {
        _db = db;
        _nguongSapHet = config.GetValue("PhongKham:NguongThuocSapHet", 20);
    }

    private static SqlCommand Ky(SqlCommand c, DateOnly tu, DateOnly den) => c
        .P("@tu", SqlDbType.DateTime, 0, tu.ToDateTime(TimeOnly.MinValue))
        .P("@denSau", SqlDbType.DateTime, 0, den.AddDays(1).ToDateTime(TimeOnly.MinValue))
        .P("@tuNgay", SqlDbType.Date, 0, tu.ToDateTime(TimeOnly.MinValue))
        .P("@denNgay", SqlDbType.Date, 0, den.ToDateTime(TimeOnly.MinValue));

    public async Task<TongQuanThongKe> TongQuanAsync(DateOnly tu, DateOnly den, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = Ky(conn.Cmd(@"
SELECT
  (SELECT COUNT(*) FROM Nhan_vien),
  (SELECT COUNT(*) FROM Nhan_vien WHERE chuc_vu = N'Bác sĩ'),
  (SELECT COUNT(*) FROM Khach_hang),
  (SELECT COUNT(DISTINCT ma_kh) FROM Ho_so_kham WHERE ngay_gio_kham >= @tu AND ngay_gio_kham < @denSau),
  (SELECT COUNT(*) FROM Lich_hen h JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec WHERE l.ngay_lam_viec BETWEEN @tuNgay AND @denNgay),
  (SELECT COUNT(*) FROM Lich_hen h JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec WHERE l.ngay_lam_viec BETWEEN @tuNgay AND @denNgay AND h.trang_thai = N'Đã khám'),
  (SELECT COUNT(*) FROM Lich_hen h JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec WHERE l.ngay_lam_viec BETWEEN @tuNgay AND @denNgay AND h.trang_thai = N'Đã hủy'),
  (SELECT COUNT(*) FROM Lich_hen WHERE trang_thai = N'Đã đặt'),
  (SELECT COUNT(*) FROM Ho_so_kham WHERE ngay_gio_kham >= @tu AND ngay_gio_kham < @denSau),
  (SELECT COUNT(*) FROM Ho_so_kham WHERE ngay_gio_kham >= @tu AND ngay_gio_kham < @denSau AND ma_lich_hen IS NULL),
  (SELECT COUNT(*) FROM Hoa_don WHERE trang_thai_thanh_toan = N'Đã thanh toán' AND ngay_gio_thanh_toan >= @tu AND ngay_gio_thanh_toan < @denSau),
  (SELECT ISNULL(SUM(tong_tien), 0) FROM Hoa_don WHERE trang_thai_thanh_toan = N'Đã thanh toán' AND ngay_gio_thanh_toan >= @tu AND ngay_gio_thanh_toan < @denSau),
  (SELECT COUNT(*) FROM Hoa_don WHERE trang_thai_thanh_toan = N'Chưa thanh toán'),
  (SELECT ISNULL(SUM(tong_tien), 0) FROM Hoa_don WHERE trang_thai_thanh_toan = N'Chưa thanh toán'),
  (SELECT COUNT(*) FROM Thuoc WHERE so_luong_ton <= @nguong)"), tu, den).P("@nguong", _nguongSapHet);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        await r.ReadAsync(ct);
        return new(tu, den, r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3), r.GetInt32(4), r.GetInt32(5), r.GetInt32(6),
            r.GetInt32(7), r.GetInt32(8), r.GetInt32(9), r.GetInt32(10), r.GetDecimal(11), r.GetInt32(12), r.GetDecimal(13),
            r.GetInt32(14), _nguongSapHet);
    }

    /// <summary>Doanh thu theo từng ngày (yyyy-MM-dd) hoặc từng tháng (yyyy-MM), có đủ các mốc không có doanh thu (giá trị 0).</summary>
    public async Task<List<DiemDoanhThu>> DoanhThuAsync(DateOnly tu, DateOnly den, bool theoThang, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = Ky(conn.Cmd(@"
SELECT CAST(ngay_gio_thanh_toan AS date), SUM(tong_tien), COUNT(*)
FROM Hoa_don WHERE trang_thai_thanh_toan = N'Đã thanh toán' AND ngay_gio_thanh_toan >= @tu AND ngay_gio_thanh_toan < @denSau
GROUP BY CAST(ngay_gio_thanh_toan AS date)"), tu, den);
        var theoNgay = (await DocDanhSach(cmd, r => (Ngay: DateOnly.FromDateTime(r.GetDateTime(0)), Tien: r.GetDecimal(1), So: r.GetInt32(2)), ct))
            .ToDictionary(x => x.Ngay);
        var kq = new List<DiemDoanhThu>();
        if (!theoThang)
        {
            for (var d = tu; d <= den; d = d.AddDays(1))
                kq.Add(theoNgay.TryGetValue(d, out var x) ? new(d.ToString("yyyy-MM-dd"), x.Tien, x.So) : new(d.ToString("yyyy-MM-dd"), 0, 0));
            return kq;
        }
        for (var m = new DateOnly(tu.Year, tu.Month, 1); m <= den; m = m.AddMonths(1))
        {
            var trongThang = theoNgay.Values.Where(x => x.Ngay.Year == m.Year && x.Ngay.Month == m.Month).ToList();
            kq.Add(new(m.ToString("yyyy-MM"), trongThang.Sum(x => x.Tien), trongThang.Sum(x => x.So)));
        }
        return kq;
    }

    public async Task<Dictionary<string, int>> LichHenTheoTrangThaiAsync(DateOnly tu, DateOnly den, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = Ky(conn.Cmd(@"
SELECT h.trang_thai, COUNT(*) FROM Lich_hen h JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec
WHERE l.ngay_lam_viec BETWEEN @tuNgay AND @denNgay GROUP BY h.trang_thai"), tu, den);
        var kq = TrangThaiLichHen.TatCa.ToDictionary(t => t, _ => 0);
        foreach (var (tt, so) in await DocDanhSach(cmd, r => (r.GetString(0), r.GetInt32(1)), ct)) kq[tt] = so;
        return kq;
    }

    public async Task<List<DiemLuotKham>> LuotKhamAsync(DateOnly tu, DateOnly den, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = Ky(conn.Cmd(@"
SELECT CAST(ngay_gio_kham AS date), SUM(CASE WHEN ma_lich_hen IS NOT NULL THEN 1 ELSE 0 END), SUM(CASE WHEN ma_lich_hen IS NULL THEN 1 ELSE 0 END)
FROM Ho_so_kham WHERE ngay_gio_kham >= @tu AND ngay_gio_kham < @denSau GROUP BY CAST(ngay_gio_kham AS date)"), tu, den);
        var ds = (await DocDanhSach(cmd, r => new DiemLuotKham(DateOnly.FromDateTime(r.GetDateTime(0)), r.GetInt32(1), r.GetInt32(2)), ct))
            .ToDictionary(x => x.Ngay);
        var kq = new List<DiemLuotKham>();
        for (var d = tu; d <= den; d = d.AddDays(1)) kq.Add(ds.TryGetValue(d, out var x) ? x : new(d, 0, 0));
        return kq;
    }

    /// <summary>Thuốc đã xuất nhiều nhất: đơn thuốc của hóa đơn đã thanh toán trong kỳ (theo ngày thanh toán).</summary>
    public async Task<List<ThuocKeNhieu>> ThuocKeNhieuAsync(DateOnly tu, DateOnly den, int top, CancellationToken ct)
        => (await SuDungThuocAsync(tu, den, ct)).Take(top)
            .Select(x => new ThuocKeNhieu(x.MaThuoc, x.TenThuoc, x.HamLuong, x.DonViTinh, x.TongSoLuong, x.ThanhTien)).ToList();

    /* ---------- Báo cáo ---------- */

    public async Task<List<BcDoanhThuDong>> BcDoanhThuAsync(DateOnly tu, DateOnly den, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = Ky(conn.Cmd(@"
SELECT d.ma_hoa_don, d.ngay_gio_thanh_toan, k.ma_kh, k.ho_ten_kh, bs.ho_ten_nv, kh.ten_khoa, hs.tien_cong_kham,
       ISNULL((SELECT SUM(ct.so_luong_thuoc * ct.don_gia_thuoc) FROM Chi_tiet_don_thuoc ct WHERE ct.ma_ho_so = hs.ma_ho_so), 0),
       d.tong_tien, tn.ho_ten_nv
FROM Hoa_don d
JOIN Ho_so_kham hs ON hs.ma_ho_so = d.ma_ho_so
JOIN Khach_hang k ON k.ma_kh = hs.ma_kh
JOIN Nhan_vien bs ON bs.ma_nv = hs.ma_nv_kham
JOIN Khoa kh ON kh.ma_khoa = bs.ma_khoa
JOIN Nhan_vien tn ON tn.ma_nv = d.ma_nv
WHERE d.trang_thai_thanh_toan = N'Đã thanh toán' AND d.ngay_gio_thanh_toan >= @tu AND d.ngay_gio_thanh_toan < @denSau
ORDER BY d.ngay_gio_thanh_toan, d.ma_hoa_don"), tu, den);
        return await DocDanhSach(cmd, r => new BcDoanhThuDong(r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetString(3), r.GetString(4),
            r.GetString(5), r.GetDecimal(6), r.GetDecimal(7), r.GetDecimal(8), r.GetString(9)), ct);
    }

    public async Task<List<BcLuotKhamDong>> BcLuotKhamAsync(DateOnly tu, DateOnly den, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = Ky(conn.Cmd(@"
SELECT CAST(hs.ngay_gio_kham AS date), kh.ten_khoa, bs.ma_nv, bs.ho_ten_nv,
       SUM(CASE WHEN hs.ma_lich_hen IS NOT NULL THEN 1 ELSE 0 END), SUM(CASE WHEN hs.ma_lich_hen IS NULL THEN 1 ELSE 0 END), COUNT(*)
FROM Ho_so_kham hs JOIN Nhan_vien bs ON bs.ma_nv = hs.ma_nv_kham JOIN Khoa kh ON kh.ma_khoa = bs.ma_khoa
WHERE hs.ngay_gio_kham >= @tu AND hs.ngay_gio_kham < @denSau
GROUP BY CAST(hs.ngay_gio_kham AS date), kh.ten_khoa, bs.ma_nv, bs.ho_ten_nv
ORDER BY 1, 2, 4"), tu, den);
        return await DocDanhSach(cmd, r => new BcLuotKhamDong(DateOnly.FromDateTime(r.GetDateTime(0)), r.GetString(1), r.GetString(2), r.GetString(3),
            r.GetInt32(4), r.GetInt32(5), r.GetInt32(6)), ct);
    }

    public async Task<List<BcLichHenDong>> BcLichHenAsync(DateOnly tu, DateOnly den, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = Ky(conn.Cmd(@"
SELECT h.ma_lich_hen, h.ngay_gio_dat, k.ho_ten_kh, k.sdt_kh, bs.ho_ten_nv, kh.ten_khoa, l.ngay_lam_viec, c.ten_ca, c.gio_bat_dau, c.gio_ket_thuc, h.trang_thai
FROM Lich_hen h
JOIN Khach_hang k ON k.ma_kh = h.ma_kh
JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec
JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca
JOIN Nhan_vien bs ON bs.ma_nv = l.ma_nv
JOIN Khoa kh ON kh.ma_khoa = bs.ma_khoa
WHERE l.ngay_lam_viec BETWEEN @tuNgay AND @denNgay
ORDER BY l.ngay_lam_viec, c.gio_bat_dau, h.ngay_gio_dat"), tu, den);
        return await DocDanhSach(cmd, r => new BcLichHenDong(r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5),
            DateOnly.FromDateTime(r.GetDateTime(6)), r.GetString(7), TimeOnly.FromTimeSpan(r.GetTimeSpan(8)), TimeOnly.FromTimeSpan(r.GetTimeSpan(9)), r.GetString(10)), ct);
    }

    public async Task<List<BcTonKhoDong>> BcTonKhoAsync(CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(@"
SELECT ma_thuoc, ten_thuoc, nsx, ham_luong, don_vi_tinh, don_gia_thuoc, so_luong_ton FROM Thuoc ORDER BY so_luong_ton, ten_thuoc");
        return await DocDanhSach(cmd, r =>
        {
            var ton = r.GetInt32(6);
            return new BcTonKhoDong(r.GetString(0), r.GetString(1), r.GetString(2), r.GetDecimal(3), r.GetString(4), r.GetDecimal(5), ton,
                ton == 0 ? "Hết hàng" : ton <= _nguongSapHet ? "Sắp hết" : "");
        }, ct);
    }

    public async Task<List<BcSuDungThuocDong>> SuDungThuocAsync(DateOnly tu, DateOnly den, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = Ky(conn.Cmd(@"
SELECT t.ma_thuoc, t.ten_thuoc, t.ham_luong, t.don_vi_tinh, SUM(ct.so_luong_thuoc), COUNT(*), SUM(ct.so_luong_thuoc * ct.don_gia_thuoc)
FROM Chi_tiet_don_thuoc ct
JOIN Hoa_don d ON d.ma_ho_so = ct.ma_ho_so
JOIN Thuoc t ON t.ma_thuoc = ct.ma_thuoc
WHERE d.trang_thai_thanh_toan = N'Đã thanh toán' AND d.ngay_gio_thanh_toan >= @tu AND d.ngay_gio_thanh_toan < @denSau
GROUP BY t.ma_thuoc, t.ten_thuoc, t.ham_luong, t.don_vi_tinh
ORDER BY SUM(ct.so_luong_thuoc) DESC, t.ten_thuoc"), tu, den);
        return await DocDanhSach(cmd, r => new BcSuDungThuocDong(r.GetString(0), r.GetString(1), r.GetDecimal(2), r.GetString(3),
            r.GetInt32(4), r.GetInt32(5), r.GetDecimal(6)), ct);
    }

    private static async Task<List<T>> DocDanhSach<T>(SqlCommand cmd, Func<SqlDataReader, T> doc, CancellationToken ct)
    {
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var ds = new List<T>();
        while (await r.ReadAsync(ct)) ds.Add(doc(r));
        return ds;
    }
}
