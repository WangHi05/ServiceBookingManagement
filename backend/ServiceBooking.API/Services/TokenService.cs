using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ServiceBooking.API.Models;

namespace ServiceBooking.API.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = jwtSection["Key"]
            ?? throw new InvalidOperationException("Jwt:Key chưa được cấu hình trong appsettings.json");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var expiresMinutes = int.Parse(jwtSection["ExpiresMinutes"] ?? "120");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            // Dùng "role" (chuỗi ngắn) thay vì ClaimTypes.Role (URI dài
            // "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").
            // Lý do: JWT ghi claim theo đúng Type truyền vào, nên nếu dùng ClaimTypes.Role,
            // key thật trong payload JWT sẽ là cái URI dài đó — phía frontend (jwt-decode)
            // không thể đọc field "role" được nữa (luôn undefined), làm middleware Next.js
            // chặn nhầm cả Admin thật khi vào /admin/*.
            // Khi backend ĐỌC LẠI token này (JwtBearer middleware), JwtSecurityTokenHandler
            // tự động map ngược "role" -> ClaimTypes.Role qua DefaultInboundClaimTypeMap,
            // nên [Authorize(Roles = "Admin")] và User.IsInRole(...) vẫn hoạt động bình
            // thường phía backend, không cần sửa gì thêm ở Program.cs.
            new("role", user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
