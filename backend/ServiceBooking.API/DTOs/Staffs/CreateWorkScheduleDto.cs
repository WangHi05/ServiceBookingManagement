using System.ComponentModel.DataAnnotations;

namespace ServiceBooking.API.DTOs.Staffs;

public class CreateWorkScheduleDto : IValidatableObject
{
    [Required(ErrorMessage = "Ngày làm việc là bắt buộc")]
    public DateOnly WorkDate { get; set; }

    [Required(ErrorMessage = "Giờ bắt đầu là bắt buộc")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Giờ kết thúc là bắt buộc")]
    public TimeOnly EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime >= EndTime)
        {
            yield return new ValidationResult(
                "Giờ bắt đầu phải nhỏ hơn giờ kết thúc.",
                new[] { nameof(StartTime), nameof(EndTime) });
        }
    }
}
