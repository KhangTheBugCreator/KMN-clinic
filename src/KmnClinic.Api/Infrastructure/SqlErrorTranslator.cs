using Microsoft.Data.SqlClient;

namespace KmnClinic.Api.Infrastructure;

/// <summary>
/// Đổi lỗi SQL Server thành (mã HTTP, câu tiếng Việt).
/// - THROW 50000–50999 trong trigger: câu thông báo đã là tiếng Việt, trả nguyên văn với 400.
/// - Vi phạm UNIQUE (2627, 2601): 409, câu theo tên ràng buộc.
/// - Vi phạm CHECK (547 + "CHECK constraint"): 400, câu theo tên ràng buộc.
/// - Vi phạm khóa ngoại (547 + FOREIGN KEY / REFERENCE): 409.
/// Các lỗi khác trả null để middleware coi là lỗi hệ thống (500).
/// </summary>
public static class SqlErrorTranslator
{
    // Tên ràng buộc lấy đúng từ PHONGKHAM-SCRIPT.sql.
    private static readonly Dictionary<string, string> ThongBaoRangBuoc = new(StringComparer.OrdinalIgnoreCase)
    {
        ["UQ_Khoa_ten_khoa"] = "Tên khoa đã tồn tại.",
        ["UQ_PhongLamViec_ten_phong"] = "Tên phòng đã tồn tại.",
        ["UQ_Nhan_vien_sdt_nv"] = "Số điện thoại nhân viên đã được sử dụng.",
        ["UQ_Khach_hang_sdt_kh"] = "Số điện thoại khách hàng đã được sử dụng.",
        ["UQ_LICH_LAM_VIEC_nv_ca_ngay"] = "Nhân viên này đã có lịch làm việc ở ca đó trong ngày.",
        ["UQ_LICH_LAM_VIEC_phong_ca_ngay"] = "Phòng này đã được xếp lịch ở ca đó trong ngày.",
        ["UQ_Thuoc_ten_nsx_ham_luong"] = "Thuốc cùng tên, nhà sản xuất và hàm lượng đã tồn tại.",
        ["UQ_Hoa_don_ma_ho_so"] = "Hồ sơ khám này đã có hóa đơn.",
        ["UX_Ho_so_kham_ma_lich_hen"] = "Lịch hẹn này đã được tiếp nhận thành hồ sơ khám.",
        ["CK_Ca_lam_viec_gio"] = "Giờ bắt đầu ca phải nhỏ hơn giờ kết thúc.",
        ["CK_CTDT_so_luong"] = "Số lượng thuốc phải lớn hơn 0.",
        ["CK_CTDT_don_gia"] = "Đơn giá thuốc phải lớn hơn 0.",
        ["CK_Ho_so_kham_tien_cong"] = "Tiền công khám không được âm.",
        ["CK_Hoa_don_tong_tien"] = "Tổng tiền không được âm.",
        ["CK_Hoa_don_trang_thai"] = "Trạng thái thanh toán không hợp lệ.",
        ["CK_Hoa_don_thanh_toan"] = "Thông tin thanh toán không khớp với trạng thái hóa đơn.",
        ["CK_Khach_hang_gioi_tinh"] = "Giới tính chỉ nhận Nam hoặc Nữ.",
        ["CK_LICH_HEN_trang_thai"] = "Trạng thái lịch hẹn không hợp lệ.",
        ["CK_LICH_HEN_le_tan"] = "Lịch hẹn đã xác nhận hoặc đã khám phải có lễ tân phụ trách.",
        ["CK_Nhan_vien_chuc_vu"] = "Chức vụ không hợp lệ.",
        ["CK_Nhan_vien_khoa"] = "Bác sĩ phải thuộc một khoa; nhân viên khác không thuộc khoa nào.",
        ["CK_Thuoc_don_gia"] = "Đơn giá thuốc phải lớn hơn 0.",
        ["CK_Thuoc_so_luong_ton"] = "Số lượng tồn không được âm.",
    };

    public static (int Status, string Message)? Translate(SqlException ex)
    {
        // Lỗi đầu tiên là lỗi gốc; các lỗi sau thường là "The statement has been terminated."
        var err = ex.Errors.Count > 0 ? ex.Errors[0] : null;
        var number = err?.Number ?? ex.Number;
        var message = err?.Message ?? ex.Message;

        if (number is >= 50000 and <= 50999)
            return (StatusCodes.Status400BadRequest, message);

        if (number is 2627 or 2601)
            return (StatusCodes.Status409Conflict, TimThongBao(message) ?? "Dữ liệu bị trùng với bản ghi đã có.");

        if (number == 547)
        {
            if (message.Contains("CHECK constraint", StringComparison.OrdinalIgnoreCase))
                return (StatusCodes.Status400BadRequest, TimThongBao(message) ?? "Dữ liệu không thỏa ràng buộc của hệ thống.");

            if (message.Contains("DELETE statement", StringComparison.OrdinalIgnoreCase))
                return (StatusCodes.Status409Conflict, "Không thể xóa vì dữ liệu này đang được sử dụng ở nơi khác.");

            return (StatusCodes.Status409Conflict, "Dữ liệu tham chiếu không tồn tại hoặc không hợp lệ.");
        }

        return null;
    }

    private static string? TimThongBao(string sqlMessage)
    {
        foreach (var (ten, thongBao) in ThongBaoRangBuoc)
            if (sqlMessage.Contains($"'{ten}'", StringComparison.OrdinalIgnoreCase) ||
                sqlMessage.Contains($"\"{ten}\"", StringComparison.OrdinalIgnoreCase))
                return thongBao;
        return null;
    }
}
