using UserAPI.Models;

namespace UserAPI.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllAsync();

        Task<User> GetUserAsync(string id);

        Task CreateAsync(User user);

        Task UpdateAsync(string id, User user);

        Task DeleteAsync(string id);

        Task<User> AuthenticateAsync(string login, string password);
    }
}
