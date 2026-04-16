using UserAPI.Models;

namespace UserAPI.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
