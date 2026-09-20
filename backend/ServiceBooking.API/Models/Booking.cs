using System.ComponentModel.DataAnnotations;

namespace ServiceBooking.API.Models;

public class Booking
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string BookingCode { get; set; } = string.Empty; // unique, vd: BK000001

    public int CustomerId { get; set; }
    public User Customer { get; set; } = null!;

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public int StaffId { get; set; }
    public Staff Staff { get; set; } = null!;

    public DateTime StartTime { get; set; }

    // Tính từ backend: EndTime = StartTime + Service.DurationMinutes
    public DateTime EndTime { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    [MaxLength(500)]
    public string? CustomerNote { get; set; }

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
