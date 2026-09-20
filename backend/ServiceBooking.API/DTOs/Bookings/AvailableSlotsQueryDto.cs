using System.ComponentModel.DataAnnotations;

namespace ServiceBooking.API.DTOs.Bookings;

public class AvailableSlotsQueryDto
{
    [Required(ErrorMessage = "ServiceId là bắt buộc")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Date là bắt buộc")]
    public DateOnly Date { get; set; }

    /// <summary>Không truyền = lấy khung giờ trống của tất cả nhân viên đang hoạt động.</summary>
    public int? StaffId { get; set; }
}
