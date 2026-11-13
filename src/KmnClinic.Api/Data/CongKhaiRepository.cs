using System.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Api.Data;

/// <summary>
/// Truy vấn cho website công khai (M4), chỉ đọc. "Còn đặt được" = ngày sau hôm nay, hoặc hôm nay mà ca chưa bắt đầu (RB16).
/// Giờ hiện tại lấy từ máy chủ web (không dùng GETDATE của SQL Server vì có thể khác múi giờ).
/// </summary>
public class CongKhaiRepository
{
    private readonly Db _db;
    public CongKhaiRepository(Db db) => _db = db;

    private static string? Rong(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private const string DieuKienConDat = @"(l.ngay_lam_viec > @homNay OR (l.ngay_lam_viec = @homNay AND c.gio_bat_dau > @gioNay))";

    private const string SqlLich = @"
SELECT l.ma_lich_lam_viec, l.ngay_lam_viec, c.ma_ca, c.ten_ca, c.gio_bat_dau, c.gio_ket_thuc,
       n.ma_nv, n.ho_ten_nv, k.ma_khoa, k.ten_khoa, p.ten_phong
FROM Lich_lam_viec l
JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca
JOIN Nhan_vien n ON n.ma_nv = l.ma_nv
JOIN Phong_lam_viec p ON p.ma_phong = l.ma_phong
JOIN Khoa k ON k.ma_khoa = n.ma_khoa";

    private static LichKhamCongKhai DocLich(SqlDataReader r) => new(
        r.GetString(0), DateOnly.FromDateTime(r.GetDateTime(1)), r.GetString(2), r.GetString(3),
        TimeOnly.FromTimeSpan(r.GetTimeSpan(4)), TimeOnly.FromTimeSpan(r.GetTimeSpan(5)),
        r.GetString(6), r.GetString(7), r.GetString(8), r.GetString(9), r.GetString(10));

    private static SqlCommand GanGio(SqlCommand c, DateTime bayGio) => c
        .P("@homNay", SqlDbType.Date, 0, bayGio.Date)
        .P("@gioNay", SqlDbType.Time, 0, bayGio.TimeOfDay);

    public async Task<TongQuanCongKhai> TongQuanAsync(DateTime bayGio, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var c = GanGio(conn.Cmd($@"
SELECT (SELECT COUNT(*) FROM Khoa),
       (SELECT COUNT(*) FROM Nhan_vien WHERE chuc_vu = N'Bác sĩ'),
       (SELECT COUNT(DISTINCT l.ma_nv) FROM Lich_lam_viec l JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca
         WHERE {DieuKienConDat} AND l.ngay_lam_viec < DATEADD(day, 7, @homNay))"), bayGio);
        await using var r = await c.ExecuteReaderAsync(ct);
        await r.ReadAsync(ct);
        return new(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2));
    }

    public async Task<List<KhoaCongKhai>> DanhSachKhoaAsync(CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        var phong = new Dictionary<string, List<string>>();
        await using (var c = conn.Cmd("SELECT ma_khoa, ten_phong FROM Phong_lam_viec ORDER BY ten_phong"))
        await using (var r = await c.ExecuteReaderAsync(ct))
            while (await r.ReadAsync(ct))
            {
                if (!phong.TryGetValue(r.GetString(0), out var ds)) phong[r.GetString(0)] = ds = [];
                ds.Add(r.GetString(1));
            }
        await using var cmd = conn.Cmd(@"
SELECT k.ma_khoa, k.ten_khoa, k.mo_ta, (SELECT COUNT(*) FROM Nhan_vien n WHERE n.ma_khoa = k.ma_khoa AND n.chuc_vu = N'Bác sĩ')
FROM Khoa k ORDER BY k.ten_khoa");
        return await DocDanhSach(cmd, r => new KhoaCongKhai(r.GetString(0), r.GetString(1), r.GetStringOrNull(2), r.GetInt32(3),
            phong.GetValueOrDefault(r.GetString(0)) ?? []), ct);
    }

    public async Task<List<LichKhamCongKhai>> LichKhamAsync(DateOnly tu, DateOnly den, string? maKhoa, string? maNv, DateTime bayGio, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = GanGio(conn.Cmd(SqlLich + $@"
WHERE {DieuKienConDat} AND l.ngay_lam_viec BETWEEN @tu AND @den
  AND (@khoa IS NULL OR n.ma_khoa = @khoa) AND (@nv IS NULL OR l.ma_nv = @nv)
ORDER BY l.ngay_lam_viec, c.gio_bat_dau, k.ten_khoa, n.ho_ten_nv"), bayGio)
            .P("@tu", SqlDbType.Date, 0, tu.ToDateTime(TimeOnly.MinValue)).P("@den", SqlDbType.Date, 0, den.ToDateTime(TimeOnly.MinValue))
            .P("@khoa", SqlDbType.VarChar, 10, Rong(maKhoa)).P("@nv", SqlDbType.VarChar, 10, Rong(maNv));
        return await DocDanhSach(cmd, DocLich, ct);
    }

    /// <summary>
    /// Bác sĩ, sắp theo lịch còn đặt được gần nhất (người không có lịch xếp cuối). Mỗi bác sĩ kèm tối đa
    /// <paramref name="soLich"/> ca sắp tới trong <paramref name="soNgay"/> ngày (hoặc đúng ngày lọc).
    /// </summary>
    public async Task<TrangKetQua<BacSiCongKhai>> DanhSachBacSiAsync(BacSiCongKhaiQuery q, int pageSize, int soNgay, int soLich, DateTime bayGio, CancellationToken ct)
    {
        var homNay = DateOnly.FromDateTime(bayGio);
        DateOnly tu = q.Ngay ?? homNay, den = q.Ngay ?? homNay.AddDays(soNgay - 1);
        int trang = Math.Max(1, q.Page);
        await using var conn = await _db.OpenAsync(ct);
        string where = $@"
WHERE n.chuc_vu = N'Bác sĩ' AND (@q IS NULL OR n.ho_ten_nv LIKE @q) AND (@khoa IS NULL OR n.ma_khoa = @khoa)
  AND (@ngay IS NULL OR EXISTS (SELECT 1 FROM Lich_lam_viec l JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca
       WHERE l.ma_nv = n.ma_nv AND l.ngay_lam_viec = @ngay AND {DieuKienConDat}))";
        SqlCommand Gan(SqlCommand c) => GanGio(c, bayGio).P("@q", SqlDbType.NVarChar, 60, q.MauTim)
            .P("@khoa", SqlDbType.VarChar, 10, Rong(q.MaKhoa)).P("@ngay", SqlDbType.Date, 0, q.Ngay?.ToDateTime(TimeOnly.MinValue));
        var total = (int)(await Gan(conn.Cmd("SELECT COUNT(*) FROM Nhan_vien n" + where)).ExecuteScalarAsync(ct))!;

        var bacSi = new List<(string Ma, string Ten, string MaKhoa, string TenKhoa, string? MoTa, string? Anh)>();
        // x.gan_nhat: ngày còn đặt được gần nhất của từng bác sĩ, để sắp xếp
        await using (var cmd = Gan(conn.Cmd($@"
SELECT n.ma_nv, n.ho_ten_nv, k.ma_khoa, k.ten_khoa, k.mo_ta, n.duong_dan_anh
FROM Nhan_vien n JOIN Khoa k ON k.ma_khoa = n.ma_khoa
OUTER APPLY (SELECT MIN(l.ngay_lam_viec) AS gan_nhat FROM Lich_lam_viec l JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca
             WHERE l.ma_nv = n.ma_nv AND {DieuKienConDat}) x
{where}
ORDER BY CASE WHEN x.gan_nhat IS NULL THEN 1 ELSE 0 END, x.gan_nhat, n.ho_ten_nv
OFFSET @bo ROWS FETCH NEXT @lay ROWS ONLY").P("@bo", (trang - 1) * pageSize).P("@lay", pageSize)))
        {
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                bacSi.Add((r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetStringOrNull(4), r.GetStringOrNull(5)));
        }

        var lich = new Dictionary<string, List<LichKhamCongKhai>>();
        if (bacSi.Count > 0)
        {
            var ds = string.Join(",", bacSi.Select((_, i) => "@nv" + i));
            await using var c = GanGio(conn.Cmd(SqlLich + $@"
WHERE {DieuKienConDat} AND l.ngay_lam_viec BETWEEN @tu AND @den AND l.ma_nv IN ({ds})
ORDER BY l.ngay_lam_viec, c.gio_bat_dau"), bayGio)
                .P("@tu", SqlDbType.Date, 0, tu.ToDateTime(TimeOnly.MinValue)).P("@den", SqlDbType.Date, 0, den.ToDateTime(TimeOnly.MinValue));
            for (int i = 0; i < bacSi.Count; i++) c.P("@nv" + i, SqlDbType.VarChar, 10, bacSi[i].Ma);
            foreach (var l in await DocDanhSach(c, DocLich, ct))
            {
                if (!lich.TryGetValue(l.MaNv, out var dsl)) lich[l.MaNv] = dsl = [];
                if (dsl.Count < soLich) dsl.Add(l);
            }
        }
        var items = bacSi.Select(b => new BacSiCongKhai(b.Ma, b.Ten, b.MaKhoa, b.TenKhoa, b.MoTa, b.Anh, lich.GetValueOrDefault(b.Ma) ?? [])).ToList();
        return new(items, total, trang, pageSize);
    }

    public async Task<BacSiCongKhai?> TimBacSiAsync(string ma, int soNgay, DateTime bayGio, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        BacSiCongKhai? bs;
        await using (var c = conn.Cmd(@"
SELECT n.ma_nv, n.ho_ten_nv, k.ma_khoa, k.ten_khoa, k.mo_ta, n.duong_dan_anh
FROM Nhan_vien n JOIN Khoa k ON k.ma_khoa = n.ma_khoa WHERE n.ma_nv = @ma AND n.chuc_vu = N'Bác sĩ'").P("@ma", SqlDbType.VarChar, 10, ma))
        await using (var r = await c.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct)) return null;
            bs = new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetStringOrNull(4), r.GetStringOrNull(5), []);
        }
        var homNay = DateOnly.FromDateTime(bayGio);
        var lich = await LichKhamAsync(homNay, homNay.AddDays(soNgay - 1), null, ma, bayGio, ct);
        return bs with { LichSapToi = lich };
    }

    private static async Task<List<T>> DocDanhSach<T>(SqlCommand cmd, Func<SqlDataReader, T> doc, CancellationToken ct)
    {
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var ds = new List<T>();
        while (await r.ReadAsync(ct)) ds.Add(doc(r));
        return ds;
    }
}
