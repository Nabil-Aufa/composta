using Composta.Models;

namespace Composta.Services
{
    public interface IUserSessionService
    {
        Task<List<User>> GetUsersAsync();
        Task<User> RegisterUserAsync(string name, string email);
        Task LoginAsync(string userId);
        void Logout();
    }
}
