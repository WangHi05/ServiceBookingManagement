using System.ComponentModel.DataAnnotations;
using ServiceBooking.API.Models;

namespace ServiceBooking.API.DTOs.Bookings;

public class UpdateBookingStatusDto
{
    [Required(ErrorMessage = "Status là bắt buộc")]
    public BookingStatus Status { get; set; }

    /// <summary>Bắt buộc nếu Status = Cancelled.</summary>
    [MaxLength(500)]
    public string? CancellationReason { get; set; }
}
