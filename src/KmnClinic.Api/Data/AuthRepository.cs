using System.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;

namespace KmnClinic.Api.Data;

/// <summary>
/// Đọc người dùng cho chức năng giả lập vai trò. Website không đọc hay ghi cột mật khẩu.
/// </summary>
public class AuthRepository
{
    private readonly Db _db;

    public AuthRepository(Db db) => _db = db;

    public async Task<NguoiDungResponse?> TimNhanVienAsync(string ma, CancellationToken ct)
    {
        const string sql = @"
SELECT nv.ma_nv, nv.ho_ten_nv, nv.sdt_nv, nv.chuc_vu, nv.ma_khoa, k.ten_khoa, nv.duong_dan_anh
FROM Nhan_vien nv LEFT JOIN Khoa k ON k.ma_khoa = nv.ma_khoa
WHERE nv.ma_nv = @ma";
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(sql).P("@ma", SqlDbType.VarChar, 10, ma);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        var chucVu = r.GetString(r.GetOrdinal("chuc_vu"));
        return new NguoiDungResponse(
            Ma: r.GetString(r.GetOrdinal("ma_nv")),
            Loai: "NV",
            HoTen: r.GetString(r.GetOrdinal("ho_ten_nv")),
            VaiTro: VaiTro.TuChucVu(chucVu) ?? "",
            ChucVu: chucVu,
            MaKhoa: r.GetStringOrNull("ma_khoa"),
            TenKhoa: r.GetStringOrNull("ten_khoa"),
            SoDienThoai: r.GetString(r.GetOrdinal("sdt_nv")),
            DuongDanAnh: r.GetStringOrNull("duong_dan_anh"));
    }

    public async Task<NguoiDungResponse?> TimKhachHangAsync(string ma, CancellationToken ct)
    {
        const string sql = "SELECT ma_kh, ho_ten_kh, sdt_kh FROM Khach_hang WHERE ma_kh = @ma";
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(sql).P("@ma", SqlDbType.VarChar, 10, ma);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return new NguoiDungResponse(
            Ma: r.GetString(0), Loai: "KH", HoTen: r.GetString(1), VaiTro: VaiTro.KhachHang,
            ChucVu: null, MaKhoa: null, TenKhoa: null, SoDienThoai: r.GetString(2), DuongDanAnh: null);
    }

    /// <summary>Nhân viên theo chức vụ (giá trị đúng như CK_Nhan_vien_chuc_vu).</summary>
    public async Task<List<NhanVienGiaLapItem>> DanhSachNhanVienAsync(string chucVu, CancellationToken ct)
    {
        const string sql = @"
SELECT nv.ma_nv, nv.ho_ten_nv, nv.chuc_vu, k.ten_khoa, nv.duong_dan_anh
FROM Nhan_vien nv LEFT JOIN Khoa k ON k.ma_khoa = nv.ma_khoa
WHERE nv.chuc_vu = @cv
ORDER BY nv.ho_ten_nv";
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(sql).P("@cv", SqlDbType.NVarChar, 20, chucVu);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var ds = new List<NhanVienGiaLapItem>();
        while (await r.ReadAsync(ct))
            ds.Add(new NhanVienGiaLapItem(r.GetString(0), r.GetString(1), r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4)));
        return ds;
    }

    /// <summary>Tìm khách theo đầu số điện thoại hoặc một phần họ tên (tối đa 20 kết quả).</summary>
    public async Task<List<KhachHangGiaLapItem>> TraCuuKhachHangAsync(string? tuKhoa, CancellationToken ct)
    {
        const string sql = @"
SELECT TOP (20) ma_kh, ho_ten_kh, sdt_kh, YEAR(ngay_sinh_kh), gioi_tinh_kh
FROM Khach_hang
WHERE @q IS NULL OR sdt_kh LIKE @sdt OR ho_ten_kh LIKE @ten
ORDER BY ho_ten_kh";
        var q = string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim();
        var mau = q is null ? null : q.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(sql)
            .P("@q", SqlDbType.NVarChar, 50, q)
            .P("@sdt", SqlDbType.VarChar, 12, mau is null ? null : mau + "%")
            .P("@ten", SqlDbType.NVarChar, 60, mau is null ? null : "%" + mau + "%");
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var ds = new List<KhachHangGiaLapItem>();
        while (await r.ReadAsync(ct))
            ds.Add(new KhachHangGiaLapItem(r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3), r.GetString(4)));
        return ds;
    }

    /// <summary>Tạo khách hàng mới, mã tự sinh, không có mật khẩu.</summary>
    public Task<string> TaoKhachHangAsync(TaoKhachHangRequest req, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            var ma = await MaSoGenerator.TaoMaAsync(conn, tx, LoaiMa.KhachHang, ct);
            await using var cmd = conn.Cmd(@"
INSERT INTO Khach_hang (ma_kh, ho_ten_kh, ngay_sinh_kh, gioi_tinh_kh, sdt_kh, dia_chi_kh, mat_khau_kh)
VALUES (@ma, @ten, @ns, @gt, @sdt, @dc, NULL)", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma)
                .P("@ten", SqlDbType.NVarChar, 50, req.HoTen.Trim())
                .P("@ns", SqlDbType.Date, 0, req.NgaySinh!.Value.ToDateTime(TimeOnly.MinValue))
                .P("@gt", SqlDbType.NVarChar, 5, req.GioiTinh)
                .P("@sdt", SqlDbType.VarChar, 10, req.SoDienThoai)
                .P("@dc", SqlDbType.NVarChar, 100, string.IsNullOrWhiteSpace(req.DiaChi) ? null : req.DiaChi.Trim());
            await cmd.ExecuteNonQueryAsync(ct);
            return ma;
        }, ct: ct);
}
