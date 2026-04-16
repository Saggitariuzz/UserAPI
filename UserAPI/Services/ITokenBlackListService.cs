namespace UserAPI.Services
{
    public interface ITokenBlackListService
    {
        Task DeactivateTokenAsync(string token);

        Task<bool> IsTokenBlackListedAsync(string token);
    }
}
