using System.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Api.Data;

/// <summary>
/// Nghiệp vụ thu ngân (M8). TG_Hoa_don_KiemTra vẫn chặn ở database: RB32 người thu phải là thu ngân,
/// không chuyển ngược về Chưa thanh toán, RB33 trừ kho khi chuyển sang Đã thanh toán; CK_Hoa_don_thanh_toan (RB31)
/// bắt buộc có giờ và người thu. Backend kiểm tra trước để báo lỗi rõ ràng.
/// </summary>
public class ThuNganRepository
{
    private readonly Db _db;
    public ThuNganRepository(Db db) => _db = db;

    private const string SqlDanhSach = @"
SELECT d.ma_hoa_don, d.tong_tien, d.trang_thai_thanh_toan, d.ngay_gio_thanh_toan,
       hs.ma_ho_so, hs.ngay_gio_kham, k.ma_kh, k.ho_ten_kh, k.sdt_kh,
       bs.ma_nv, bs.ho_ten_nv, kh.ten_khoa,
       (SELECT COUNT(*) FROM Chi_tiet_don_thuoc ct WHERE ct.ma_ho_so = hs.ma_ho_so),
       tn.ma_nv, tn.ho_ten_nv
FROM Hoa_don d
JOIN Ho_so_kham hs ON hs.ma_ho_so = d.ma_ho_so
JOIN Khach_hang k ON k.ma_kh = hs.ma_kh
JOIN Nhan_vien bs ON bs.ma_nv = hs.ma_nv_kham
JOIN Khoa kh ON kh.ma_khoa = bs.ma_khoa
LEFT JOIN Nhan_vien tn ON tn.ma_nv = d.ma_nv";

    private static HoaDonItem DocDong(SqlDataReader r) => new(
        r.GetString(0), r.GetDecimal(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetDateTime(3),
        r.GetString(4), r.GetDateTime(5), r.GetString(6), r.GetString(7), r.GetString(8),
        r.GetString(9), r.GetString(10), r.GetString(11), r.GetInt32(12), r.GetStringOrNull(13), r.GetStringOrNull(14));

    public async Task<TrangKetQua<HoaDonItem>> DanhSachAsync(HoaDonQuery q, string maThuNgan, CancellationToken ct)
    {
        const string where = @"
WHERE (@tt IS NULL OR d.trang_thai_thanh_toan = @tt)
  AND (@tu IS NULL OR d.ngay_gio_thanh_toan >= @tu) AND (@den IS NULL OR d.ngay_gio_thanh_toan < DATEADD(day, 1, @den))
  AND (@tn IS NULL OR d.ma_nv = @tn)
  AND (@q IS NULL OR d.ma_hoa_don LIKE @q OR hs.ma_ho_so LIKE @q OR k.ho_ten_kh LIKE @q OR k.sdt_kh LIKE @q OR k.ma_kh LIKE @q)";
        await using var conn = await _db.OpenAsync(ct);
        SqlCommand Gan(SqlCommand c) => c
            .P("@tt", SqlDbType.NVarChar, 20, string.IsNullOrWhiteSpace(q.TrangThai) ? null : q.TrangThai.Trim())
            .P("@tu", SqlDbType.DateTime, 0, q.TuNgay?.ToDateTime(TimeOnly.MinValue))
            .P("@den", SqlDbType.DateTime, 0, q.DenNgay?.ToDateTime(TimeOnly.MinValue))
            .P("@tn", SqlDbType.VarChar, 10, q.CuaToi ? maThuNgan : null)
            .P("@q", SqlDbType.NVarChar, 60, q.MauTim);
        var total = (int)(await Gan(conn.Cmd(@"SELECT COUNT(*) FROM Hoa_don d JOIN Ho_so_kham hs ON hs.ma_ho_so = d.ma_ho_so
JOIN Khach_hang k ON k.ma_kh = hs.ma_kh" + where)).ExecuteScalarAsync(ct))!;
        // Chờ thu: hóa đơn cũ nhất lên đầu (khách đợi lâu nhất); đã thu: mới thu nhất lên đầu
        await using var cmd = Gan(conn.Cmd(SqlDanhSach + where + @"
ORDER BY CASE WHEN d.trang_thai_thanh_toan = N'Chưa thanh toán' THEN 0 ELSE 1 END,
         CASE WHEN d.trang_thai_thanh_toan = N'Chưa thanh toán' THEN hs.ngay_gio_kham END,
         d.ngay_gio_thanh_toan DESC, d.ma_hoa_don DESC
OFFSET @bo ROWS FETCH NEXT @lay ROWS ONLY")).P("@bo", q.BoQua).P("@lay", q.CoTrangHopLe);
        return new(await DocDanhSach(cmd, DocDong, ct), total, q.TrangHopLe, q.CoTrangHopLe);
    }

    public async Task<HoaDonChiTiet?> ChiTietAsync(string ma, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await ChiTietAsync(conn, null, ma, ct);
    }

    private static async Task<HoaDonChiTiet?> ChiTietAsync(SqlConnection conn, SqlTransaction? tx, string ma, CancellationToken ct)
    {
        HoaDonChiTiet hd;
        await using (var cmd = conn.Cmd(@"
SELECT d.ma_hoa_don, d.tong_tien, d.trang_thai_thanh_toan, d.ngay_gio_thanh_toan, tn.ma_nv, tn.ho_ten_nv,
       hs.ma_ho_so, hs.ngay_gio_kham, hs.trieu_chung, hs.chan_doan_benh, hs.loi_dan, hs.tien_cong_kham,
       k.ma_kh, k.ho_ten_kh, k.sdt_kh, k.gioi_tinh_kh, k.ngay_sinh_kh, k.dia_chi_kh,
       bs.ma_nv, bs.ho_ten_nv, kh.ten_khoa
FROM Hoa_don d
JOIN Ho_so_kham hs ON hs.ma_ho_so = d.ma_ho_so
JOIN Khach_hang k ON k.ma_kh = hs.ma_kh
JOIN Nhan_vien bs ON bs.ma_nv = hs.ma_nv_kham
JOIN Khoa kh ON kh.ma_khoa = bs.ma_khoa
LEFT JOIN Nhan_vien tn ON tn.ma_nv = d.ma_nv
WHERE d.ma_hoa_don = @ma", tx).P("@ma", SqlDbType.VarChar, 10, ma))
        await using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct)) return null;
            hd = new HoaDonChiTiet(r.GetString(0), r.GetDecimal(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetDateTime(3),
                r.GetStringOrNull(4), r.GetStringOrNull(5),
                r.GetString(6), r.GetDateTime(7), r.GetString(8), r.GetStringOrNull(9), r.GetStringOrNull(10), r.GetDecimal(11),
                r.GetString(12), r.GetString(13), r.GetString(14), r.GetString(15), DateOnly.FromDateTime(r.GetDateTime(16)), r.GetStringOrNull(17),
                r.GetString(18), r.GetString(19), r.GetString(20), new(), 0);
        }
        await using var c2 = conn.Cmd(@"
SELECT ct.ma_thuoc, t.ten_thuoc, t.ham_luong, t.don_vi_tinh, ct.so_luong_thuoc, ct.lieu_dung, ct.don_gia_thuoc,
       ct.so_luong_thuoc * ct.don_gia_thuoc, t.so_luong_ton
FROM Chi_tiet_don_thuoc ct JOIN Thuoc t ON t.ma_thuoc = ct.ma_thuoc
WHERE ct.ma_ho_so = @hs ORDER BY t.ten_thuoc", tx).P("@hs", SqlDbType.VarChar, 10, hd.MaHoSo);
        var thuoc = await DocDanhSach(c2, r => new DongHoaDon(r.GetString(0), r.GetString(1), r.GetDecimal(2), r.GetString(3),
            r.GetInt32(4), r.GetString(5), r.GetDecimal(6), r.GetDecimal(7), r.GetInt32(8)), ct);
        return hd with { Thuoc = thuoc, TienThuoc = thuoc.Sum(t => t.ThanhTien) };
    }

    /// <summary>
    /// Xác nhận đã thu tiền. Khóa dòng hóa đơn rồi mới kiểm tra trạng thái, nên bấm hai lần (hoặc hai thu ngân cùng bấm)
    /// chỉ một lần thành công, lần sau nhận 409. Khóa luôn các dòng thuốc trong đơn để hai hóa đơn dùng chung thuốc
    /// không cùng qua bước kiểm tra tồn. Trigger trừ kho (RB33) trong cùng giao dịch.
    /// </summary>
    public Task<HoaDonChiTiet?> ThanhToanAsync(string ma, string maThuNgan, DateTime bayGio, CancellationToken ct)
        => _db.InTransactionAsync<HoaDonChiTiet?>(async (conn, tx) =>
        {
            string trangThai, maHoSo; DateTime? luc; string? nguoiThu;
            await using (var c = conn.Cmd(@"
SELECT d.trang_thai_thanh_toan, d.ma_ho_so, d.ngay_gio_thanh_toan, tn.ho_ten_nv
FROM Hoa_don d WITH (UPDLOCK, ROWLOCK) LEFT JOIN Nhan_vien tn ON tn.ma_nv = d.ma_nv
WHERE d.ma_hoa_don = @ma", tx).P("@ma", SqlDbType.VarChar, 10, ma))
            await using (var r = await c.ExecuteReaderAsync(ct))
            {
                if (!await r.ReadAsync(ct)) return null;
                trangThai = r.GetString(0); maHoSo = r.GetString(1);
                luc = r.IsDBNull(2) ? null : r.GetDateTime(2); nguoiThu = r.GetStringOrNull(3);
            }
            if (trangThai == TrangThaiHoaDon.DaThanhToan)
                throw AppException.Conflict($"Hóa đơn {ma} đã được thanh toán lúc {luc:HH:mm dd/MM/yyyy}" + (nguoiThu is null ? "." : $" bởi {nguoiThu}."));

            // Kho có thể đã giảm sau khi bác sĩ hoàn tất (hóa đơn khác đã thu): báo rõ thay vì để lỗi kho âm
            await using (var c = conn.Cmd(@"
SELECT t.ten_thuoc, ct.so_luong_thuoc, t.so_luong_ton
FROM Chi_tiet_don_thuoc ct JOIN Thuoc t WITH (UPDLOCK, ROWLOCK) ON t.ma_thuoc = ct.ma_thuoc
WHERE ct.ma_ho_so = @hs", tx).P("@hs", SqlDbType.VarChar, 10, maHoSo))
            {
                var thieu = (await DocDanhSach(c, r => (Ten: r.GetString(0), Ke: r.GetInt32(1), Ton: r.GetInt32(2)), ct))
                    .Where(x => x.Ke > x.Ton).ToList();
                if (thieu.Count > 0)
                    throw AppException.BadRequest("Kho không đủ thuốc để thanh toán: "
                        + string.Join("; ", thieu.Select(x => $"{x.Ten} cần {x.Ke}, còn {x.Ton}"))
                        + ". Vui lòng báo quản trị viên nhập thêm thuốc rồi thanh toán lại.");
            }

            await conn.Cmd(@"
UPDATE Hoa_don SET trang_thai_thanh_toan = N'Đã thanh toán', ngay_gio_thanh_toan = @luc, ma_nv = @tn
WHERE ma_hoa_don = @ma AND trang_thai_thanh_toan = N'Chưa thanh toán'", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma).P("@luc", SqlDbType.DateTime, 0, bayGio)
                .P("@tn", SqlDbType.VarChar, 10, maThuNgan).ExecuteNonQueryAsync(ct);
            return await ChiTietAsync(conn, tx, ma, ct);
        }, ct: ct);

    public async Task<TongQuanThuNgan> TongQuanAsync(string maThuNgan, DateTime bayGio, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        int cho, daThu, cuaToi; decimal tienCho, doanhThu;
        await using (var cmd = conn.Cmd(@"
SELECT
  SUM(CASE WHEN trang_thai_thanh_toan = N'Chưa thanh toán' THEN 1 ELSE 0 END),
  SUM(CASE WHEN trang_thai_thanh_toan = N'Chưa thanh toán' THEN tong_tien ELSE 0 END),
  SUM(CASE WHEN ngay_gio_thanh_toan >= @homNay THEN 1 ELSE 0 END),
  SUM(CASE WHEN ngay_gio_thanh_toan >= @homNay THEN tong_tien ELSE 0 END),
  SUM(CASE WHEN ngay_gio_thanh_toan >= @homNay AND ma_nv = @tn THEN 1 ELSE 0 END)
FROM Hoa_don WHERE trang_thai_thanh_toan = N'Chưa thanh toán' OR ngay_gio_thanh_toan >= @homNay")
            .P("@homNay", SqlDbType.DateTime, 0, bayGio.Date).P("@tn", SqlDbType.VarChar, 10, maThuNgan))
        await using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            await r.ReadAsync(ct);
            cho = r.IsDBNull(0) ? 0 : r.GetInt32(0); tienCho = r.IsDBNull(1) ? 0 : r.GetDecimal(1);
            daThu = r.IsDBNull(2) ? 0 : r.GetInt32(2); doanhThu = r.IsDBNull(3) ? 0 : r.GetDecimal(3);
            cuaToi = r.IsDBNull(4) ? 0 : r.GetInt32(4);
        }
        var theoGio = new int[24];
        await using (var cmd = conn.Cmd(@"
SELECT DATEPART(hour, ngay_gio_thanh_toan), COUNT(*) FROM Hoa_don
WHERE ngay_gio_thanh_toan >= @homNay AND ngay_gio_thanh_toan < DATEADD(day, 1, @homNay)
GROUP BY DATEPART(hour, ngay_gio_thanh_toan)").P("@homNay", SqlDbType.DateTime, 0, bayGio.Date))
        await using (var r = await cmd.ExecuteReaderAsync(ct))
            while (await r.ReadAsync(ct)) theoGio[r.GetInt32(0)] = r.GetInt32(1);
        return new(cho, tienCho, daThu, doanhThu, cuaToi, theoGio);
    }

    private static async Task<List<T>> DocDanhSach<T>(SqlCommand cmd, Func<SqlDataReader, T> doc, CancellationToken ct)
    {
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var ds = new List<T>();
        while (await r.ReadAsync(ct)) ds.Add(doc(r));
        return ds;
    }
}
