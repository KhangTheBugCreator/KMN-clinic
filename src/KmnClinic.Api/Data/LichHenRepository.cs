using System.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Api.Data;

/// <summary>
/// Lịch hẹn phía khách hàng (M5). Giờ hiện tại do máy chủ web truyền vào (không dùng GETDATE của SQL Server).
/// </summary>
public class LichHenRepository
{
    private readonly Db _db;
    public LichHenRepository(Db db) => _db = db;

    private const string SqlLichHen = @"
SELECT h.ma_lich_hen, h.ngay_gio_dat, h.trieu_chung_ban_dau, h.trang_thai,
       l.ma_lich_lam_viec, l.ngay_lam_viec, c.ten_ca, c.gio_bat_dau, c.gio_ket_thuc,
       n.ma_nv, n.ho_ten_nv, n.duong_dan_anh, k.ten_khoa, p.ten_phong
FROM Lich_hen h
JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec
JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca
JOIN Nhan_vien n ON n.ma_nv = l.ma_nv
JOIN Phong_lam_viec p ON p.ma_phong = l.ma_phong
JOIN Khoa k ON k.ma_khoa = n.ma_khoa";

    /// <summary>Giờ bắt đầu ca của lịch hẹn, để so với giờ hiện tại.</summary>
    private static DateTime BatDau(DateOnly ngay, TimeOnly gio) => ngay.ToDateTime(gio);

    private static LichHenCuaToiItem Doc(SqlDataReader r, DateTime bayGio)
    {
        var tt = r.GetString(3);
        var ngay = DateOnly.FromDateTime(r.GetDateTime(5));
        var gioBd = TimeOnly.FromTimeSpan(r.GetTimeSpan(7));
        bool coTheHuy = (tt == TrangThaiLichHen.DaDat || tt == TrangThaiLichHen.DaXacNhan) && bayGio < BatDau(ngay, gioBd);
        return new(r.GetString(0), r.GetDateTime(1), r.GetStringOrNull(2), tt, r.GetString(4), ngay, r.GetString(6),
            gioBd, TimeOnly.FromTimeSpan(r.GetTimeSpan(8)), r.GetString(9), r.GetString(10), r.GetStringOrNull(11),
            r.GetString(12), r.GetString(13), coTheHuy);
    }

    public async Task<List<LichHenCuaToiItem>> DanhSachAsync(string maKh, DateTime bayGio, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlLichHen + @"
WHERE h.ma_kh = @kh
ORDER BY l.ngay_lam_viec DESC, c.gio_bat_dau DESC, h.ngay_gio_dat DESC").P("@kh", SqlDbType.VarChar, 10, maKh);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var ds = new List<LichHenCuaToiItem>();
        while (await r.ReadAsync(ct)) ds.Add(Doc(r, bayGio));
        return ds;
    }

    /// <summary>Chỉ trả lịch hẹn của đúng khách hàng này; của người khác coi như không tồn tại.</summary>
    public async Task<LichHenCuaToiItem?> TimAsync(string ma, string maKh, DateTime bayGio, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlLichHen + " WHERE h.ma_lich_hen = @ma AND h.ma_kh = @kh")
            .P("@ma", SqlDbType.VarChar, 10, ma).P("@kh", SqlDbType.VarChar, 10, maKh);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? Doc(r, bayGio) : null;
    }

    /// <summary>
    /// Đặt lịch: lịch làm việc phải tồn tại và chưa tới giờ bắt đầu ca (RB16); một khách không có hai lịch hẹn
    /// còn hiệu lực trên cùng một lịch làm việc (Q6). Trạng thái mới luôn là Đã đặt, chưa có lễ tân (RB14).
    /// </summary>
    public Task<string> DatLichAsync(string maKh, DatLichRequest req, DateTime bayGio, CancellationToken ct)
        => _db.InTransactionAsync(async (conn, tx) =>
        {
            DateOnly ngay; TimeOnly gioBd;
            await using (var c = conn.Cmd(@"
SELECT l.ngay_lam_viec, c.gio_bat_dau FROM Lich_lam_viec l WITH (UPDLOCK, ROWLOCK)
JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca WHERE l.ma_lich_lam_viec = @l", tx).P("@l", SqlDbType.VarChar, 10, req.MaLichLamViec))
            await using (var r = await c.ExecuteReaderAsync(ct))
            {
                if (!await r.ReadAsync(ct)) throw AppException.NotFound("Không tìm thấy lịch làm việc đã chọn. Vui lòng chọn lại.");
                ngay = DateOnly.FromDateTime(r.GetDateTime(0));
                gioBd = TimeOnly.FromTimeSpan(r.GetTimeSpan(1));
            }
            if (bayGio >= BatDau(ngay, gioBd))
                throw AppException.BadRequest("Ca khám này đã bắt đầu hoặc đã qua, không đặt được nữa (RB16). Vui lòng chọn ca khác.");

            // Dòng lịch làm việc đã bị khóa (UPDLOCK) ở trên nên các lần đặt cùng ca phải chờ nhau:
            // hai lần bấm cùng lúc không tạo hai lịch hẹn trùng, còn đặt ca khác không bị chặn.
            await using (var c = conn.Cmd(@"
SELECT TOP 1 ma_lich_hen FROM Lich_hen
WHERE ma_kh = @kh AND ma_lich_lam_viec = @l AND trang_thai IN (N'Đã đặt', N'Đã xác nhận')", tx)
                .P("@kh", SqlDbType.VarChar, 10, maKh).P("@l", SqlDbType.VarChar, 10, req.MaLichLamViec))
            {
                if (await c.ExecuteScalarAsync(ct) is string trung)
                    throw AppException.Conflict($"Bạn đã có lịch hẹn {trung} với bác sĩ ở ca này. Không cần đặt lại.");
            }

            var ma = await MaSoGenerator.TaoMaAsync(conn, tx, LoaiMa.LichHen, ct);
            var trieuChung = string.IsNullOrWhiteSpace(req.TrieuChungBanDau) ? null : req.TrieuChungBanDau.Trim();
            await using (var c = conn.Cmd(@"
INSERT INTO Lich_hen (ma_lich_hen, ngay_gio_dat, trieu_chung_ban_dau, trang_thai, ma_kh, ma_lich_lam_viec, ma_nv)
VALUES (@ma, @dat, @tc, N'Đã đặt', @kh, @l, NULL)", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma).P("@dat", SqlDbType.DateTime, 0, bayGio)
                .P("@tc", SqlDbType.NVarChar, 255, trieuChung)
                .P("@kh", SqlDbType.VarChar, 10, maKh).P("@l", SqlDbType.VarChar, 10, req.MaLichLamViec))
                await c.ExecuteNonQueryAsync(ct);
            return ma;
        }, IsolationLevel.ReadCommitted, ct);

    /// <summary>
    /// Khách tự hủy (Q7): chỉ lịch của mình, đang Đã đặt hoặc Đã xác nhận (RB15) và trước giờ bắt đầu ca.
    /// Trả false nếu không tìm thấy lịch hẹn của khách này.
    /// </summary>
    public Task<bool> HuyAsync(string ma, string maKh, DateTime bayGio, CancellationToken ct)
        => _db.InTransactionAsync(async (conn, tx) =>
        {
            string tt; DateOnly ngay; TimeOnly gioBd;
            await using (var c = conn.Cmd(@"
SELECT h.trang_thai, l.ngay_lam_viec, c.gio_bat_dau
FROM Lich_hen h WITH (UPDLOCK)
JOIN Lich_lam_viec l ON l.ma_lich_lam_viec = h.ma_lich_lam_viec
JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca
WHERE h.ma_lich_hen = @ma AND h.ma_kh = @kh", tx).P("@ma", SqlDbType.VarChar, 10, ma).P("@kh", SqlDbType.VarChar, 10, maKh))
            await using (var r = await c.ExecuteReaderAsync(ct))
            {
                if (!await r.ReadAsync(ct)) return false;
                tt = r.GetString(0);
                ngay = DateOnly.FromDateTime(r.GetDateTime(1));
                gioBd = TimeOnly.FromTimeSpan(r.GetTimeSpan(2));
            }
            if (tt == TrangThaiLichHen.DaHuy) throw AppException.Conflict("Lịch hẹn này đã được hủy trước đó.");
            if (tt == TrangThaiLichHen.DaKham) throw AppException.BadRequest("Lịch hẹn đã được tiếp nhận khám, không hủy được.");
            if (bayGio >= BatDau(ngay, gioBd))
                throw AppException.BadRequest("Ca khám đã bắt đầu, không tự hủy được nữa. Vui lòng liên hệ lễ tân.");

            await using (var c = conn.Cmd("UPDATE Lich_hen SET trang_thai = N'Đã hủy' WHERE ma_lich_hen = @ma", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma))
                await c.ExecuteNonQueryAsync(ct);
            return true;
        }, IsolationLevel.ReadCommitted, ct);
}
