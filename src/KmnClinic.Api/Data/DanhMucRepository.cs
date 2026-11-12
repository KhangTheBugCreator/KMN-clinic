using System.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Api.Data;

/// <summary>Khoa, Phòng làm việc, Ca làm việc, Thuốc (M1). Chỉ dùng cột có sẵn trong database.</summary>
public class DanhMucRepository
{
    private readonly Db _db;
    private readonly int _nguongSapHet;

    public DanhMucRepository(Db db, IConfiguration config)
    {
        _db = db;
        _nguongSapHet = config.GetValue("PhongKham:NguongThuocSapHet", 20);
    }

    public int NguongSapHet => _nguongSapHet;

    private static string? Rong(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /* =============================== KHOA =============================== */

    private const string SqlKhoa = @"
SELECT k.ma_khoa, k.ten_khoa, k.mo_ta,
       (SELECT COUNT(*) FROM Phong_lam_viec p WHERE p.ma_khoa = k.ma_khoa) AS so_phong,
       (SELECT COUNT(*) FROM Nhan_vien n WHERE n.ma_khoa = k.ma_khoa) AS so_bac_si
FROM Khoa k";

    private static KhoaItem DocKhoa(SqlDataReader r) =>
        new(r.GetString(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), r.GetInt32(3), r.GetInt32(4));

    public async Task<TrangKetQua<KhoaItem>> DanhSachKhoaAsync(TimKiemQuery q, CancellationToken ct)
    {
        const string where = " WHERE @q IS NULL OR k.ten_khoa LIKE @q OR k.ma_khoa LIKE @q";
        await using var conn = await _db.OpenAsync(ct);
        var total = (int)(await conn.Cmd("SELECT COUNT(*) FROM Khoa k" + where).P("@q", SqlDbType.NVarChar, 60, q.MauTim).ExecuteScalarAsync(ct))!;
        await using var cmd = conn.Cmd(SqlKhoa + where + " ORDER BY k.ten_khoa OFFSET @bo ROWS FETCH NEXT @lay ROWS ONLY")
            .P("@q", SqlDbType.NVarChar, 60, q.MauTim).P("@bo", q.BoQua).P("@lay", q.CoTrangHopLe);
        return new(await DocDanhSach(cmd, DocKhoa, ct), total, q.TrangHopLe, q.CoTrangHopLe);
    }

    public async Task<KhoaItem?> TimKhoaAsync(string ma, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlKhoa + " WHERE k.ma_khoa = @ma").P("@ma", SqlDbType.VarChar, 10, ma);
        return (await DocDanhSach(cmd, DocKhoa, ct)).FirstOrDefault();
    }

    public Task<string> ThemKhoaAsync(KhoaRequest req, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            var ma = await MaSoGenerator.TaoMaAsync(conn, tx, LoaiMa.Khoa, ct);
            await conn.Cmd("INSERT INTO Khoa (ma_khoa, ten_khoa, mo_ta) VALUES (@ma, @ten, @mt)", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma).P("@ten", SqlDbType.NVarChar, 50, req.TenKhoa.Trim())
                .P("@mt", SqlDbType.NVarChar, 255, Rong(req.MoTa)).ExecuteNonQueryAsync(ct);
            return ma;
        }, ct: ct);

    public async Task<bool> SuaKhoaAsync(string ma, KhoaRequest req, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.Cmd("UPDATE Khoa SET ten_khoa = @ten, mo_ta = @mt WHERE ma_khoa = @ma")
            .P("@ma", SqlDbType.VarChar, 10, ma).P("@ten", SqlDbType.NVarChar, 50, req.TenKhoa.Trim())
            .P("@mt", SqlDbType.NVarChar, 255, Rong(req.MoTa)).ExecuteNonQueryAsync(ct) > 0;
    }

    /// <summary>Xóa khoa khi không còn phòng và bác sĩ. Kiểm tra và xóa trong cùng transaction.</summary>
    public Task<bool> XoaKhoaAsync(string ma, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            await using (var c = conn.Cmd(@"
SELECT (SELECT COUNT(*) FROM Phong_lam_viec WITH (UPDLOCK, HOLDLOCK) WHERE ma_khoa = @ma),
       (SELECT COUNT(*) FROM Nhan_vien WITH (UPDLOCK, HOLDLOCK) WHERE ma_khoa = @ma)", tx).P("@ma", SqlDbType.VarChar, 10, ma))
            await using (var r = await c.ExecuteReaderAsync(ct))
            {
                await r.ReadAsync(ct);
                int soPhong = r.GetInt32(0), soBs = r.GetInt32(1);
                if (soPhong > 0 || soBs > 0)
                    throw AppException.Conflict($"Không thể xóa: khoa còn {soPhong} phòng và {soBs} bác sĩ. Hãy chuyển hoặc xóa chúng trước.");
            }
            return await conn.Cmd("DELETE FROM Khoa WHERE ma_khoa = @ma", tx).P("@ma", SqlDbType.VarChar, 10, ma).ExecuteNonQueryAsync(ct) > 0;
        }, ct: ct);

    /* =============================== PHÒNG =============================== */

    private const string SqlPhong = @"
SELECT p.ma_phong, p.ten_phong, p.ma_khoa, k.ten_khoa,
       (SELECT COUNT(*) FROM Lich_lam_viec l WHERE l.ma_phong = p.ma_phong) AS so_llv
FROM Phong_lam_viec p JOIN Khoa k ON k.ma_khoa = p.ma_khoa";

    private static PhongItem DocPhong(SqlDataReader r) =>
        new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt32(4));

    public async Task<TrangKetQua<PhongItem>> DanhSachPhongAsync(TimKiemQuery q, string? maKhoa, CancellationToken ct)
    {
        const string where = " WHERE (@q IS NULL OR p.ten_phong LIKE @q OR p.ma_phong LIKE @q) AND (@khoa IS NULL OR p.ma_khoa = @khoa)";
        await using var conn = await _db.OpenAsync(ct);
        var total = (int)(await conn.Cmd("SELECT COUNT(*) FROM Phong_lam_viec p" + where)
            .P("@q", SqlDbType.NVarChar, 60, q.MauTim).P("@khoa", SqlDbType.VarChar, 10, Rong(maKhoa)).ExecuteScalarAsync(ct))!;
        await using var cmd = conn.Cmd(SqlPhong + where + " ORDER BY p.ten_phong OFFSET @bo ROWS FETCH NEXT @lay ROWS ONLY")
            .P("@q", SqlDbType.NVarChar, 60, q.MauTim).P("@khoa", SqlDbType.VarChar, 10, Rong(maKhoa))
            .P("@bo", q.BoQua).P("@lay", q.CoTrangHopLe);
        return new(await DocDanhSach(cmd, DocPhong, ct), total, q.TrangHopLe, q.CoTrangHopLe);
    }

    public async Task<PhongItem?> TimPhongAsync(string ma, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlPhong + " WHERE p.ma_phong = @ma").P("@ma", SqlDbType.VarChar, 10, ma);
        return (await DocDanhSach(cmd, DocPhong, ct)).FirstOrDefault();
    }

    public Task<string> ThemPhongAsync(PhongRequest req, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            var ma = await MaSoGenerator.TaoMaAsync(conn, tx, LoaiMa.Phong, ct);
            await conn.Cmd("INSERT INTO Phong_lam_viec (ma_phong, ten_phong, ma_khoa) VALUES (@ma, @ten, @khoa)", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma).P("@ten", SqlDbType.NVarChar, 50, req.TenPhong.Trim())
                .P("@khoa", SqlDbType.VarChar, 10, req.MaKhoa).ExecuteNonQueryAsync(ct);
            return ma;
        }, ct: ct);

    /// <summary>
    /// Sửa phòng. Không cho đổi khoa khi phòng đã có lịch làm việc, vì RB11 (phòng thuộc khoa của bác sĩ)
    /// chỉ được trigger kiểm tra lúc tạo lịch: đổi khoa sau đó sẽ làm lịch cũ sai RB11.
    /// </summary>
    public Task<bool> SuaPhongAsync(string ma, PhongRequest req, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            string? khoaCu; int soLich;
            await using (var c = conn.Cmd(@"
SELECT p.ma_khoa, (SELECT COUNT(*) FROM Lich_lam_viec l WITH (UPDLOCK, HOLDLOCK) WHERE l.ma_phong = p.ma_phong)
FROM Phong_lam_viec p WITH (UPDLOCK) WHERE p.ma_phong = @ma", tx).P("@ma", SqlDbType.VarChar, 10, ma))
            await using (var r = await c.ExecuteReaderAsync(ct))
            {
                if (!await r.ReadAsync(ct)) return false;
                khoaCu = r.GetString(0); soLich = r.GetInt32(1);
            }
            if (khoaCu != req.MaKhoa && soLich > 0)
                throw AppException.Conflict($"Không thể đổi khoa: phòng đã có {soLich} lịch làm việc theo khoa hiện tại (RB11).");
            await conn.Cmd("UPDATE Phong_lam_viec SET ten_phong = @ten, ma_khoa = @khoa WHERE ma_phong = @ma", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma).P("@ten", SqlDbType.NVarChar, 50, req.TenPhong.Trim())
                .P("@khoa", SqlDbType.VarChar, 10, req.MaKhoa).ExecuteNonQueryAsync(ct);
            return true;
        }, ct: ct);

    public Task<bool> XoaPhongAsync(string ma, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            var soLich = (int)(await conn.Cmd("SELECT COUNT(*) FROM Lich_lam_viec WITH (UPDLOCK, HOLDLOCK) WHERE ma_phong = @ma", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma).ExecuteScalarAsync(ct))!;
            if (soLich > 0)
                throw AppException.Conflict($"Không thể xóa: phòng đã có {soLich} lịch làm việc. Lịch sử sử dụng phòng cần được giữ lại.");
            return await conn.Cmd("DELETE FROM Phong_lam_viec WHERE ma_phong = @ma", tx).P("@ma", SqlDbType.VarChar, 10, ma).ExecuteNonQueryAsync(ct) > 0;
        }, ct: ct);

    public async Task<TrangKetQua<LichSuDungPhongItem>> LichSuDungPhongAsync(string ma, TimKiemQuery q, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        var total = (int)(await conn.Cmd("SELECT COUNT(*) FROM Lich_lam_viec WHERE ma_phong = @ma")
            .P("@ma", SqlDbType.VarChar, 10, ma).ExecuteScalarAsync(ct))!;
        await using var cmd = conn.Cmd(@"
SELECT l.ma_lich_lam_viec, l.ngay_lam_viec, c.ten_ca, c.gio_bat_dau, c.gio_ket_thuc, n.ma_nv, n.ho_ten_nv,
       (SELECT COUNT(*) FROM Lich_hen h WHERE h.ma_lich_lam_viec = l.ma_lich_lam_viec AND h.trang_thai <> N'Đã hủy')
FROM Lich_lam_viec l JOIN Ca_lam_viec c ON c.ma_ca = l.ma_ca JOIN Nhan_vien n ON n.ma_nv = l.ma_nv
WHERE l.ma_phong = @ma
ORDER BY l.ngay_lam_viec DESC, c.gio_bat_dau DESC OFFSET @bo ROWS FETCH NEXT @lay ROWS ONLY")
            .P("@ma", SqlDbType.VarChar, 10, ma).P("@bo", q.BoQua).P("@lay", q.CoTrangHopLe);
        var ds = await DocDanhSach(cmd, r => new LichSuDungPhongItem(r.GetString(0), DateOnly.FromDateTime(r.GetDateTime(1)),
            r.GetString(2), TimeOnly.FromTimeSpan(r.GetTimeSpan(3)), TimeOnly.FromTimeSpan(r.GetTimeSpan(4)),
            r.GetString(5), r.GetString(6), r.GetInt32(7)), ct);
        return new(ds, total, q.TrangHopLe, q.CoTrangHopLe);
    }

    /* =============================== CA =============================== */

    private const string SqlCa = @"
SELECT c.ma_ca, c.ten_ca, c.gio_bat_dau, c.gio_ket_thuc,
       (SELECT COUNT(*) FROM Lich_lam_viec l WHERE l.ma_ca = c.ma_ca)
FROM Ca_lam_viec c";

    private static CaItem DocCa(SqlDataReader r) =>
        new(r.GetString(0), r.GetString(1), TimeOnly.FromTimeSpan(r.GetTimeSpan(2)), TimeOnly.FromTimeSpan(r.GetTimeSpan(3)), r.GetInt32(4));

    public async Task<List<CaItem>> DanhSachCaAsync(CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlCa + " ORDER BY c.gio_bat_dau, c.ten_ca");
        return await DocDanhSach(cmd, DocCa, ct);
    }

    public async Task<CaItem?> TimCaAsync(string ma, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlCa + " WHERE c.ma_ca = @ma").P("@ma", SqlDbType.VarChar, 10, ma);
        return (await DocDanhSach(cmd, DocCa, ct)).FirstOrDefault();
    }

    public Task<string> ThemCaAsync(CaRequest req, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            var ma = await MaSoGenerator.TaoMaAsync(conn, tx, LoaiMa.Ca, ct);
            await conn.Cmd("INSERT INTO Ca_lam_viec (ma_ca, ten_ca, gio_bat_dau, gio_ket_thuc) VALUES (@ma, @ten, @bd, @kt)", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma).P("@ten", SqlDbType.NVarChar, 20, req.TenCa.Trim())
                .P("@bd", SqlDbType.Time, 0, req.GioBatDau!.Value.ToTimeSpan())
                .P("@kt", SqlDbType.Time, 0, req.GioKetThuc!.Value.ToTimeSpan()).ExecuteNonQueryAsync(ct);
            return ma;
        }, ct: ct);

    /// <summary>
    /// Sửa ca. Đổi giờ của ca đã được xếp lịch sẽ làm sai lịch hẹn và lịch sử đã có,
    /// nên chỉ cho đổi tên; muốn đổi giờ thì tạo ca mới.
    /// </summary>
    public Task<bool> SuaCaAsync(string ma, CaRequest req, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            TimeSpan bd, kt; int soLich;
            await using (var c = conn.Cmd(@"
SELECT c.gio_bat_dau, c.gio_ket_thuc, (SELECT COUNT(*) FROM Lich_lam_viec l WITH (UPDLOCK, HOLDLOCK) WHERE l.ma_ca = c.ma_ca)
FROM Ca_lam_viec c WITH (UPDLOCK) WHERE c.ma_ca = @ma", tx).P("@ma", SqlDbType.VarChar, 10, ma))
            await using (var r = await c.ExecuteReaderAsync(ct))
            {
                if (!await r.ReadAsync(ct)) return false;
                bd = r.GetTimeSpan(0); kt = r.GetTimeSpan(1); soLich = r.GetInt32(2);
            }
            var doiGio = bd != req.GioBatDau!.Value.ToTimeSpan() || kt != req.GioKetThuc!.Value.ToTimeSpan();
            if (doiGio && soLich > 0)
                throw AppException.Conflict($"Không thể đổi giờ: ca đã được dùng trong {soLich} lịch làm việc. Chỉ được đổi tên, hoặc tạo ca mới với giờ khác.");
            await conn.Cmd("UPDATE Ca_lam_viec SET ten_ca = @ten, gio_bat_dau = @bd, gio_ket_thuc = @kt WHERE ma_ca = @ma", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma).P("@ten", SqlDbType.NVarChar, 20, req.TenCa.Trim())
                .P("@bd", SqlDbType.Time, 0, req.GioBatDau!.Value.ToTimeSpan())
                .P("@kt", SqlDbType.Time, 0, req.GioKetThuc!.Value.ToTimeSpan()).ExecuteNonQueryAsync(ct);
            return true;
        }, ct: ct);

    public Task<bool> XoaCaAsync(string ma, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            var soLich = (int)(await conn.Cmd("SELECT COUNT(*) FROM Lich_lam_viec WITH (UPDLOCK, HOLDLOCK) WHERE ma_ca = @ma", tx)
                .P("@ma", SqlDbType.VarChar, 10, ma).ExecuteScalarAsync(ct))!;
            if (soLich > 0)
                throw AppException.Conflict($"Không thể xóa: ca đã được dùng trong {soLich} lịch làm việc.");
            return await conn.Cmd("DELETE FROM Ca_lam_viec WHERE ma_ca = @ma", tx).P("@ma", SqlDbType.VarChar, 10, ma).ExecuteNonQueryAsync(ct) > 0;
        }, ct: ct);

    /* =============================== THUỐC =============================== */

    private const string SqlThuoc = @"
SELECT t.ma_thuoc, t.ten_thuoc, t.nsx, t.ham_luong, t.don_vi_tinh, t.don_gia_thuoc, t.so_luong_ton, t.mo_ta
FROM Thuoc t";

    private ThuocItem DocThuoc(SqlDataReader r) =>
        new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetDecimal(3), r.GetString(4), r.GetDecimal(5),
            r.GetInt32(6), r.IsDBNull(7) ? null : r.GetString(7), r.GetInt32(6) <= _nguongSapHet);

    public async Task<TrangKetQua<ThuocItem>> DanhSachThuocAsync(TimKiemQuery q, bool chiSapHet, CancellationToken ct)
    {
        const string where = " WHERE (@q IS NULL OR t.ten_thuoc LIKE @q OR t.nsx LIKE @q OR t.ma_thuoc LIKE @q) AND (@sapHet = 0 OR t.so_luong_ton <= @nguong)";
        await using var conn = await _db.OpenAsync(ct);
        SqlCommand Gan(SqlCommand c) => c.P("@q", SqlDbType.NVarChar, 210, q.MauTim).P("@sapHet", chiSapHet ? 1 : 0).P("@nguong", _nguongSapHet);
        var total = (int)(await Gan(conn.Cmd("SELECT COUNT(*) FROM Thuoc t" + where)).ExecuteScalarAsync(ct))!;
        await using var cmd = Gan(conn.Cmd(SqlThuoc + where
            + (chiSapHet ? " ORDER BY t.so_luong_ton, t.ten_thuoc" : " ORDER BY t.ten_thuoc, t.ham_luong")
            + " OFFSET @bo ROWS FETCH NEXT @lay ROWS ONLY")).P("@bo", q.BoQua).P("@lay", q.CoTrangHopLe);
        return new(await DocDanhSach(cmd, DocThuoc, ct), total, q.TrangHopLe, q.CoTrangHopLe);
    }

    public async Task<ThuocItem?> TimThuocAsync(string ma, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        await using var cmd = conn.Cmd(SqlThuoc + " WHERE t.ma_thuoc = @ma").P("@ma", SqlDbType.VarChar, 10, ma);
        return (await DocDanhSach(cmd, DocThuoc, ct)).FirstOrDefault();
    }

    private static SqlCommand GanThuoc(SqlCommand c, ThuocRequest req) => c
        .P("@ten", SqlDbType.NVarChar, 100, req.TenThuoc.Trim())
        .P("@nsx", SqlDbType.NVarChar, 200, req.Nsx.Trim())
        .P("@hl", req.HamLuong!.Value)
        .P("@dvt", SqlDbType.NVarChar, 20, req.DonViTinh.Trim())
        .P("@gia", req.DonGiaThuoc!.Value)
        .P("@ton", req.SoLuongTon!.Value)
        .P("@mt", SqlDbType.NVarChar, 255, Rong(req.MoTa));

    public Task<string> ThemThuocAsync(ThuocRequest req, CancellationToken ct) =>
        _db.InTransactionAsync(async (conn, tx) =>
        {
            var ma = await MaSoGenerator.TaoMaAsync(conn, tx, LoaiMa.Thuoc, ct);
            await GanThuoc(conn.Cmd(@"
INSERT INTO Thuoc (ma_thuoc, ten_thuoc, nsx, ham_luong, don_vi_tinh, don_gia_thuoc, so_luong_ton, mo_ta)
VALUES (@ma, @ten, @nsx, @hl, @dvt, @gia, @ton, @mt)", tx), req).P("@ma", SqlDbType.VarChar, 10, ma).ExecuteNonQueryAsync(ct);
            return ma;
        }, ct: ct);

    /// <summary>
    /// Sửa thuốc. Đổi đơn giá không ảnh hưởng đơn đã kê (RB28 lưu đơn giá lúc kê).
    /// Đổi tồn kho là thao tác quản trị có xác nhận ở giao diện (Q8).
    /// </summary>
    public async Task<bool> SuaThuocAsync(string ma, ThuocRequest req, CancellationToken ct)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await GanThuoc(conn.Cmd(@"
UPDATE Thuoc SET ten_thuoc = @ten, nsx = @nsx, ham_luong = @hl, don_vi_tinh = @dvt,
       don_gia_thuoc = @gia, so_luong_ton = @ton, mo_ta = @mt
WHERE ma_thuoc = @ma"), req).P("@ma", SqlDbType.VarChar, 10, ma).ExecuteNonQueryAsync(ct) > 0;
    }

    /* =============================== chung =============================== */

    private static async Task<List<T>> DocDanhSach<T>(SqlCommand cmd, Func<SqlDataReader, T> doc, CancellationToken ct)
    {
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var ds = new List<T>();
        while (await r.ReadAsync(ct)) ds.Add(doc(r));
        return ds;
    }
}
