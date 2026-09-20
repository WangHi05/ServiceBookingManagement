using ServiceBooking.API.DTOs.Common;

namespace ServiceBooking.API.DTOs.Services;

public class ServiceQueryDto : PaginationQuery
{
    /// <summary>Tìm theo tên dịch vụ (chứa chuỗi, không phân biệt hoa thường)</summary>
    public string? Search { get; set; }

    /// <summary>Lọc theo trạng thái hoạt động. Null = lấy tất cả (dùng cho Admin).</summary>
    public bool? IsActive { get; set; }
}
