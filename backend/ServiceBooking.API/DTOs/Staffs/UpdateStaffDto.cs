using System.ComponentModel.DataAnnotations;

namespace ServiceBooking.API.DTOs.Staffs;

public class UpdateStaffDto
{
    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
