using System.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Api.Data;

/// <summary>
/// Nghiệp vụ lễ tân (M6). Giờ hiện tại do máy chủ web truyền vào. Trigger TG_LICH_HEN_KiemTra (RB14–RB17) và
/// TG_Ho_so_kham_KiemTra (RB19, RB20, RB34, RB41) vẫn chặn ở database; backend kiểm tra trước để báo lỗi rõ ràng.
/// </summary>
public class LeTanRepository
{
    private readonly Db _db;
    public LeTanRepository(Db db) => _db = db;

    private static string? Rong(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private const string SqlLichHen = @"
SELECT h.ma_lich_hen, h.ngay_gio_dat, h.trieu_chung_ban_dau, h.trang_thai,
       k.ma_kh, k.ho_ten_kh, k.sdt_kh, k.gioi_tinh_kh, k.ngay_sinh_kh,
       l.ma_lich_lam_viec, l.ngay_lam_viec, c.ten_ca, c.gio_bat_dau, c.gio_ket_thuc,
       n.ma_nv, n.ho_ten_nv, kh.ten_khoa, p.ten_phong,
       h.ma_nv, lt.ho_ten_nv, hs.ma_ho_so
FROM Lich_hen h
JOIN Khach_hang k ON k.ma_kh = h.ma_kh
JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec
JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca
JOIN Nhan_vien n ON n.ma_nv = l.ma_nv
JOIN Khoa kh ON kh.ma_khoa = n.ma_khoa
JOIN Phong_lam_viec p ON p.ma_phong = l.ma_phong
LEFT JOIN Nhan_vien lt ON lt.ma_nv = h.ma_nv
LEFT JOIN Ho_so_kham hs ON hs.ma_lich_hen = h.ma_lich_hen";

    private static LichHenLeTanItem DocLichHen(SqlDataReader r) => new(
        r.GetString(0), r.GetDateTime(1), r.GetStringOrNull(2), r.GetString(3),
        r.GetString(4), r.GetString(5), r.GetString(6), r.GetString(7), DateOnly.FromDateTime(r.GetDateTime(8)),
        r.GetString(9), DateOnly.FromDateTime(r.GetDateTime(10)), r.GetString(11),
        TimeOnly.FromTimeSpan(r.GetTimeSpan(12)), TimeOnly.FromTimeSpan(r.GetTimeSpan(13)),
        r.GetString(14), r.GetString(15), r.GetString(16), r.GetString(17),
        r.GetStringOrNull(18), r.GetStringOrNull(19), r.GetStringOrNull(20));

    private const string SqlHoSo = @"
SELECT hs.ma_ho_so, hs.ngay_gio_kham, hs.trieu_chung, hs.ma_lich_hen, k.ma_kh, k.ho_ten_kh, k.sdt_kh,
       bs.ma_nv, bs.ho_ten_nv, kh.ten_khoa, lt.ma_nv, lt.ho_ten_nv,
       CASE WHEN EXISTS (SELECT 1 FROM Hoa_don d WHERE d.ma_ho_so = hs.ma_ho_so) THEN 0 ELSE 1 END
FROM Ho_so_kham hs
JOIN Khach_hang k ON k.ma_kh = hs.ma_kh
JOIN Nhan_vien bs ON bs.ma_nv = hs.ma_nv_kham
JOIN Khoa kh ON kh.ma_khoa = bs.ma_khoa
JOIN Nhan_vien lt ON lt.ma_nv = hs.ma_nv_tiep_nhan";

    private static HoSoTiepNhanItem DocHoSo(SqlDataReader r) => new(
        r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetStringOrNull(3), r.GetString(4), r.GetString(5), r.GetString(6),
        r.GetString(7), r.GetString(8), r.GetString(9), r.GetString(10), r.GetString(11), r.GetInt32(12) == 1);

    /* ---------- Lịch hẹn ---------- */

    public async Task<TrangKetQua<LichHenLeTanItem>> DanhSachLichHenAsync(LichHenLeTanQuery q, CancellationToken ct)
    {
        const string where = @"
WHERE (@ngay IS NULL OR l.ngay_lam_viec = @ngay) AND (@tt IS NULL OR h.trang_thai = @tt) AND (@nv IS NULL OR l.ma_nv = @nv)
  AND (@q IS NULL OR h.ma_lich_hen LIKE @q OR k.ho_ten_kh LIKE @q OR k.sdt_kh LIKE @q OR k.ma_kh LIKE @q)";
        await using var conn = await _db.OpenAsync(ct);
        SqlCommand Gan(SqlCommand c) => c.P("@ngay", SqlDbType.Date, 0, q.Ngay?.ToDateTime(TimeOnly.MinValue))
            .P("@tt", SqlDbType.NVarChar, 20, Rong(q.TrangThai)).P("@nv", SqlDbType.VarChar, 10, Rong(q.MaNv))
            .P("@q", SqlDbType.NVarChar, 60, q.MauTim);
        var total = (int)(await Gan(conn.Cmd(@"SELECT COUNT(*) FROM Lich_hen h JOIN Khach_hang k ON k.ma_kh = h.ma_kh
JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec" + where)).ExecuteScalarAsync(ct))!;
        // Đã đặt (cần gọi xác nhận) lên đầu, rồi Đã xác nhận, theo giờ ca
        await using var cmd = Gan(conn.Cmd(SqlLichHen + where + @"
ORDER BY l.ngay_lam_viec, CASE h.trang_thai WHEN N'Đã đặt' THEN 0 WHEN N'Đã xác nhận' THEN 1 WHEN N'Đã khám' THEN 2 ELSE 3 END,
         c.gio_bat_dau, h.ngay_gio_dat
OFFSET @bo ROWS FETCH NEXT @lay ROWS ONLY")).P("@bo", q.BoQua).P("@lay", q.CoTrangHopLe);
        return new(await DocDanhSach(cmd, DocLichHen, ct), total, q.TrangHopLe, q.CoTrangHopLe);
    }

    public async Task<LichHenLeTanItem?> TimLichHenAsync(string ma, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlLichHen + " WHERE h.ma_lich_hen = @ma").P("@ma", SqlDbType.VarChar, 10, ma);
        return (await DocDanhSach(cmd, DocLichHen, ct)).FirstOrDefault();
    }

    /// <summary>Khóa và đọc lịch hẹn kèm ngày, giờ ca. Null nếu không có.</summary>
    private static async Task<(string TrangThai, DateOnly Ngay, TimeOnly BatDau, TimeOnly KetThuc, string MaKh, string MaBs, string? TrieuChung)?>
        KhoaLichHen(SqlConnection conn, SqlTransaction tx, string ma, CancellationToken ct)
    {
        await using var c = conn.Cmd(@"
SELECT h.trang_thai, l.ngay_lam_viec, c.gio_bat_dau, c.gio_ket_thuc, h.ma_kh, l.ma_nv, h.trieu_chung_ban_dau
FROM Lich_hen h WITH (UPDLOCK, ROWLOCK)
JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec
JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca
WHERE h.ma_lich_hen = @ma", tx).P("@ma", SqlDbType.VarChar, 10, ma);
        await using var r = await c.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return (r.GetString(0), DateOnly.FromDateTime(r.GetDateTime(1)), TimeOnly.FromTimeSpan(r.GetTimeSpan(2)),
            TimeOnly.FromTimeSpan(r.GetTimeSpan(3)), r.GetString(4), r.GetString(5), r.GetStringOrNull(6));
    }

    private static Task XacNhanTrongTx(SqlConnection conn, SqlTransaction tx, string ma, string maLeTan, CancellationToken ct)
        => conn.Cmd("UPDATE Lich_hen SET trang_thai = N'Đã xác nhận', ma_nv = @lt WHERE ma_lich_hen = @ma", tx)
            .P("@ma", SqlDbType.VarChar, 10, ma).P("@lt", SqlDbType.VarChar, 10, maLeTan).ExecuteNonQueryAsync(ct);

    /// <summary>Đã đặt → Đã xác nhận, ghi lễ tân xác nhận (RB15, RB17). Không xác nhận lịch mà ca đã kết thúc.</summary>
    public Task<bool> XacNhanAsync(string ma, string maLeTan, DateTime bayGio, CancellationToken ct)
        => _db.InTransactionAsync(async (conn, tx) =>
        {
            var h = await KhoaLichHen(conn, tx, ma, ct);
            if (h is null) return false;
            if (h.Value.TrangThai != TrangThaiLichHen.DaDat)
                throw AppException.Conflict($"Lịch hẹn đang ở trạng thái {h.Value.TrangThai}, chỉ xác nhận được lịch Đã đặt.");
            if (bayGio >= h.Value.Ngay.ToDateTime(h.Value.KetThuc))
                throw AppException.BadRequest("Ca khám của lịch hẹn này đã kết thúc, không xác nhận được nữa.");
            await XacNhanTrongTx(conn, tx, ma, maLeTan, ct);
            return true;
        }, ct: ct);

    /// <summary>Hủy lịch Đã đặt hoặc Đã xác nhận (RB15). Giữ nguyên lễ tân đã xác nhận (nếu có) để truy vết.</summary>
    public Task<bool> HuyAsync(string ma, CancellationToken ct)
        => _db.InTransactionAsync(async (conn, tx) =>
        {
            var h = await KhoaLichHen(conn, tx, ma, ct);
            if (h is null) return false;
            if (h.Value.TrangThai == TrangThaiLichHen.DaHuy) throw AppException.Conflict("Lịch hẹn này đã được hủy trước đó.");
            if (h.Value.TrangThai == TrangThaiLichHen.DaKham) throw AppException.BadRequest("Lịch hẹn đã được tiếp nhận khám, không hủy được.");
            await conn.Cmd("UPDATE Lich_hen SET trang_thai = N'Đã hủy' WHERE ma_lich_hen = @ma", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma).ExecuteNonQueryAsync(ct);
            return true;
        }, ct: ct);

    /// <summary>
    /// Tiếp nhận khách có hẹn: chỉ trong đúng ngày của lịch hẹn (Q10). Lịch còn Đã đặt thì xác nhận rồi tạo hồ sơ
    /// trong cùng một giao dịch (Q3). Trigger tự chuyển lịch hẹn sang Đã khám (RB19). Tiền công ghi 0, bác sĩ nhập khi khám.
    /// Trả mã hồ sơ, null nếu không có lịch hẹn.
    /// </summary>
    public Task<string?> TiepNhanCoHenAsync(string ma, string maLeTan, string trieuChung, DateTime bayGio, CancellationToken ct)
        => _db.InTransactionAsync<string?>(async (conn, tx) =>
        {
            var h = await KhoaLichHen(conn, tx, ma, ct);
            if (h is null) return null;
            var (tt, ngay, _, _, maKh, maBs, _) = h.Value;
            if (tt is not (TrangThaiLichHen.DaDat or TrangThaiLichHen.DaXacNhan))
                throw AppException.Conflict($"Lịch hẹn đang ở trạng thái {tt}, không tiếp nhận được.");
            if (ngay != DateOnly.FromDateTime(bayGio))
                throw AppException.BadRequest($"Chỉ tiếp nhận đúng ngày hẹn ({ngay:dd/MM/yyyy}). Khách đến khác ngày thì tiếp nhận như khách vãng lai.");
            if (tt == TrangThaiLichHen.DaDat) await XacNhanTrongTx(conn, tx, ma, maLeTan, ct);
            return await ThemHoSo(conn, tx, maKh, maBs, maLeTan, ma, trieuChung, bayGio, ct);
        }, ct: ct);

    /// <summary>
    /// Khách vãng lai: bác sĩ phải có ca hôm nay bao trùm giờ hiện tại, được tính sớm 30 phút trước ca (RB41).
    /// </summary>
    public Task<string> TiepNhanVangLaiAsync(TiepNhanVangLaiRequest req, string maLeTan, DateTime bayGio, CancellationToken ct)
        => _db.InTransactionAsync(async (conn, tx) =>
        {
            await using (var c = conn.Cmd("SELECT COUNT(*) FROM Khach_hang WHERE ma_kh = @kh", tx).P("@kh", SqlDbType.VarChar, 10, req.MaKh))
                if ((int)(await c.ExecuteScalarAsync(ct))! == 0) throw AppException.NotFound("Không tìm thấy khách hàng đã chọn.");
            var dangTruc = await DanhSachBacSiDangTrucAsync(conn, tx, bayGio, ct);
            if (!dangTruc.Any(b => b.MaNv == req.MaNvKham))
                throw AppException.BadRequest("Bác sĩ được chọn không trực ca này (RB41). Chỉ nhận khách vãng lai từ 30 phút trước giờ bắt đầu ca đến hết ca.");
            return await ThemHoSo(conn, tx, req.MaKh!, req.MaNvKham!, maLeTan, null, req.TrieuChung!.Trim(), bayGio, ct);
        }, ct: ct);

    private static async Task<string> ThemHoSo(SqlConnection conn, SqlTransaction tx, string maKh, string maBs, string maLeTan,
        string? maLichHen, string trieuChung, DateTime bayGio, CancellationToken ct)
    {
        var ma = await MaSoGenerator.TaoMaAsync(conn, tx, LoaiMa.HoSoKham, ct);
        await conn.Cmd(@"
INSERT INTO Ho_so_kham (ma_ho_so, ngay_gio_kham, trieu_chung, chan_doan_benh, loi_dan, tien_cong_kham, ma_lich_hen, ma_kh, ma_nv_kham, ma_nv_tiep_nhan)
VALUES (@ma, @gio, @tc, NULL, NULL, 0, @lh, @kh, @bs, @lt)", tx)
            .P("@ma", SqlDbType.VarChar, 10, ma).P("@gio", SqlDbType.DateTime, 0, bayGio).P("@tc", SqlDbType.NVarChar, 255, trieuChung)
            .P("@lh", SqlDbType.VarChar, 10, maLichHen).P("@kh", SqlDbType.VarChar, 10, maKh)
            .P("@bs", SqlDbType.VarChar, 10, maBs).P("@lt", SqlDbType.VarChar, 10, maLeTan)
            .ExecuteNonQueryAsync(ct);
        return ma;
    }

    /* ---------- Bác sĩ đang trực, hồ sơ, tổng quan ---------- */

    private const string DieuKienDangTruc = @"l.ngay_lam_viec = @homNay
  AND @gioNay BETWEEN DATEADD(MINUTE, -30, c.gio_bat_dau) AND c.gio_ket_thuc";

    private static async Task<List<BacSiDangTrucItem>> DanhSachBacSiDangTrucAsync(SqlConnection conn, SqlTransaction? tx, DateTime bayGio, CancellationToken ct)
    {
        await using var cmd = conn.Cmd($@"
SELECT n.ma_nv, n.ho_ten_nv, n.duong_dan_anh, k.ten_khoa, p.ten_phong, c.ten_ca, c.gio_bat_dau, c.gio_ket_thuc,
       (SELECT COUNT(*) FROM Ho_so_kham hs WHERE hs.ma_nv_kham = n.ma_nv AND CAST(hs.ngay_gio_kham AS date) = @homNay
          AND NOT EXISTS (SELECT 1 FROM Hoa_don d WHERE d.ma_ho_so = hs.ma_ho_so))
FROM Lich_lam_viec l
JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca
JOIN Nhan_vien n ON n.ma_nv = l.ma_nv
JOIN Khoa k ON k.ma_khoa = n.ma_khoa
JOIN Phong_lam_viec p ON p.ma_phong = l.ma_phong
WHERE {DieuKienDangTruc}
ORDER BY k.ten_khoa, n.ho_ten_nv", tx)
            .P("@homNay", SqlDbType.Date, 0, bayGio.Date).P("@gioNay", SqlDbType.Time, 0, bayGio.TimeOfDay);
        return await DocDanhSach(cmd, r => new BacSiDangTrucItem(r.GetString(0), r.GetString(1), r.GetStringOrNull(2), r.GetString(3),
            r.GetString(4), r.GetString(5), TimeOnly.FromTimeSpan(r.GetTimeSpan(6)), TimeOnly.FromTimeSpan(r.GetTimeSpan(7)), r.GetInt32(8)), ct);
    }

    public async Task<List<BacSiDangTrucItem>> BacSiDangTrucAsync(DateTime bayGio, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await DanhSachBacSiDangTrucAsync(conn, null, bayGio, ct);
    }

    public async Task<List<HoSoTiepNhanItem>> HoSoTheoNgayAsync(DateOnly ngay, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlHoSo + @"
WHERE hs.ngay_gio_kham >= @tu AND hs.ngay_gio_kham < DATEADD(day, 1, @tu)
ORDER BY hs.ngay_gio_kham DESC").P("@tu", SqlDbType.DateTime, 0, ngay.ToDateTime(TimeOnly.MinValue));
        return await DocDanhSach(cmd, DocHoSo, ct);
    }

    public async Task<HoSoTiepNhanItem?> TimHoSoAsync(string ma, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlHoSo + " WHERE hs.ma_ho_so = @ma").P("@ma", SqlDbType.VarChar, 10, ma);
        return (await DocDanhSach(cmd, DocHoSo, ct)).FirstOrDefault();
    }

    public async Task<TongQuanLeTan> TongQuanAsync(DateTime bayGio, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd($@"
SELECT
  (SELECT COUNT(*) FROM Lich_hen h JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec
     JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca
     WHERE h.trang_thai = N'Đã đặt' AND (l.ngay_lam_viec > @homNay OR (l.ngay_lam_viec = @homNay AND c.gio_ket_thuc > @gioNay))),
  (SELECT COUNT(*) FROM Lich_hen h JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec
     WHERE l.ngay_lam_viec = @homNay AND h.trang_thai <> N'Đã hủy'),
  (SELECT COUNT(*) FROM Lich_hen h JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec
     WHERE l.ngay_lam_viec = @homNay AND h.trang_thai = N'Đã xác nhận'),
  (SELECT COUNT(*) FROM Ho_so_kham WHERE CAST(ngay_gio_kham AS date) = @homNay),
  (SELECT COUNT(*) FROM Ho_so_kham WHERE CAST(ngay_gio_kham AS date) = @homNay AND ma_lich_hen IS NULL),
  (SELECT COUNT(DISTINCT l.ma_nv) FROM Lich_lam_viec l JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca WHERE {DieuKienDangTruc})")
            .P("@homNay", SqlDbType.Date, 0, bayGio.Date).P("@gioNay", SqlDbType.Time, 0, bayGio.TimeOfDay);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        await r.ReadAsync(ct);
        return new(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3), r.GetInt32(4), r.GetInt32(5));
    }

    private static async Task<List<T>> DocDanhSach<T>(SqlCommand cmd, Func<SqlDataReader, T> doc, CancellationToken ct)
    {
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var ds = new List<T>();
        while (await r.ReadAsync(ct)) ds.Add(doc(r));
        return ds;
    }
}
