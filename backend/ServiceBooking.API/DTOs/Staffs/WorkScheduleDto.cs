namespace ServiceBooking.API.DTOs.Staffs;

public class WorkScheduleDto
{
    public int Id { get; set; }
    public int StaffId { get; set; }
    public DateOnly WorkDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
