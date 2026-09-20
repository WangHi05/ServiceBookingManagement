using Microsoft.EntityFrameworkCore;
using ServiceBooking.API.Common;
using ServiceBooking.API.Data;
using ServiceBooking.API.DTOs.Services;

namespace ServiceBooking.API.Services;

public class ServiceManagementService : IServiceManagementService
{
    private readonly ApplicationDbContext _context;

    public ServiceManagementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ServiceDto>> GetListAsync(ServiceQueryDto query)
    {
        var servicesQuery = _context.Services.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim();
            servicesQuery = servicesQuery.Where(s => EF.Functions.Like(s.Name, $"%{keyword}%"));
        }

        if (query.IsActive.HasValue)
        {
            servicesQuery = servicesQuery.Where(s => s.IsActive == query.IsActive.Value);
        }

        var totalCount = await servicesQuery.CountAsync();

        // Phân trang tại database (Skip/Take dịch thành OFFSET/FETCH), không tải hết rồi mới cắt
        var items = await servicesQuery
            .OrderBy(s => s.Name)
            .Skip((query.SafePageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => MapToDto(s))
            .ToListAsync();

        return new PagedResult<ServiceDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.SafePageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<ServiceDto> GetByIdAsync(int id)
    {
        var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id)
            ?? throw ApiException.NotFound($"Không tìm thấy dịch vụ có Id = {id}.");

        return MapToDto(service);
    }

    public async Task<ServiceDto> CreateAsync(CreateServiceDto dto)
    {
        // DataAnnotations đã validate DurationMinutes > 0 và Price >= 0 ở tầng model binding,
        // nhưng vẫn kiểm tra lại ở đây để bảo vệ nếu service này được gọi trực tiếp (không qua controller).
        if (dto.DurationMinutes <= 0)
            throw ApiException.BadRequest("Thời lượng dịch vụ phải lớn hơn 0.");

        if (dto.Price < 0)
            throw ApiException.BadRequest("Giá dịch vụ không được âm.");

        var service = new Models.Service
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            DurationMinutes = dto.DurationMinutes,
            Price = dto.Price,
            IsActive = true
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return MapToDto(service);
    }

    public async Task<ServiceDto> UpdateAsync(int id, UpdateServiceDto dto)
    {
        var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw ApiException.NotFound($"Không tìm thấy dịch vụ có Id = {id}.");

        if (dto.DurationMinutes <= 0)
            throw ApiException.BadRequest("Thời lượng dịch vụ phải lớn hơn 0.");

        if (dto.Price < 0)
            throw ApiException.BadRequest("Giá dịch vụ không được âm.");

        service.Name = dto.Name.Trim();
        service.Description = dto.Description?.Trim();
        service.DurationMinutes = dto.DurationMinutes;
        service.Price = dto.Price;
        service.IsActive = dto.IsActive; // dùng để khóa/mở khóa dịch vụ

        await _context.SaveChangesAsync();

        return MapToDto(service);
    }

    private static ServiceDto MapToDto(Models.Service s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Description = s.Description,
        DurationMinutes = s.DurationMinutes,
        Price = s.Price,
        IsActive = s.IsActive
    };
}
