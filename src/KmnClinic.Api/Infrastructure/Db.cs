using System.Data;
using Microsoft.Data.SqlClient;

namespace KmnClinic.Api.Infrastructure;

/// <summary>
/// Tạo kết nối SQL Server. Mọi truy vấn trong dự án dùng tham số (SqlParameter), không nối chuỗi.
/// </summary>
public class Db
{
    private readonly string _connectionString;

    public Db(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PhongKham") ?? "";
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException(
                "Chưa cấu hình chuỗi kết nối 'PhongKham'. Đặt bằng User Secrets hoặc biến môi trường ConnectionStrings__PhongKham.");
    }

    public async Task<SqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    private const int SoLanThuToiDa = 3;

    /// <summary>
    /// Chạy một khối công việc trong transaction: commit khi xong, rollback khi có lỗi.
    /// Nếu SQL Server chọn transaction này làm nạn nhân deadlock (lỗi 1205) thì chạy lại cả khối, tối đa 3 lần:
    /// toàn bộ thay đổi đã bị rollback nên chạy lại là an toàn. Khối công việc không được làm gì ngoài database.
    /// </summary>
    public async Task<T> InTransactionAsync<T>(Func<SqlConnection, SqlTransaction, Task<T>> work,
        IsolationLevel isolation = IsolationLevel.ReadCommitted, CancellationToken ct = default)
    {
        for (int lan = 1; ; lan++)
        {
            try
            {
                return await ChayMotLan(work, isolation, ct);
            }
            catch (SqlException ex) when (ex.Number == 1205 && lan < SoLanThuToiDa)
            {
                await Task.Delay(Random.Shared.Next(20, 80) * lan, ct);
            }
        }
    }

    private async Task<T> ChayMotLan<T>(Func<SqlConnection, SqlTransaction, Task<T>> work, IsolationLevel isolation, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct);
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(isolation, ct);
        try
        {
            var result = await work(conn, tx);
            await tx.CommitAsync(ct);
            return result;
        }
        catch
        {
            try { await tx.RollbackAsync(CancellationToken.None); } catch { /* kết nối có thể đã đóng */ }
            throw;
        }
    }
}

public static class SqlExtensions
{
    public static SqlCommand Cmd(this SqlConnection conn, string sql, SqlTransaction? tx = null)
        => new(sql, conn, tx);

    /// <summary>Thêm tham số; null được đổi thành DBNull.</summary>
    public static SqlCommand P(this SqlCommand cmd, string name, object? value)
    {
        cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }

    /// <summary>Thêm tham số có kiểu rõ ràng (nên dùng cho varchar để tránh chuyển kiểu ngầm làm mất index).</summary>
    public static SqlCommand P(this SqlCommand cmd, string name, SqlDbType type, int size, object? value)
    {
        cmd.Parameters.Add(name, type, size).Value = value ?? DBNull.Value;
        return cmd;
    }

    public static string? GetStringOrNull(this SqlDataReader r, string col)
    {
        var i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    public static string? GetStringOrNull(this SqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);
}
