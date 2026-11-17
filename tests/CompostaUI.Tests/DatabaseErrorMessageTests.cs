using Composta.Data;
using Composta.Services;
using Npgsql;

namespace Composta.UITests
{
    public class DatabaseErrorMessageTests : DatabaseTest
    {
        public DatabaseErrorMessageTests(DatabaseFixture db) : base(db)
        {
        }

        private static Exception Capture(Action action)
        {
            return Assert.ThrowsAny<Exception>(action);
        }

        private string WithDatabase(string database)
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(Db.Connstring) { Database = database };
            return builder.ConnectionString;
        }

        [SkippableFact]
        public void OldSchema_AsksToRerunScript()
        {
            RequireDatabase();

            using (NpgsqlConnection conn = new NpgsqlConnection(Db.Connstring))
            {
                conn.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("DROP FUNCTION st_select_user_by_id(VARCHAR)", conn);
                cmd.ExecuteNonQuery();
            }

            Exception ex = Capture(() => new UserRepository(Db.Connstring).GetById("x"));
            string message = DatabaseErrorMessage.From(ex, "composta_test@localhost");

            Assert.StartsWith("Skema database composta_test@localhost belum sesuai", message);
            Assert.Contains("composta_postgres.sql", message);
        }

        [SkippableFact]
        public void MissingDatabase_AsksToCreateIt()
        {
            RequireDatabase();

            Exception ex = Capture(() => new UserRepository(WithDatabase("composta_tidak_ada")).GetAll());
            string message = DatabaseErrorMessage.From(ex, "composta_tidak_ada@localhost");

            Assert.StartsWith("Database composta_tidak_ada@localhost belum dibuat.", message);
        }

        [Fact]
        public void ServerNotRunning_AsksToStartPostgres()
        {
            string connstring = "Host=127.0.0.1;Port=1;Username=x;Password=x;Database=x;Timeout=2";

            Exception ex = Capture(() => new UserRepository(connstring).GetAll());
            string message = DatabaseErrorMessage.From(ex, "x@127.0.0.1:1");

            Assert.StartsWith("Tidak dapat terhubung ke server PostgreSQL", message);
        }

        [Fact]
        public void ApplicationError_KeepsOriginalMessage()
        {
            string message = DatabaseErrorMessage.From(new InvalidOperationException("Tabel users masih kosong."), "db");

            Assert.Equal("Tabel users masih kosong.", message);
        }

        [Fact]
        public void UnknownError_IncludesDatabaseAndDetail()
        {
            string message = DatabaseErrorMessage.From(new Exception("rusak"), "composta@localhost:5432");

            Assert.Equal("Terjadi kesalahan pada database composta@localhost:5432.\n\nrusak", message);
        }
    }
}
