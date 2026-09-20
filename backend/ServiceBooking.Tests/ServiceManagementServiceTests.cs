using System.Net;
using ServiceBooking.API.Common;
using ServiceBooking.API.DTOs.Services;
using ServiceBooking.API.Services;
using Xunit;

namespace ServiceBooking.Tests;

public class ServiceManagementServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly ServiceManagementService _sut;

    public ServiceManagementServiceTests()
    {
        _factory = new TestDbContextFactory();
        _sut = new ServiceManagementService(_factory.Context);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CreateAsync_ThrowsBadRequest_WhenDurationIsZero()
    {
        var dto = new CreateServiceDto { Name = "Test", DurationMinutes = 0, Price = 100_000 };

        var ex = await Assert.ThrowsAsync<ApiException>(() => _sut.CreateAsync(dto));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ThrowsBadRequest_WhenPriceIsNegative()
    {
        var dto = new CreateServiceDto { Name = "Test", DurationMinutes = 30, Price = -1000 };

        var ex = await Assert.ThrowsAsync<ApiException>(() => _sut.CreateAsync(dto));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_Succeeds_WhenDataIsValid()
    {
        var dto = new CreateServiceDto { Name = "Dịch vụ test", DurationMinutes = 45, Price = 0 }; // Price = 0 hợp lệ (chỉ cấm âm)

        var result = await _sut.CreateAsync(dto);

        Assert.True(result.IsActive); // Dịch vụ mới tạo luôn mở sẵn
        Assert.Equal(45, result.DurationMinutes);
    }

    [Fact]
    public async Task UpdateAsync_CanToggleIsActive_ToLockService()
    {
        var created = await _sut.CreateAsync(new CreateServiceDto { Name = "Test khóa", DurationMinutes = 30, Price = 50_000 });

        var updated = await _sut.UpdateAsync(created.Id, new UpdateServiceDto
        {
            Name = created.Name,
            DurationMinutes = created.DurationMinutes,
            Price = created.Price,
            IsActive = false
        });

        Assert.False(updated.IsActive);
    }
}
