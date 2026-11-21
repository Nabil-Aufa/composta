using Composta.Models;

namespace Composta.Services
{
    public class UserSessionService : IUserSessionService
    {
        public Task<List<User>> GetUsersAsync() => Task.Run(AppData.GetUsers);

        public Task<User> RegisterUserAsync(string name, string email) =>
            Task.Run(() => AppData.RegisterUser(name, email));

        public Task LoginAsync(string userId) => Task.Run(() => AppData.Login(userId));

        public void Logout() => AppData.Logout();
    }
}
