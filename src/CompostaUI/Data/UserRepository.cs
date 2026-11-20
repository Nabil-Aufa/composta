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
            return ExecuteSelect("SELECT * FROM st_select_user_by_id(:_id)",
                p => p.AddWithValue("_id", id)).FirstOrDefault();
        }

        public User? GetByEmail(string email)
        {
            return ExecuteSelect("SELECT * FROM st_select_user_by_email(:_email)",
                p => p.AddWithValue("_email", email)).FirstOrDefault();
        }

        public void Add(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            EnsureSuccess(ExecuteFunction("SELECT st_insert_user(:_id, :_name, :_email, :_joined_at)",
                p =>
                {
                    p.AddWithValue("_id", user.Id);
                    p.AddWithValue("_name", user.Name);
                    p.AddWithValue("_email", user.Email);
                    p.AddWithValue("_joined_at", user.JoinedAt);
                }), "Gagal menambahkan user ke database.");
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
