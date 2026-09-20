namespace ServiceBooking.API.DTOs.Bookings;

public class BookingDto
{
    public int Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;

    public int StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CustomerNote { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
