using System.ComponentModel.DataAnnotations;

namespace ServiceBooking.API.DTOs.Bookings;

public class CancelBookingDto
{
    [Required(ErrorMessage = "Lý do hủy là bắt buộc")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
