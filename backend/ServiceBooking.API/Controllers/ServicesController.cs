using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBooking.API.Common;
using ServiceBooking.API.DTOs.Services;
using ServiceBooking.API.Services;

namespace ServiceBooking.API.Controllers;

[ApiController]
[Route("api/services")]
[Authorize] // phải đăng nhập mới xem được danh sách dịch vụ (Customer hoặc Admin)
public class ServicesController : ControllerBase
{
    private readonly IServiceManagementService _serviceManagementService;

    public ServicesController(IServiceManagementService serviceManagementService)
    {
        _serviceManagementService = serviceManagementService;
    }

    /// <summary>
    /// Danh sách dịch vụ, có tìm kiếm theo tên và phân trang.
    /// Customer mặc định chỉ nên gọi với isActive=true; Admin có thể xem cả dịch vụ bị khóa.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] ServiceQueryDto query)
    {
        var result = await _serviceManagementService.GetListAsync(query);
        return Ok(ApiResponse<PagedResult<ServiceDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _serviceManagementService.GetByIdAsync(id);
        return Ok(ApiResponse<ServiceDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateServiceDto dto)
    {
        var result = await _serviceManagementService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<ServiceDto>.Ok(result, "Tạo dịch vụ thành công."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateServiceDto dto)
    {
        var result = await _serviceManagementService.UpdateAsync(id, dto);
        return Ok(ApiResponse<ServiceDto>.Ok(result, "Cập nhật dịch vụ thành công."));
    }
}
