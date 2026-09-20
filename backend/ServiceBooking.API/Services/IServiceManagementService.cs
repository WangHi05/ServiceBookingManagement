using ServiceBooking.API.Common;
using ServiceBooking.API.DTOs.Services;

namespace ServiceBooking.API.Services;

public interface IServiceManagementService
{
    Task<PagedResult<ServiceDto>> GetListAsync(ServiceQueryDto query);
    Task<ServiceDto> GetByIdAsync(int id);
    Task<ServiceDto> CreateAsync(CreateServiceDto dto);
    Task<ServiceDto> UpdateAsync(int id, UpdateServiceDto dto);
}
