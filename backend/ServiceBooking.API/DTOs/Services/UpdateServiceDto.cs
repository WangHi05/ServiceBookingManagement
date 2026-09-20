using System.ComponentModel.DataAnnotations;

namespace ServiceBooking.API.DTOs.Services;

public class UpdateServiceDto
{
    [Required(ErrorMessage = "Tên dịch vụ là bắt buộc")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Thời lượng phải lớn hơn 0")]
    public int DurationMinutes { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá không được âm")]
    public decimal Price { get; set; }

    /// <summary>true = đang mở cho đặt lịch, false = khóa dịch vụ</summary>
    public bool IsActive { get; set; }
}
