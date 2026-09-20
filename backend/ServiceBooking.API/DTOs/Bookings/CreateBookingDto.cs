using System.ComponentModel.DataAnnotations;

namespace ServiceBooking.API.DTOs.Bookings;

public class CreateBookingDto
{
    [Required(ErrorMessage = "ServiceId là bắt buộc")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "StaffId là bắt buộc")]
    public int StaffId { get; set; }

    /// <summary>Chỉ cần chọn thời gian bắt đầu; EndTime được backend tính tự động.</summary>
    [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
    public DateTime StartTime { get; set; }

    [MaxLength(500)]
    public string? CustomerNote { get; set; }
}
