using ServiceBooking.API.DTOs.Auth;

namespace ServiceBooking.API.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<UserDto> GetCurrentUserAsync(int userId);
}
