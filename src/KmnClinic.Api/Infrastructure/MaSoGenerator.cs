using System.Data;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Api.Infrastructure;

/// <summary>
/// Các loại mã chính. Khóa chính trong database là varchar(10), không IDENTITY,
/// nên hệ thống tự sinh: tiền tố + số thứ tự, ví dụ KH0001, NV0012, HD0103.
/// </summary>
public sealed record LoaiMa(string Bang, string Cot, string TienTo)
{
    public static readonly LoaiMa Khoa = new("Khoa", "ma_khoa", "K");
    public static readonly LoaiMa Phong = new("Phong_lam_viec", "ma_phong", "P");
    public static readonly LoaiMa Ca = new("Ca_lam_viec", "ma_ca", "CA");
    public static readonly LoaiMa Thuoc = new("Thuoc", "ma_thuoc", "TH");
    public static readonly LoaiMa NhanVien = new("Nhan_vien", "ma_nv", "NV");
    public static readonly LoaiMa KhachHang = new("Khach_hang", "ma_kh", "KH");
    public static readonly LoaiMa LichLamViec = new("Lich_lam_viec", "ma_lich_lam_viec", "LV");
    public static readonly LoaiMa LichHen = new("Lich_hen", "ma_lich_hen", "LH");
    public static readonly LoaiMa HoSoKham = new("Ho_so_kham", "ma_ho_so", "HS");
    public static readonly LoaiMa HoaDon = new("Hoa_don", "ma_hoa_don", "HD");

    internal static readonly LoaiMa[] TatCa =
        { Khoa, Phong, Ca, Thuoc, NhanVien, KhachHang, LichLamViec, LichHen, HoSoKham, HoaDon };
}

public static class MaSoGenerator
{
    private const int DoDaiSoToiThieu = 4;
    private const int DoDaiMaToiDa = 10;

    /// <summary>
    /// Sinh mã kế tiếp. Phải gọi bên trong transaction của lệnh INSERT:
    /// UPDLOCK + HOLDLOCK giữ khóa phạm vi tới khi transaction kết thúc,
    /// nên hai yêu cầu đồng thời không nhận cùng một mã.
    /// Mã cũ có định dạng khác (không phải tiền tố + số) được bỏ qua khi tính số tiếp theo.
    /// </summary>
    public static async Task<string> TaoMaAsync(SqlConnection conn, SqlTransaction tx, LoaiMa loai, CancellationToken ct = default)
    {
        // Tên bảng/cột không thể truyền bằng tham số, nên chỉ chấp nhận các giá trị khai báo sẵn ở trên.
        if (!LoaiMa.TatCa.Contains(loai))
            throw new ArgumentException("Loại mã không hợp lệ.", nameof(loai));

        var sql = $@"
SELECT MAX(TRY_CAST(SUBSTRING({loai.Cot}, LEN(@tienTo) + 1, 10) AS int)) AS so_lon_nhat,
       MAX(LEN({loai.Cot}) - LEN(@tienTo))                              AS do_dai_so
FROM {loai.Bang} WITH (UPDLOCK, HOLDLOCK)
WHERE {loai.Cot} LIKE @mau";

        await using var cmd = conn.Cmd(sql, tx)
            .P("@tienTo", SqlDbType.VarChar, 10, loai.TienTo)
            .P("@mau", SqlDbType.VarChar, 20, loai.TienTo + "[0-9]%");
        await using var r = await cmd.ExecuteReaderAsync(ct);
        await r.ReadAsync(ct);
        var soLonNhat = r.IsDBNull(0) ? 0 : r.GetInt32(0);
        var doDaiSo = r.IsDBNull(1) ? DoDaiSoToiThieu : Math.Max(DoDaiSoToiThieu, r.GetInt32(1));

        var ma = loai.TienTo + (soLonNhat + 1).ToString().PadLeft(doDaiSo, '0');
        if (ma.Length > DoDaiMaToiDa)
            throw new InvalidOperationException($"Đã hết mã khả dụng cho {loai.Bang}.");
        return ma;
    }
}
