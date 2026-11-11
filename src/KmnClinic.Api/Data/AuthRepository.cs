using System.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;

namespace KmnClinic.Api.Data;

/// <summary>Truy cập dữ liệu cho đăng nhập: chỉ đọc/ghi các cột có sẵn của Nhan_vien và Khach_hang.</summary>
public class AuthRepository
{
    private readonly Db _db;

    public AuthRepository(Db db) => _db = db;

    public record TaiKhoan(NguoiDungResponse NguoiDung, string? MatKhauDangLuu);

    public async Task<TaiKhoan?> TimNhanVienTheoSdtAsync(string sdt, CancellationToken ct)
    {
        const string sql = @"
SELECT nv.ma_nv, nv.ho_ten_nv, nv.sdt_nv, nv.chuc_vu, nv.ma_khoa, k.ten_khoa, nv.duong_dan_anh, nv.mat_khau_nv
FROM Nhan_vien nv LEFT JOIN Khoa k ON k.ma_khoa = nv.ma_khoa
WHERE nv.sdt_nv = @sdt";
        return await DocNhanVienAsync(sql, "@sdt", sdt, ct);
    }

    public async Task<TaiKhoan?> TimNhanVienTheoMaAsync(string ma, CancellationToken ct)
    {
        const string sql = @"
SELECT nv.ma_nv, nv.ho_ten_nv, nv.sdt_nv, nv.chuc_vu, nv.ma_khoa, k.ten_khoa, nv.duong_dan_anh, nv.mat_khau_nv
FROM Nhan_vien nv LEFT JOIN Khoa k ON k.ma_khoa = nv.ma_khoa
WHERE nv.ma_nv = @ma";
        return await DocNhanVienAsync(sql, "@ma", ma, ct);
    }

    public async Task<TaiKhoan?> TimKhachHangTheoSdtAsync(string sdt, CancellationToken ct)
    {
        const string sql = "SELECT ma_kh, ho_ten_kh, sdt_kh, mat_khau_kh FROM Khach_hang WHERE sdt_kh = @sdt";
        return await DocKhachHangAsync(sql, "@sdt", sdt, ct);
    }

    public async Task<TaiKhoan?> TimKhachHangTheoMaAsync(string ma, CancellationToken ct)
    {
        const string sql = "SELECT ma_kh, ho_ten_kh, sdt_kh, mat_khau_kh FROM Khach_hang WHERE ma_kh = @ma";
        return await DocKhachHangAsync(sql, "@ma", ma, ct);
    }

    public async Task CapNhatMatKhauAsync(string loai, string ma, string giaTriLuu, CancellationToken ct)
    {
        var sql = loai == "NV"
            ? "UPDATE Nhan_vien SET mat_khau_nv = @mk WHERE ma_nv = @ma"
            : "UPDATE Khach_hang SET mat_khau_kh = @mk WHERE ma_kh = @ma";
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(sql)
            .P("@mk", SqlDbType.VarChar, 255, giaTriLuu)
            .P("@ma", SqlDbType.VarChar, 10, ma);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<TaiKhoan?> DocNhanVienAsync(string sql, string param, string value, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(sql).P(param, SqlDbType.VarChar, 10, value);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        var chucVu = r.GetString(r.GetOrdinal("chuc_vu"));
        var nd = new NguoiDungResponse(
            Ma: r.GetString(r.GetOrdinal("ma_nv")),
            Loai: "NV",
            HoTen: r.GetString(r.GetOrdinal("ho_ten_nv")),
            VaiTro: VaiTro.TuChucVu(chucVu) ?? "",
            ChucVu: chucVu,
            MaKhoa: r.GetStringOrNull("ma_khoa"),
            TenKhoa: r.GetStringOrNull("ten_khoa"),
            SoDienThoai: r.GetString(r.GetOrdinal("sdt_nv")),
            DuongDanAnh: r.GetStringOrNull("duong_dan_anh"));
        return new TaiKhoan(nd, r.GetStringOrNull("mat_khau_nv"));
    }

    private async Task<TaiKhoan?> DocKhachHangAsync(string sql, string param, string value, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(sql).P(param, SqlDbType.VarChar, 10, value);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        var nd = new NguoiDungResponse(
            Ma: r.GetString(r.GetOrdinal("ma_kh")),
            Loai: "KH",
            HoTen: r.GetString(r.GetOrdinal("ho_ten_kh")),
            VaiTro: VaiTro.KhachHang,
            ChucVu: null, MaKhoa: null, TenKhoa: null,
            SoDienThoai: r.GetString(r.GetOrdinal("sdt_kh")),
            DuongDanAnh: null);
        return new TaiKhoan(nd, r.GetStringOrNull("mat_khau_kh"));
    }
}
