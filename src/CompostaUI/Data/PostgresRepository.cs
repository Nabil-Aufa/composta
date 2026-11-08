using Npgsql;

namespace Composta.Data
{
    // Kelas induk untuk semua repository PostgreSQL (Inheritance)
    public abstract class PostgresRepository<T> where T : class
    {
        // Encapsulation: connection string hanya bisa diakses dari dalam kelas ini
        private readonly string _connstring;

        protected PostgresRepository(string connstring)
        {
            if (string.IsNullOrWhiteSpace(connstring))
            {
                throw new ArgumentException("Connection string tidak boleh kosong.", nameof(connstring));
            }

            _connstring = connstring;
        }

        // Polymorphism: setiap repository turunan menentukan sendiri cara mengubah
        // satu baris hasil query menjadi objek
        protected abstract T MapRow(NpgsqlDataReader rd);

        protected NpgsqlConnection OpenConnection()
        {
            NpgsqlConnection conn = new NpgsqlConnection(_connstring);
            conn.Open();
            return conn;
        }

        protected List<T> ExecuteSelect(string sql, Action<NpgsqlParameterCollection>? addParameters = null)
        {
            using NpgsqlConnection conn = OpenConnection();
            return ExecuteSelect(conn, sql, MapRow, addParameters);
        }

        protected static List<TResult> ExecuteSelect<TResult>(NpgsqlConnection conn, string sql,
            Func<NpgsqlDataReader, TResult> map, Action<NpgsqlParameterCollection>? addParameters = null)
        {
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, conn);
            addParameters?.Invoke(cmd.Parameters);

            List<TResult> result = new List<TResult>();
            using NpgsqlDataReader rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                result.Add(map(rd));
            }

            return result;
        }

        // Menjalankan fungsi st_insert / st_update / st_delete yang mengembalikan 1 (berhasil) atau 0 (gagal)
        protected int ExecuteFunction(string sql, Action<NpgsqlParameterCollection> addParameters)
        {
            using NpgsqlConnection conn = OpenConnection();
            return ExecuteFunction(conn, null, sql, addParameters);
        }

        protected static int ExecuteFunction(NpgsqlConnection conn, NpgsqlTransaction? tx, string sql,
            Action<NpgsqlParameterCollection> addParameters)
        {
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, conn, tx);
            addParameters(cmd.Parameters);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        protected static void EnsureSuccess(int result, string message)
        {
            if (result != 1)
            {
                throw new InvalidOperationException(message);
            }
        }

        protected static object DbValue(object? value)
        {
            return value ?? DBNull.Value;
        }
    }
}
