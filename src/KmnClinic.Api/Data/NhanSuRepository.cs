using System.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Api.Data;

/// <summary>Nhân viên và khách hàng (M2). Không bao giờ SELECT mat_khau_nv, mat_khau_kh.</summary>
public class NhanSuRepository
{
    private readonly Db _db;
    public NhanSuRepository(Db db) => _db = db;

    private static string? Rong(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /* =============================== NHÂN VIÊN =============================== */

    /// <summary>Số dòng dữ liệu nghiệp vụ đang trỏ tới nhân viên (lịch làm việc, lịch hẹn, hồ sơ, hóa đơn).</summary>
    private const string SqlSoLienKet = @"
  (SELECT COUNT(*) FROM Lich_lam_viec x WHERE x.ma_nv = n.ma_nv)
+ (SELECT COUNT(*) FROM Lich_hen x WHERE x.ma_nv = n.ma_nv)
+ (SELECT COUNT(*) FROM Ho_so_kham x WHERE x.ma_nv_kham = n.ma_nv OR x.ma_nv_tiep_nhan = n.ma_nv)
+ (SELECT COUNT(*) FROM Hoa_don x WHERE x.ma_nv = n.ma_nv)";

    private const string SqlNhanVien = @"
SELECT n.ma_nv, n.ho_ten_nv, n.ngay_sinh_nv, n.sdt_nv, n.chuc_vu, n.ma_khoa, k.ten_khoa, n.duong_dan_anh, " + SqlSoLienKet + @" AS so_lien_ket
FROM Nhan_vien n LEFT JOIN Khoa k ON k.ma_khoa = n.ma_khoa";

    private static NhanVienItem DocNhanVien(SqlDataReader r) => new(
        r.GetString(0), r.GetString(1), r.IsDBNull(2) ? null : DateOnly.FromDateTime(r.GetDateTime(2)),
        r.GetString(3), r.GetString(4), r.GetStringOrNull(5), r.GetStringOrNull(6), r.GetStringOrNull(7), r.GetInt32(8));

    public async Task<TrangKetQua<NhanVienItem>> DanhSachNhanVienAsync(NhanVienQuery q, CancellationToken ct)
    {
        const string where = @" WHERE (@q IS NULL OR n.ho_ten_nv LIKE @q OR n.sdt_nv LIKE @q OR n.ma_nv LIKE @q)
  AND (@cv IS NULL OR n.chuc_vu = @cv) AND (@khoa IS NULL OR n.ma_khoa = @khoa)";
        await using var conn = await _db.OpenAsync(ct);
        SqlCommand Gan(SqlCommand c) => c.P("@q", SqlDbType.NVarChar, 60, q.MauTim)
            .P("@cv", SqlDbType.NVarChar, 20, Rong(q.ChucVu)).P("@khoa", SqlDbType.VarChar, 10, Rong(q.MaKhoa));
        var total = (int)(await Gan(conn.Cmd("SELECT COUNT(*) FROM Nhan_vien n" + where)).ExecuteScalarAsync(ct))!;
        await using var cmd = Gan(conn.Cmd(SqlNhanVien + where + " ORDER BY n.ma_nv OFFSET @bo ROWS FETCH NEXT @lay ROWS ONLY"))
            .P("@bo", q.BoQua).P("@lay", q.CoTrangHopLe);
        return new(await DocDanhSach(cmd, DocNhanVien, ct), total, q.TrangHopLe, q.CoTrangHopLe);
    }

    public async Task<NhanVienItem?> TimNhanVienAsync(string ma, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlNhanVien + " WHERE n.ma_nv = @ma").P("@ma", SqlDbType.VarChar, 10, ma);
        return (await DocDanhSach(cmd, DocNhanVien, ct)).FirstOrDefault();
    }

    public Task<string> ThemNhanVienAsync(NhanVienRequest req, string? anh, string matKhauDaBam, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            var ma = await MaSoGenerator.TaoMaAsync(conn, tx, LoaiMa.NhanVien, ct);
            await GanNhanVien(conn.Cmd(@"
INSERT INTO Nhan_vien (ma_nv, ho_ten_nv, ngay_sinh_nv, sdt_nv, chuc_vu, mat_khau_nv, ma_khoa, duong_dan_anh)
VALUES (@ma, @ten, @ns, @sdt, @cv, @mk, @khoa, @anh)", tx), ma, req, anh)
                .P("@mk", SqlDbType.VarChar, 255, matKhauDaBam).ExecuteNonQueryAsync(ct);
            return ma;
        }, ct: ct);

    /// <summary>
    /// Sửa thông tin. Đổi chức vụ hoặc khoa bị chặn khi nhân viên đã có dữ liệu nghiệp vụ,
    /// vì trigger chỉ kiểm tra RB10, RB11, RB17, RB20, RB34 lúc thêm dòng mới (dữ liệu cũ sẽ sai mà không ai báo).
    /// </summary>
    public Task<bool> SuaNhanVienAsync(string ma, NhanVienRequest req, string? anh, string maNguoiSua, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            var cu = await KhoaVaDocNhanVien(conn, tx, ma, ct);
            if (cu is null) return false;
            var (chucVuCu, khoaCu, soLienKet) = cu.Value;
            if ((chucVuCu != req.ChucVu || khoaCu != req.MaKhoa) && soLienKet > 0)
                throw AppException.Conflict(
                    $"Không thể đổi chức vụ hoặc khoa: nhân viên đã có {soLienKet} dữ liệu lịch làm việc, lịch hẹn, hồ sơ hoặc hóa đơn theo chức vụ hiện tại.");
            if (chucVuCu == "Quản trị viên" && req.ChucVu != chucVuCu)
            {
                if (ma == maNguoiSua) throw AppException.Conflict("Bạn không thể tự đổi chức vụ của mình khi đang ở vai trò quản trị viên.");
                await KiemTraConQuanTri(conn, tx, ma, ct);
            }
            await GanNhanVien(conn.Cmd(@"
UPDATE Nhan_vien SET ho_ten_nv = @ten, ngay_sinh_nv = @ns, sdt_nv = @sdt, chuc_vu = @cv, ma_khoa = @khoa, duong_dan_anh = @anh
WHERE ma_nv = @ma", tx), ma, req, anh).ExecuteNonQueryAsync(ct);
            return true;
        }, ct: ct);

    public Task<bool> XoaNhanVienAsync(string ma, string maNguoiXoa, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            var cu = await KhoaVaDocNhanVien(conn, tx, ma, ct);
            if (cu is null) return false;
            if (ma == maNguoiXoa) throw AppException.Conflict("Bạn không thể tự xóa chính mình.");
            if (cu.Value.SoLienKet > 0)
                throw AppException.Conflict($"Không thể xóa: nhân viên đã có {cu.Value.SoLienKet} dữ liệu lịch làm việc, lịch hẹn, hồ sơ hoặc hóa đơn cần giữ lại.");
            if (cu.Value.ChucVu == "Quản trị viên") await KiemTraConQuanTri(conn, tx, ma, ct);
            return await conn.Cmd("DELETE FROM Nhan_vien WHERE ma_nv = @ma", tx).P("@ma", SqlDbType.VarChar, 10, ma).ExecuteNonQueryAsync(ct) > 0;
        }, ct: ct);

    private static SqlCommand GanNhanVien(SqlCommand c, string ma, NhanVienRequest req, string? anh) => c
        .P("@ma", SqlDbType.VarChar, 10, ma)
        .P("@ten", SqlDbType.NVarChar, 50, req.HoTen.Trim())
        .P("@ns", SqlDbType.Date, 0, req.NgaySinh?.ToDateTime(TimeOnly.MinValue))
        .P("@sdt", SqlDbType.VarChar, 10, req.SoDienThoai)
        .P("@cv", SqlDbType.NVarChar, 20, req.ChucVu)
        .P("@khoa", SqlDbType.VarChar, 10, req.MaKhoa)
        .P("@anh", SqlDbType.NVarChar, 1000, anh);

    /// <summary>Khóa dòng nhân viên và các bảng con (UPDLOCK, HOLDLOCK) để kết quả kiểm tra không đổi tới lúc ghi.</summary>
    private static async Task<(string ChucVu, string? MaKhoa, int SoLienKet)?> KhoaVaDocNhanVien(SqlConnection conn, SqlTransaction tx, string ma, CancellationToken ct)
    {
        await using var c = conn.Cmd(@"
SELECT n.chuc_vu, n.ma_khoa,
  (SELECT COUNT(*) FROM Lich_lam_viec x WITH (UPDLOCK, HOLDLOCK) WHERE x.ma_nv = n.ma_nv)
+ (SELECT COUNT(*) FROM Lich_hen x WITH (UPDLOCK, HOLDLOCK) WHERE x.ma_nv = n.ma_nv)
+ (SELECT COUNT(*) FROM Ho_so_kham x WITH (UPDLOCK, HOLDLOCK) WHERE x.ma_nv_kham = n.ma_nv OR x.ma_nv_tiep_nhan = n.ma_nv)
+ (SELECT COUNT(*) FROM Hoa_don x WITH (UPDLOCK, HOLDLOCK) WHERE x.ma_nv = n.ma_nv)
FROM Nhan_vien n WITH (UPDLOCK) WHERE n.ma_nv = @ma", tx).P("@ma", SqlDbType.VarChar, 10, ma);
        await using var r = await c.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return (r.GetString(0), r.GetStringOrNull(1), r.GetInt32(2));
    }

    /// <summary>Luôn còn ít nhất một quản trị viên khác ngoài người đang bị đổi chức vụ hoặc xóa.</summary>
    private static async Task KiemTraConQuanTri(SqlConnection conn, SqlTransaction tx, string ma, CancellationToken ct)
    {
        var con = (int)(await conn.Cmd(
            "SELECT COUNT(*) FROM Nhan_vien WITH (UPDLOCK, HOLDLOCK) WHERE chuc_vu = N'Quản trị viên' AND ma_nv <> @ma", tx)
            .P("@ma", SqlDbType.VarChar, 10, ma).ExecuteScalarAsync(ct))!;
        if (con == 0) throw AppException.Conflict("Phòng khám phải còn ít nhất một quản trị viên.");
    }

    public async Task<bool> KhoaTonTaiAsync(string maKhoa, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.Cmd("SELECT 1 FROM Khoa WHERE ma_khoa = @k").P("@k", SqlDbType.VarChar, 10, maKhoa).ExecuteScalarAsync(ct) is not null;
    }

    /* =============================== KHÁCH HÀNG =============================== */

    private const string SqlKhachHang = @"
SELECT k.ma_kh, k.ho_ten_kh, k.ngay_sinh_kh, k.gioi_tinh_kh, k.sdt_kh, k.dia_chi_kh,
       (SELECT COUNT(*) FROM Lich_hen h WHERE h.ma_kh = k.ma_kh) AS so_lich_hen,
       (SELECT COUNT(*) FROM Ho_so_kham s WHERE s.ma_kh = k.ma_kh) AS so_ho_so
FROM Khach_hang k";

    private static KhachHangItem DocKhachHang(SqlDataReader r) => new(
        r.GetString(0), r.GetString(1), DateOnly.FromDateTime(r.GetDateTime(2)), r.GetString(3), r.GetString(4),
        r.GetStringOrNull(5), r.GetInt32(6), r.GetInt32(7));

    public async Task<TrangKetQua<KhachHangItem>> DanhSachKhachHangAsync(KhachHangQuery q, CancellationToken ct)
    {
        const string where = @" WHERE (@q IS NULL OR k.ho_ten_kh LIKE @q OR k.sdt_kh LIKE @q OR k.ma_kh LIKE @q)
  AND (@gt IS NULL OR k.gioi_tinh_kh = @gt)";
        await using var conn = await _db.OpenAsync(ct);
        SqlCommand Gan(SqlCommand c) => c.P("@q", SqlDbType.NVarChar, 60, q.MauTim).P("@gt", SqlDbType.NVarChar, 5, Rong(q.GioiTinh));
        var total = (int)(await Gan(conn.Cmd("SELECT COUNT(*) FROM Khach_hang k" + where)).ExecuteScalarAsync(ct))!;
        await using var cmd = Gan(conn.Cmd(SqlKhachHang + where + " ORDER BY k.ma_kh DESC OFFSET @bo ROWS FETCH NEXT @lay ROWS ONLY"))
            .P("@bo", q.BoQua).P("@lay", q.CoTrangHopLe);
        return new(await DocDanhSach(cmd, DocKhachHang, ct), total, q.TrangHopLe, q.CoTrangHopLe);
    }

    public async Task<KhachHangItem?> TimKhachHangAsync(string ma, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlKhachHang + " WHERE k.ma_kh = @ma").P("@ma", SqlDbType.VarChar, 10, ma);
        return (await DocDanhSach(cmd, DocKhachHang, ct)).FirstOrDefault();
    }

    /// <summary>Khách mới không có tài khoản: mat_khau_kh để NULL (RB37).</summary>
    public Task<string> ThemKhachHangAsync(KhachHangRequest req, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            var ma = await MaSoGenerator.TaoMaAsync(conn, tx, LoaiMa.KhachHang, ct);
            await GanKhachHang(conn.Cmd(@"
INSERT INTO Khach_hang (ma_kh, ho_ten_kh, ngay_sinh_kh, gioi_tinh_kh, sdt_kh, dia_chi_kh, mat_khau_kh)
VALUES (@ma, @ten, @ns, @gt, @sdt, @dc, NULL)", tx), ma, req).ExecuteNonQueryAsync(ct);
            return ma;
        }, ct: ct);

    /// <summary>Không đụng tới mat_khau_kh.</summary>
    public async Task<bool> SuaKhachHangAsync(string ma, KhachHangRequest req, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await GanKhachHang(conn.Cmd(@"
UPDATE Khach_hang SET ho_ten_kh = @ten, ngay_sinh_kh = @ns, gioi_tinh_kh = @gt, sdt_kh = @sdt, dia_chi_kh = @dc
WHERE ma_kh = @ma"), ma, req).ExecuteNonQueryAsync(ct) > 0;
    }

    public Task<bool> XoaKhachHangAsync(string ma, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            int soHen, soHoSo;
            await using (var c = conn.Cmd(@"
SELECT (SELECT COUNT(*) FROM Lich_hen WITH (UPDLOCK, HOLDLOCK) WHERE ma_kh = @ma),
       (SELECT COUNT(*) FROM Ho_so_kham WITH (UPDLOCK, HOLDLOCK) WHERE ma_kh = @ma)", tx).P("@ma", SqlDbType.VarChar, 10, ma))
            await using (var r = await c.ExecuteReaderAsync(ct))
            {
                await r.ReadAsync(ct);
                soHen = r.GetInt32(0); soHoSo = r.GetInt32(1);
            }
            if (soHen > 0 || soHoSo > 0)
                throw AppException.Conflict($"Không thể xóa: khách hàng đã có {soHen} lịch hẹn và {soHoSo} hồ sơ khám cần giữ lại.");
            return await conn.Cmd("DELETE FROM Khach_hang WHERE ma_kh = @ma", tx).P("@ma", SqlDbType.VarChar, 10, ma).ExecuteNonQueryAsync(ct) > 0;
        }, ct: ct);

    private static SqlCommand GanKhachHang(SqlCommand c, string ma, KhachHangRequest req) => c
        .P("@ma", SqlDbType.VarChar, 10, ma)
        .P("@ten", SqlDbType.NVarChar, 50, req.HoTen.Trim())
        .P("@ns", SqlDbType.Date, 0, req.NgaySinh!.Value.ToDateTime(TimeOnly.MinValue))
        .P("@gt", SqlDbType.NVarChar, 5, req.GioiTinh)
        .P("@sdt", SqlDbType.VarChar, 10, req.SoDienThoai)
        .P("@dc", SqlDbType.NVarChar, 100, Rong(req.DiaChi));

    private static async Task<List<T>> DocDanhSach<T>(SqlCommand cmd, Func<SqlDataReader, T> doc, CancellationToken ct)
    {
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var ds = new List<T>();
        while (await r.ReadAsync(ct)) ds.Add(doc(r));
        return ds;
    }
}
