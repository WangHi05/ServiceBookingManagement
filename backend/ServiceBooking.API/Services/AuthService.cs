using Microsoft.EntityFrameworkCore;
using ServiceBooking.API.Common;
using ServiceBooking.API.Data;
using ServiceBooking.API.DTOs.Auth;

namespace ServiceBooking.API.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthService(ApplicationDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        // Không tiết lộ email có tồn tại hay không -> cùng 1 thông báo lỗi chung
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw ApiException.Unauthorized("Email hoặc mật khẩu không đúng.");
        }

        if (!user.IsActive)
        {
            throw ApiException.Forbidden("Tài khoản của bạn đã bị khóa.");
        }

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        return new LoginResponseDto
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = MapToDto(user)
        };
    }

    public async Task<UserDto> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw ApiException.NotFound("Không tìm thấy người dùng.");

        return MapToDto(user);
    }

    private static UserDto MapToDto(Models.User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role.ToString(),
        IsActive = user.IsActive
    };
}
