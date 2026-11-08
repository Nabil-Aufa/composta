using Composta.Models;
using Npgsql;

namespace Composta.Data
{
    public class UserRepository : PostgresRepository<User>
    {
        public UserRepository(string connstring) : base(connstring)
        {
        }

        public List<User> GetAll()
        {
            return ExecuteSelect("SELECT * FROM st_select_users()");
        }

        public User? GetById(string id)
        {
            return GetAll().FirstOrDefault(u => u.Id == id);
        }

        protected override User MapRow(NpgsqlDataReader rd)
        {
            User user = new User((string)rd["name"], (string)rd["email"]);
            user.Id = (string)rd["id"];
            user.JoinedAt = (DateTime)rd["joined_at"];
            return user;
        }
    }
}
