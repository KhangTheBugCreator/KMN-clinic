using System.Data;
using System.Globalization;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Api.Data;

/// <summary>
/// Lịch làm việc của bác sĩ (M3). Trigger TG_LICH_LAM_VIEC_KiemTra (RB9, RB10, RB11) và hai UNIQUE (RB12, RB13)
/// là lớp chặn cuối; backend kiểm tra trước để báo lỗi rõ ràng (ai đang dùng phòng, bác sĩ đang ở phòng nào).
/// </summary>
public class LichLamViecRepository
{
    private readonly Db _db;
    public LichLamViecRepository(Db db) => _db = db;

    private static DateOnly HomNay => DateOnly.FromDateTime(DateTime.Today);
    private static string Ngay(DateOnly d) => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    private static string? Rong(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private const string SqlLich = @"
SELECT l.ma_lich_lam_viec, l.ngay_lam_viec, c.ma_ca, c.ten_ca, c.gio_bat_dau, c.gio_ket_thuc,
       n.ma_nv, n.ho_ten_nv, n.duong_dan_anh, k.ma_khoa, k.ten_khoa, p.ma_phong, p.ten_phong,
       (SELECT COUNT(*) FROM Lich_hen h WHERE h.ma_lich_lam_viec = l.ma_lich_lam_viec) AS so_hen,
       (SELECT COUNT(*) FROM Lich_hen h WHERE h.ma_lich_lam_viec = l.ma_lich_lam_viec
            AND h.trang_thai IN (N'Đã đặt', N'Đã xác nhận')) AS so_hen_con
FROM Lich_lam_viec l
JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca
JOIN Nhan_vien n ON n.ma_nv = l.ma_nv
JOIN Phong_lam_viec p ON p.ma_phong = l.ma_phong
JOIN Khoa k ON k.ma_khoa = p.ma_khoa";

    private static LichLamViecItem DocLich(SqlDataReader r)
    {
        var ngay = DateOnly.FromDateTime(r.GetDateTime(1));
        return new(r.GetString(0), ngay, r.GetString(2), r.GetString(3),
            TimeOnly.FromTimeSpan(r.GetTimeSpan(4)), TimeOnly.FromTimeSpan(r.GetTimeSpan(5)),
            r.GetString(6), r.GetString(7), r.GetStringOrNull(8), r.GetString(9), r.GetString(10),
            r.GetString(11), r.GetString(12), r.GetInt32(13), r.GetInt32(14), ngay < HomNay);
    }

    public async Task<List<LichLamViecItem>> DanhSachAsync(LichLamViecQuery q, DateOnly tu, DateOnly den, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlLich + @"
WHERE l.ngay_lam_viec BETWEEN @tu AND @den
  AND (@nv IS NULL OR l.ma_nv = @nv) AND (@khoa IS NULL OR p.ma_khoa = @khoa)
  AND (@phong IS NULL OR l.ma_phong = @phong) AND (@ca IS NULL OR l.ma_ca = @ca)
ORDER BY l.ngay_lam_viec, c.gio_bat_dau, p.ten_phong")
            .P("@tu", SqlDbType.Date, 0, tu.ToDateTime(TimeOnly.MinValue))
            .P("@den", SqlDbType.Date, 0, den.ToDateTime(TimeOnly.MinValue))
            .P("@nv", SqlDbType.VarChar, 10, Rong(q.MaNv)).P("@khoa", SqlDbType.VarChar, 10, Rong(q.MaKhoa))
            .P("@phong", SqlDbType.VarChar, 10, Rong(q.MaPhong)).P("@ca", SqlDbType.VarChar, 10, Rong(q.MaCa));
        return await DocDanhSach(cmd, DocLich, ct);
    }

    public async Task<LichLamViecItem?> TimAsync(string ma, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlLich + " WHERE l.ma_lich_lam_viec = @ma").P("@ma", SqlDbType.VarChar, 10, ma);
        return (await DocDanhSach(cmd, DocLich, ct)).FirstOrDefault();
    }

    public async Task<List<BacSiItem>> DanhSachBacSiAsync(string? maKhoa, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(@"
SELECT n.ma_nv, n.ho_ten_nv, k.ma_khoa, k.ten_khoa, n.duong_dan_anh
FROM Nhan_vien n JOIN Khoa k ON k.ma_khoa = n.ma_khoa
WHERE n.chuc_vu = N'Bác sĩ' AND (@khoa IS NULL OR n.ma_khoa = @khoa)
ORDER BY k.ten_khoa, n.ho_ten_nv").P("@khoa", SqlDbType.VarChar, 10, Rong(maKhoa));
        return await DocDanhSach(cmd, r => new BacSiItem(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetStringOrNull(4)), ct);
    }

    /* ------------------------------ Ghi ------------------------------ */

    public Task<string> ThemAsync(LichLamViecRequest req, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            var ngay = req.NgayLamViec!.Value;
            await KiemTraNguonLuc(conn, tx, req.MaNv, req.MaPhong, req.MaCa, ct);
            await KiemTraTrung(conn, tx, null, [ngay], req.MaNv, req.MaPhong, req.MaCa, ct);
            return await Chen(conn, tx, ngay, req, ct);
        }, ct: ct);

    public Task<List<string>> ThemHangLoatAsync(List<DateOnly> cacNgay, LichLamViecRequest mau, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            await KiemTraNguonLuc(conn, tx, mau.MaNv, mau.MaPhong, mau.MaCa, ct);
            await KiemTraTrung(conn, tx, null, cacNgay, mau.MaNv, mau.MaPhong, mau.MaCa, ct);
            var ma = new List<string>();
            foreach (var ngay in cacNgay) ma.Add(await Chen(conn, tx, ngay, mau, ct));
            return ma;
        }, ct: ct);

    /// <summary>
    /// Chỉ sửa được lịch chưa qua và chưa phát sinh lịch hẹn hay hồ sơ vãng lai: trigger không kiểm tra lại
    /// RB16, RB19, RB41 cho dữ liệu cũ nên đổi bác sĩ, phòng, ngày, ca sẽ làm sai lịch sử.
    /// </summary>
    public Task<bool> SuaAsync(string ma, LichLamViecRequest req, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            if (!await KhoaVaKiemTraDuocDoi(conn, tx, ma, "sửa", ct)) return false;
            await KiemTraNguonLuc(conn, tx, req.MaNv, req.MaPhong, req.MaCa, ct);
            await KiemTraTrung(conn, tx, ma, [req.NgayLamViec!.Value], req.MaNv, req.MaPhong, req.MaCa, ct);
            await conn.Cmd(@"
UPDATE Lich_lam_viec SET ngay_lam_viec = @ngay, ma_nv = @nv, ma_phong = @phong, ma_ca = @ca
WHERE ma_lich_lam_viec = @ma", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma)
                .P("@ngay", SqlDbType.Date, 0, req.NgayLamViec!.Value.ToDateTime(TimeOnly.MinValue))
                .P("@nv", SqlDbType.VarChar, 10, req.MaNv).P("@phong", SqlDbType.VarChar, 10, req.MaPhong)
                .P("@ca", SqlDbType.VarChar, 10, req.MaCa).ExecuteNonQueryAsync(ct);
            return true;
        }, ct: ct);

    public Task<bool> XoaAsync(string ma, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            if (!await KhoaVaKiemTraDuocDoi(conn, tx, ma, "xóa", ct)) return false;
            await conn.Cmd("DELETE FROM Lich_lam_viec WHERE ma_lich_lam_viec = @ma", tx).P("@ma", SqlDbType.VarChar, 10, ma).ExecuteNonQueryAsync(ct);
            return true;
        }, ct: ct);

    private static async Task<string> Chen(SqlConnection conn, SqlTransaction tx, DateOnly ngay, LichLamViecRequest req, CancellationToken ct)
    {
        var ma = await MaSoGenerator.TaoMaAsync(conn, tx, LoaiMa.LichLamViec, ct);
        await conn.Cmd(@"
INSERT INTO Lich_lam_viec (ma_lich_lam_viec, ngay_lam_viec, ma_nv, ma_phong, ma_ca) VALUES (@ma, @ngay, @nv, @phong, @ca)", tx)
            .P("@ma", SqlDbType.VarChar, 10, ma).P("@ngay", SqlDbType.Date, 0, ngay.ToDateTime(TimeOnly.MinValue))
            .P("@nv", SqlDbType.VarChar, 10, req.MaNv).P("@phong", SqlDbType.VarChar, 10, req.MaPhong)
            .P("@ca", SqlDbType.VarChar, 10, req.MaCa).ExecuteNonQueryAsync(ct);
        return ma;
    }

    /// <summary>RB10 (phải là bác sĩ), RB11 (phòng thuộc khoa của bác sĩ), ca tồn tại.</summary>
    private static async Task KiemTraNguonLuc(SqlConnection conn, SqlTransaction tx, string maNv, string maPhong, string maCa, CancellationToken ct)
    {
        await using var c = conn.Cmd(@"
SELECT (SELECT chuc_vu FROM Nhan_vien WHERE ma_nv = @nv),
       (SELECT ma_khoa FROM Nhan_vien WHERE ma_nv = @nv),
       (SELECT ma_khoa FROM Phong_lam_viec WHERE ma_phong = @phong),
       (SELECT COUNT(*) FROM Ca_lam_viec WHERE ma_ca = @ca)", tx)
            .P("@nv", SqlDbType.VarChar, 10, maNv).P("@phong", SqlDbType.VarChar, 10, maPhong).P("@ca", SqlDbType.VarChar, 10, maCa);
        await using var r = await c.ExecuteReaderAsync(ct);
        await r.ReadAsync(ct);
        var chucVu = r.GetStringOrNull(0);
        var khoaBs = r.GetStringOrNull(1);
        var khoaPhong = r.GetStringOrNull(2);
        if (chucVu is null) throw AppException.BadRequest("Không tìm thấy bác sĩ đã chọn.");
        if (chucVu != "Bác sĩ") throw AppException.BadRequest("Nhân viên được xếp lịch phải là bác sĩ (RB10).");
        if (khoaPhong is null) throw AppException.BadRequest("Không tìm thấy phòng đã chọn.");
        if (r.GetInt32(3) == 0) throw AppException.BadRequest("Không tìm thấy ca đã chọn.");
        if (khoaPhong != khoaBs) throw AppException.BadRequest("Phòng phải thuộc khoa của bác sĩ (RB11).");
    }

    /// <summary>RB12 (phòng/ca/ngày) và RB13 (bác sĩ/ca/ngày). Khóa phạm vi để hai người xếp cùng lúc không lọt.</summary>
    private static async Task KiemTraTrung(SqlConnection conn, SqlTransaction tx, string? boQuaMa, List<DateOnly> cacNgay,
        string maNv, string maPhong, string maCa, CancellationToken ct)
    {
        var tu = cacNgay.Min(); var den = cacNgay.Max();
        await using var c = conn.Cmd(@"
SELECT l.ngay_lam_viec, l.ma_nv, n.ho_ten_nv, l.ma_phong, p.ten_phong
FROM Lich_lam_viec l WITH (UPDLOCK, HOLDLOCK)
JOIN Nhan_vien n ON n.ma_nv = l.ma_nv JOIN Phong_lam_viec p ON p.ma_phong = l.ma_phong
WHERE l.ma_ca = @ca AND l.ngay_lam_viec BETWEEN @tu AND @den AND (l.ma_nv = @nv OR l.ma_phong = @phong)
  AND (@bo IS NULL OR l.ma_lich_lam_viec <> @bo)", tx)
            .P("@ca", SqlDbType.VarChar, 10, maCa).P("@nv", SqlDbType.VarChar, 10, maNv).P("@phong", SqlDbType.VarChar, 10, maPhong)
            .P("@tu", SqlDbType.Date, 0, tu.ToDateTime(TimeOnly.MinValue)).P("@den", SqlDbType.Date, 0, den.ToDateTime(TimeOnly.MinValue))
            .P("@bo", SqlDbType.VarChar, 10, boQuaMa);
        var chon = cacNgay.ToHashSet();
        var loi = new List<string>();
        await using (var r = await c.ExecuteReaderAsync(ct))
            while (await r.ReadAsync(ct))
            {
                var ngay = DateOnly.FromDateTime(r.GetDateTime(0));
                if (!chon.Contains(ngay)) continue;
                loi.Add(r.GetString(1) == maNv
                    ? $"ngày {Ngay(ngay)} bác sĩ đã có lịch ở {r.GetString(4)} (RB13)"
                    : $"ngày {Ngay(ngay)} {r.GetString(4)} đã được xếp cho {r.GetString(2)} (RB12)");
            }
        if (loi.Count == 0) return;
        var hien = loi.Take(5).ToList();
        if (loi.Count > 5) hien.Add($"và {loi.Count - 5} ngày khác");
        throw AppException.Conflict("Trùng lịch trong ca này: " + string.Join("; ", hien) + ".");
    }

    /// <summary>Khóa lịch; ném 409 nếu lịch đã qua hoặc đã có lịch hẹn, hồ sơ vãng lai. Trả false nếu không tồn tại.</summary>
    private static async Task<bool> KhoaVaKiemTraDuocDoi(SqlConnection conn, SqlTransaction tx, string ma, string thaoTac, CancellationToken ct)
    {
        await using var c = conn.Cmd(@"
SELECT l.ngay_lam_viec,
       (SELECT COUNT(*) FROM Lich_hen h WITH (UPDLOCK, HOLDLOCK) WHERE h.ma_lich_lam_viec = l.ma_lich_lam_viec),
       (SELECT COUNT(*) FROM Ho_so_kham s WITH (UPDLOCK, HOLDLOCK)
         WHERE s.ma_lich_hen IS NULL AND s.ma_nv_kham = l.ma_nv AND CAST(s.ngay_gio_kham AS date) = l.ngay_lam_viec)
FROM Lich_lam_viec l WITH (UPDLOCK) WHERE l.ma_lich_lam_viec = @ma", tx).P("@ma", SqlDbType.VarChar, 10, ma);
        await using var r = await c.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return false;
        var ngay = DateOnly.FromDateTime(r.GetDateTime(0));
        int soHen = r.GetInt32(1), soVangLai = r.GetInt32(2);
        if (ngay < HomNay) throw AppException.Conflict($"Lịch ngày {Ngay(ngay)} đã qua, không {thaoTac} được để giữ lịch sử.");
        if (soHen > 0) throw AppException.Conflict($"Không thể {thaoTac}: lịch này đã có {soHen} lịch hẹn của khách.");
        if (soVangLai > 0) throw AppException.Conflict($"Không thể {thaoTac}: bác sĩ đã tiếp nhận {soVangLai} khách vãng lai theo lịch này.");
        return true;
    }

    private static async Task<List<T>> DocDanhSach<T>(SqlCommand cmd, Func<SqlDataReader, T> doc, CancellationToken ct)
    {
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var ds = new List<T>();
        while (await r.ReadAsync(ct)) ds.Add(doc(r));
        return ds;
    }
}
