using System.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Api.Data;

/// <summary>
/// Nghiệp vụ bác sĩ (M7). Bác sĩ chỉ thấy và sửa hồ sơ mình khám (ma_nv_kham lấy từ token); hồ sơ người khác trả 404.
/// Hồ sơ đã có hóa đơn là đã hoàn tất và bị khóa (Q4). Trigger vẫn chặn ở database:
/// TG_CTDT_KeDon (RB27 tồn kho, RB28 lấy đơn giá từ danh mục), TG_CTDT_CapNhat (RB27, RB28, RB33, tính lại RB29),
/// TG_Hoa_don_KiemTra (RB22 phải có chẩn đoán, RB29 tổng tiền). Backend kiểm tra trước để báo lỗi rõ ràng.
/// </summary>
public class BacSiRepository
{
    private readonly Db _db;
    public BacSiRepository(Db db) => _db = db;

    private static string? Rong(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Trạng thái suy ra: có hóa đơn = Hoàn tất/Đã thanh toán; có chẩn đoán hoặc thuốc = Đang khám; còn lại Chờ khám.</summary>
    private const string BieuThucTrangThai = @"CASE
  WHEN d.trang_thai_thanh_toan = N'Đã thanh toán' THEN N'Đã thanh toán'
  WHEN d.ma_hoa_don IS NOT NULL THEN N'Hoàn tất'
  WHEN hs.chan_doan_benh IS NOT NULL OR EXISTS (SELECT 1 FROM Chi_tiet_don_thuoc x WHERE x.ma_ho_so = hs.ma_ho_so) THEN N'Đang khám'
  ELSE N'Chờ khám' END";

    private const string SqlDanhSach = $@"
SELECT hs.ma_ho_so, hs.ngay_gio_kham, hs.trieu_chung, hs.chan_doan_benh, hs.ma_lich_hen,
       k.ma_kh, k.ho_ten_kh, k.gioi_tinh_kh, k.ngay_sinh_kh,
       (SELECT COUNT(*) FROM Chi_tiet_don_thuoc x WHERE x.ma_ho_so = hs.ma_ho_so),
       {BieuThucTrangThai}, d.ma_hoa_don, d.tong_tien
FROM Ho_so_kham hs
JOIN Khach_hang k ON k.ma_kh = hs.ma_kh
LEFT JOIN Hoa_don d ON d.ma_ho_so = hs.ma_ho_so";

    private static HoSoBacSiItem DocDong(SqlDataReader r) => new(
        r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetStringOrNull(3), r.GetStringOrNull(4),
        r.GetString(5), r.GetString(6), r.GetString(7), DateOnly.FromDateTime(r.GetDateTime(8)),
        r.GetInt32(9), r.GetString(10), r.GetStringOrNull(11), r.IsDBNull(12) ? null : r.GetDecimal(12));

    /* ---------- Đọc ---------- */

    /// <summary>Hàng chờ: mọi hồ sơ của bác sĩ chưa có hóa đơn (kể cả hồ sơ ngày trước còn sót), theo giờ tiếp nhận.</summary>
    public async Task<List<HoSoBacSiItem>> HangChoAsync(string maBs, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlDanhSach + @"
WHERE hs.ma_nv_kham = @bs AND d.ma_hoa_don IS NULL
ORDER BY hs.ngay_gio_kham, hs.ma_ho_so").P("@bs", SqlDbType.VarChar, 10, maBs);
        return await DocDanhSach(cmd, DocDong, ct);
    }

    public async Task<TrangKetQua<HoSoBacSiItem>> LichSuAsync(string maBs, HoSoBacSiQuery q, CancellationToken ct)
    {
        var where = $@"
WHERE hs.ma_nv_kham = @bs
  AND (@tu IS NULL OR hs.ngay_gio_kham >= @tu) AND (@den IS NULL OR hs.ngay_gio_kham < DATEADD(day, 1, @den))
  AND (@q IS NULL OR hs.ma_ho_so LIKE @q OR k.ho_ten_kh LIKE @q OR k.ma_kh LIKE @q OR hs.chan_doan_benh LIKE @q)
  AND (@tt IS NULL OR {BieuThucTrangThai} = @tt)";
        await using var conn = await _db.OpenAsync(ct);
        SqlCommand Gan(SqlCommand c) => c.P("@bs", SqlDbType.VarChar, 10, maBs)
            .P("@tu", SqlDbType.DateTime, 0, q.TuNgay?.ToDateTime(TimeOnly.MinValue))
            .P("@den", SqlDbType.DateTime, 0, q.DenNgay?.ToDateTime(TimeOnly.MinValue))
            .P("@q", SqlDbType.NVarChar, 60, q.MauTim).P("@tt", SqlDbType.NVarChar, 20, Rong(q.TrangThai));
        var total = (int)(await Gan(conn.Cmd(@"SELECT COUNT(*) FROM Ho_so_kham hs JOIN Khach_hang k ON k.ma_kh = hs.ma_kh
LEFT JOIN Hoa_don d ON d.ma_ho_so = hs.ma_ho_so" + where)).ExecuteScalarAsync(ct))!;
        await using var cmd = Gan(conn.Cmd(SqlDanhSach + where + @"
ORDER BY hs.ngay_gio_kham DESC, hs.ma_ho_so DESC
OFFSET @bo ROWS FETCH NEXT @lay ROWS ONLY")).P("@bo", q.BoQua).P("@lay", q.CoTrangHopLe);
        return new(await DocDanhSach(cmd, DocDong, ct), total, q.TrangHopLe, q.CoTrangHopLe);
    }

    /// <summary>Chi tiết hồ sơ kèm đơn thuốc và hóa đơn. Null nếu không có hoặc không phải hồ sơ của bác sĩ này.</summary>
    public async Task<HoSoChiTiet?> ChiTietAsync(string ma, string maBs, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await ChiTietAsync(conn, null, ma, maBs, ct);
    }

    private static async Task<HoSoChiTiet?> ChiTietAsync(SqlConnection conn, SqlTransaction? tx, string ma, string maBs, CancellationToken ct)
    {
        HoSoChiTiet? hs;
        await using (var cmd = conn.Cmd($@"
SELECT hs.ma_ho_so, hs.ngay_gio_kham, hs.trieu_chung, hs.chan_doan_benh, hs.loi_dan, hs.tien_cong_kham, hs.ma_lich_hen,
       k.ma_kh, k.ho_ten_kh, k.gioi_tinh_kh, k.ngay_sinh_kh, bs.ma_nv, bs.ho_ten_nv, kh.ten_khoa, lt.ho_ten_nv,
       {BieuThucTrangThai}, d.ma_hoa_don, d.tong_tien, d.trang_thai_thanh_toan, d.ngay_gio_thanh_toan
FROM Ho_so_kham hs
JOIN Khach_hang k ON k.ma_kh = hs.ma_kh
JOIN Nhan_vien bs ON bs.ma_nv = hs.ma_nv_kham
JOIN Khoa kh ON kh.ma_khoa = bs.ma_khoa
JOIN Nhan_vien lt ON lt.ma_nv = hs.ma_nv_tiep_nhan
LEFT JOIN Hoa_don d ON d.ma_ho_so = hs.ma_ho_so
WHERE hs.ma_ho_so = @ma AND hs.ma_nv_kham = @bs", tx).P("@ma", SqlDbType.VarChar, 10, ma).P("@bs", SqlDbType.VarChar, 10, maBs))
        await using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct)) return null;
            var hoaDon = r.IsDBNull(16) ? null
                : new HoaDonTomTat(r.GetString(16), r.GetDecimal(17), r.GetString(18), r.IsDBNull(19) ? null : r.GetDateTime(19));
            hs = new HoSoChiTiet(r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetStringOrNull(3), r.GetStringOrNull(4),
                r.GetDecimal(5), r.GetStringOrNull(6), r.GetString(7), r.GetString(8), r.GetString(9),
                DateOnly.FromDateTime(r.GetDateTime(10)), r.GetString(11), r.GetString(12), r.GetString(13), r.GetString(14),
                r.GetString(15), new(), 0, hoaDon);
        }
        var don = await DonThuocAsync(conn, tx, ma, ct);
        return hs with { DonThuoc = don, TienThuoc = don.Sum(t => t.ThanhTien) };
    }

    private static async Task<List<ThuocTrongDon>> DonThuocAsync(SqlConnection conn, SqlTransaction? tx, string maHoSo, CancellationToken ct)
    {
        await using var cmd = conn.Cmd(@"
SELECT ct.ma_thuoc, t.ten_thuoc, t.ham_luong, t.don_vi_tinh, ct.so_luong_thuoc, ct.lieu_dung, ct.don_gia_thuoc,
       ct.so_luong_thuoc * ct.don_gia_thuoc, t.so_luong_ton
FROM Chi_tiet_don_thuoc ct JOIN Thuoc t ON t.ma_thuoc = ct.ma_thuoc
WHERE ct.ma_ho_so = @ma
ORDER BY t.ten_thuoc", tx).P("@ma", SqlDbType.VarChar, 10, maHoSo);
        return await DocDanhSach(cmd, r => new ThuocTrongDon(r.GetString(0), r.GetString(1), r.GetDecimal(2), r.GetString(3),
            r.GetInt32(4), r.GetString(5), r.GetDecimal(6), r.GetDecimal(7), r.GetInt32(8)), ct);
    }

    /// <summary>
    /// Các lần khám đã hoàn tất trước đó của bệnh nhân trong hồ sơ <paramref name="ma"/> (mọi bác sĩ), chỉ đọc (Q13).
    /// Chỉ xem được khi hồ sơ <paramref name="ma"/> là của bác sĩ này. Null nếu không phải.
    /// </summary>
    public async Task<List<LanKhamTruoc>?> LanKhamTruocAsync(string ma, string maBs, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        string maKh; DateTime moc;
        await using (var c = conn.Cmd("SELECT ma_kh, ngay_gio_kham FROM Ho_so_kham WHERE ma_ho_so = @ma AND ma_nv_kham = @bs")
            .P("@ma", SqlDbType.VarChar, 10, ma).P("@bs", SqlDbType.VarChar, 10, maBs))
        await using (var r = await c.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct)) return null;
            maKh = r.GetString(0); moc = r.GetDateTime(1);
        }
        var ds = new List<LanKhamTruoc>();
        await using (var cmd = conn.Cmd(@"
SELECT TOP 20 hs.ma_ho_so, hs.ngay_gio_kham, bs.ho_ten_nv, kh.ten_khoa, hs.trieu_chung, hs.chan_doan_benh, hs.loi_dan
FROM Ho_so_kham hs
JOIN Nhan_vien bs ON bs.ma_nv = hs.ma_nv_kham
JOIN Khoa kh ON kh.ma_khoa = bs.ma_khoa
WHERE hs.ma_kh = @kh AND hs.ma_ho_so <> @ma AND hs.ngay_gio_kham <= @moc
  AND EXISTS (SELECT 1 FROM Hoa_don d WHERE d.ma_ho_so = hs.ma_ho_so)
ORDER BY hs.ngay_gio_kham DESC").P("@kh", SqlDbType.VarChar, 10, maKh).P("@ma", SqlDbType.VarChar, 10, ma).P("@moc", SqlDbType.DateTime, 0, moc))
            ds = await DocDanhSach(cmd, r => new LanKhamTruoc(r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetString(3),
                r.GetString(4), r.GetStringOrNull(5), r.GetStringOrNull(6), new()), ct);
        foreach (var lan in ds)
            lan.Thuoc.AddRange((await DonThuocAsync(conn, null, lan.MaHoSo, ct))
                .Select(t => $"{t.TenThuoc} {t.HamLuong:0.##} × {t.SoLuong} {t.DonViTinh.ToLowerInvariant()} ({t.LieuDung})"));
        return ds;
    }

    public async Task<TongQuanBacSi> TongQuanAsync(string maBs, DateTime bayGio, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        int cho, dang, xong, tong;
        await using (var cmd = conn.Cmd($@"
SELECT
  SUM(CASE WHEN x.tt = N'Chờ khám' THEN 1 ELSE 0 END),
  SUM(CASE WHEN x.tt = N'Đang khám' THEN 1 ELSE 0 END),
  SUM(CASE WHEN x.tt IN (N'Hoàn tất', N'Đã thanh toán') AND x.ngay = @homNay THEN 1 ELSE 0 END),
  SUM(CASE WHEN x.ngay = @homNay THEN 1 ELSE 0 END)
FROM (SELECT {BieuThucTrangThai} AS tt, CAST(hs.ngay_gio_kham AS date) AS ngay
      FROM Ho_so_kham hs LEFT JOIN Hoa_don d ON d.ma_ho_so = hs.ma_ho_so
      WHERE hs.ma_nv_kham = @bs AND (d.ma_hoa_don IS NULL OR hs.ngay_gio_kham >= @homNay)) x")
            .P("@bs", SqlDbType.VarChar, 10, maBs).P("@homNay", SqlDbType.Date, 0, bayGio.Date))
        await using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            await r.ReadAsync(ct);
            int So(int i) => r.IsDBNull(i) ? 0 : r.GetInt32(i);
            cho = So(0); dang = So(1); xong = So(2); tong = So(3);
        }

        // Ca chưa kết thúc từ hôm nay đến 7 ngày tới
        List<LichTrucSapToi> lich;
        await using (var cmd = conn.Cmd(@"
SELECT l.ma_lich_lam_viec, l.ngay_lam_viec, c.ten_ca, c.gio_bat_dau, c.gio_ket_thuc, p.ten_phong,
       (SELECT COUNT(*) FROM Lich_hen h WHERE h.ma_lich_lam_viec = l.ma_lich_lam_viec AND h.trang_thai <> N'Đã hủy')
FROM Lich_lam_viec l
JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca
JOIN Phong_lam_viec p ON p.ma_phong = l.ma_phong
WHERE l.ma_nv = @bs AND l.ngay_lam_viec <= DATEADD(day, 7, @homNay)
  AND (l.ngay_lam_viec > @homNay OR (l.ngay_lam_viec = @homNay AND c.gio_ket_thuc > @gioNay))
ORDER BY l.ngay_lam_viec, c.gio_bat_dau")
            .P("@bs", SqlDbType.VarChar, 10, maBs).P("@homNay", SqlDbType.Date, 0, bayGio.Date).P("@gioNay", SqlDbType.Time, 0, bayGio.TimeOfDay))
            lich = await DocDanhSach(cmd, r => new LichTrucSapToi(r.GetString(0), DateOnly.FromDateTime(r.GetDateTime(1)), r.GetString(2),
                TimeOnly.FromTimeSpan(r.GetTimeSpan(3)), TimeOnly.FromTimeSpan(r.GetTimeSpan(4)), r.GetString(5), r.GetInt32(6)), ct);

        // Ca hiện tại: tính sớm 30 phút trước giờ bắt đầu, giống quy tắc nhận khách (RB41)
        var homNay = DateOnly.FromDateTime(bayGio);
        var gio = TimeOnly.FromDateTime(bayGio);
        var hienTai = lich.FirstOrDefault(l => l.Ngay == homNay && gio >= l.GioBatDau.AddMinutes(-30) && gio <= l.GioKetThuc);
        return new(cho, dang, xong, tong, hienTai, lich);
    }

    /* ---------- Ghi: mọi thao tác khóa dòng hồ sơ trước để không chen với Hoàn tất ---------- */

    /// <summary>Khóa hồ sơ của bác sĩ. 404 nếu không có hoặc của bác sĩ khác; 409 nếu đã hoàn tất (Q4).</summary>
    private static async Task KhoaHoSoDangKham(SqlConnection conn, SqlTransaction tx, string ma, string maBs, CancellationToken ct)
    {
        await using var c = conn.Cmd(@"
SELECT hs.ma_nv_kham, (SELECT COUNT(*) FROM Hoa_don d WHERE d.ma_ho_so = hs.ma_ho_so)
FROM Ho_so_kham hs WITH (UPDLOCK, ROWLOCK) WHERE hs.ma_ho_so = @ma", tx).P("@ma", SqlDbType.VarChar, 10, ma);
        await using var r = await c.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct) || r.GetString(0) != maBs) throw AppException.NotFound("Không tìm thấy hồ sơ khám của bạn.");
        if (r.GetInt32(1) > 0) throw AppException.Conflict("Hồ sơ đã hoàn tất khám và đã lập hóa đơn, không sửa được nữa.");
    }

    private static Task LuuThongTinKham(SqlConnection conn, SqlTransaction tx, string ma, KhamBenhRequest req, CancellationToken ct)
        => conn.Cmd(@"
UPDATE Ho_so_kham SET trieu_chung = @tc, chan_doan_benh = @cd, loi_dan = @ld, tien_cong_kham = @tien
WHERE ma_ho_so = @ma", tx)
            .P("@ma", SqlDbType.VarChar, 10, ma).P("@tc", SqlDbType.NVarChar, 255, req.TrieuChung!.Trim())
            .P("@cd", SqlDbType.NVarChar, 255, Rong(req.ChanDoanBenh)).P("@ld", SqlDbType.NVarChar, 255, Rong(req.LoiDan))
            .P("@tien", SqlDbType.Decimal, 0, req.TienCongKham!.Value).ExecuteNonQueryAsync(ct);

    /// <summary>Lưu nháp thông tin khám (chẩn đoán được để trống).</summary>
    public Task<HoSoChiTiet> LuuAsync(string ma, string maBs, KhamBenhRequest req, CancellationToken ct)
        => _db.InTransactionAsync(async (conn, tx) =>
        {
            await KhoaHoSoDangKham(conn, tx, ma, maBs, ct);
            await LuuThongTinKham(conn, tx, ma, req, ct);
            return (await ChiTietAsync(conn, tx, ma, maBs, ct))!;
        }, ct: ct);

    /// <summary>Đọc thuốc trong danh mục: (tên, tồn). 404 nếu không có (trigger INSTEAD OF sẽ âm thầm bỏ qua thuốc không tồn tại).</summary>
    private static async Task<(string Ten, int Ton)> DocThuoc(SqlConnection conn, SqlTransaction tx, string maThuoc, CancellationToken ct)
    {
        await using var c = conn.Cmd("SELECT ten_thuoc, so_luong_ton FROM Thuoc WHERE ma_thuoc = @t", tx).P("@t", SqlDbType.VarChar, 10, maThuoc);
        await using var r = await c.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) throw AppException.NotFound("Không tìm thấy thuốc đã chọn.");
        return (r.GetString(0), r.GetInt32(1));
    }

    private static void KiemTraTon(string ten, int ton, int soLuong)
    {
        if (soLuong > ton)
            throw AppException.BadRequest($"{ten} chỉ còn {ton} trong kho, không kê được {soLuong} (RB27).");
    }

    /// <summary>Kê thêm một thuốc. Đơn giá do trigger lấy từ danh mục lúc kê (RB28).</summary>
    public Task<HoSoChiTiet> KeThuocAsync(string ma, string maBs, KeThuocRequest req, CancellationToken ct)
        => _db.InTransactionAsync(async (conn, tx) =>
        {
            await KhoaHoSoDangKham(conn, tx, ma, maBs, ct);
            var (ten, ton) = await DocThuoc(conn, tx, req.MaThuoc!, ct);
            await using (var c = conn.Cmd("SELECT COUNT(*) FROM Chi_tiet_don_thuoc WHERE ma_ho_so = @ma AND ma_thuoc = @t", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma).P("@t", SqlDbType.VarChar, 10, req.MaThuoc))
                if ((int)(await c.ExecuteScalarAsync(ct))! > 0)
                    throw AppException.Conflict($"{ten} đã có trong đơn. Hãy sửa số lượng ở dòng đó.");
            KiemTraTon(ten, ton, req.SoLuong!.Value);
            // don_gia_thuoc gửi 0 chỉ để đủ cột: trigger INSTEAD OF bỏ qua và lấy giá từ Thuoc
            await conn.Cmd(@"
INSERT INTO Chi_tiet_don_thuoc (ma_ho_so, ma_thuoc, so_luong_thuoc, lieu_dung, don_gia_thuoc)
SELECT @ma, ma_thuoc, @sl, @ld, don_gia_thuoc FROM Thuoc WHERE ma_thuoc = @t", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma).P("@t", SqlDbType.VarChar, 10, req.MaThuoc)
                .P("@sl", SqlDbType.Int, 0, req.SoLuong).P("@ld", SqlDbType.NVarChar, 255, req.LieuDung!.Trim())
                .ExecuteNonQueryAsync(ct);
            return (await ChiTietAsync(conn, tx, ma, maBs, ct))!;
        }, ct: ct);

    /// <summary>Sửa số lượng, liều dùng. Đơn giá giữ nguyên như lúc kê (RB28).</summary>
    public Task<HoSoChiTiet> SuaThuocAsync(string ma, string maThuoc, string maBs, SuaThuocTrongDonRequest req, CancellationToken ct)
        => _db.InTransactionAsync(async (conn, tx) =>
        {
            await KhoaHoSoDangKham(conn, tx, ma, maBs, ct);
            var (ten, ton) = await DocThuoc(conn, tx, maThuoc, ct);
            KiemTraTon(ten, ton, req.SoLuong!.Value);
            var n = await conn.Cmd("UPDATE Chi_tiet_don_thuoc SET so_luong_thuoc = @sl, lieu_dung = @ld WHERE ma_ho_so = @ma AND ma_thuoc = @t", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma).P("@t", SqlDbType.VarChar, 10, maThuoc)
                .P("@sl", SqlDbType.Int, 0, req.SoLuong).P("@ld", SqlDbType.NVarChar, 255, req.LieuDung!.Trim())
                .ExecuteNonQueryAsync(ct);
            if (n == 0) throw AppException.NotFound($"{ten} không có trong đơn.");
            return (await ChiTietAsync(conn, tx, ma, maBs, ct))!;
        }, ct: ct);

    public Task<HoSoChiTiet> XoaThuocAsync(string ma, string maThuoc, string maBs, CancellationToken ct)
        => _db.InTransactionAsync(async (conn, tx) =>
        {
            await KhoaHoSoDangKham(conn, tx, ma, maBs, ct);
            var n = await conn.Cmd("DELETE FROM Chi_tiet_don_thuoc WHERE ma_ho_so = @ma AND ma_thuoc = @t", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma).P("@t", SqlDbType.VarChar, 10, maThuoc).ExecuteNonQueryAsync(ct);
            if (n == 0) throw AppException.NotFound("Thuốc này không có trong đơn.");
            return (await ChiTietAsync(conn, tx, ma, maBs, ct))!;
        }, ct: ct);

    /// <summary>
    /// Hoàn tất khám (Q4): lưu thông tin cuối, kiểm tra chẩn đoán (RB22) và tồn kho hiện tại của từng thuốc,
    /// rồi lập hóa đơn Chưa thanh toán (chưa có thu ngân, chưa có giờ thanh toán). Trigger tự tính tổng tiền (RB29).
    /// Sau bước này hồ sơ và đơn thuốc bị khóa.
    /// </summary>
    public Task<HoSoChiTiet> HoanTatAsync(string ma, string maBs, KhamBenhRequest req, CancellationToken ct)
        => _db.InTransactionAsync(async (conn, tx) =>
        {
            await KhoaHoSoDangKham(conn, tx, ma, maBs, ct);
            if (Rong(req.ChanDoanBenh) is null)
                throw AppException.BadRequest("Vui lòng nhập chẩn đoán trước khi hoàn tất khám (RB22).");
            await LuuThongTinKham(conn, tx, ma, req, ct);

            // Tồn kho có thể đã giảm từ lúc kê (thu ngân thanh toán hóa đơn khác): báo để bác sĩ sửa đơn,
            // tránh để hóa đơn không thanh toán được vì kho âm.
            var thieu = (await DonThuocAsync(conn, tx, ma, ct)).Where(t => t.SoLuong > t.SoLuongTon).ToList();
            if (thieu.Count > 0)
                throw AppException.BadRequest("Tồn kho không đủ, vui lòng sửa đơn: "
                    + string.Join("; ", thieu.Select(t => $"{t.TenThuoc} kê {t.SoLuong}, còn {t.SoLuongTon}")) + ".");

            var maHd = await MaSoGenerator.TaoMaAsync(conn, tx, LoaiMa.HoaDon, ct);
            await conn.Cmd(@"
INSERT INTO Hoa_don (ma_hoa_don, tong_tien, ngay_gio_thanh_toan, trang_thai_thanh_toan, ma_ho_so, ma_nv)
VALUES (@hd, 0, NULL, N'Chưa thanh toán', @ma, NULL)", tx)
                .P("@hd", SqlDbType.VarChar, 10, maHd).P("@ma", SqlDbType.VarChar, 10, ma).ExecuteNonQueryAsync(ct);
            return (await ChiTietAsync(conn, tx, ma, maBs, ct))!;
        }, ct: ct);

    private static async Task<List<T>> DocDanhSach<T>(SqlCommand cmd, Func<SqlDataReader, T> doc, CancellationToken ct)
    {
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var ds = new List<T>();
        while (await r.ReadAsync(ct)) ds.Add(doc(r));
        return ds;
    }
}
