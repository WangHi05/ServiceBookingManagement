using ServiceBooking.API.Models;

namespace ServiceBooking.API.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
