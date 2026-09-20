namespace ServiceBooking.API.DTOs.Bookings;

public class AvailableSlotDto
{
    public int StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
