using System.Globalization;
using Npgsql;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Composta.UITests
{
    public class DatabaseFixture
    {
        public const string NabilId = "3f1c2a9e-5b7d-4c1e-9a2f-000000000001";
        public const string DemoUserId = "3f1c2a9e-5b7d-4c1e-9a2f-000000000002";

        private const string DefaultTestConnstring =
            "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=composta_test";

        private static readonly string ScriptPath =
            Path.Combine(AppContext.BaseDirectory, "Database", "composta_postgres.sql");

        public DatabaseFixture()
        {
            Connstring = Environment.GetEnvironmentVariable("COMPOSTA_TEST_DB") ?? DefaultTestConnstring;

            string databaseName = new NpgsqlConnectionStringBuilder(Connstring).Database ?? string.Empty;
            if (string.Equals(databaseName, "composta", StringComparison.OrdinalIgnoreCase))
            {
                UnavailableReason = "COMPOSTA_TEST_DB tidak boleh memakai database utama 'composta' karena isinya akan dihapus.";
                return;
            }

            try
            {
                ResetDatabase();
            }
            catch (Exception ex)
            {
                UnavailableReason = "Database test tidak tersedia (" + databaseName + "): " + ex.Message;
                return;
            }

            Environment.SetEnvironmentVariable("COMPOSTA_DB", Connstring);
            IsAvailable = true;
        }

        public string Connstring { get; }

        public bool IsAvailable { get; }

        public string UnavailableReason { get; } = string.Empty;

        public void ResetDatabase()
        {
            using NpgsqlConnection conn = new NpgsqlConnection(Connstring);
            conn.Open();
            using NpgsqlCommand cmd = new NpgsqlCommand(File.ReadAllText(ScriptPath), conn);
            cmd.ExecuteNonQuery();
        }

        public long Count(string sql)
        {
            using NpgsqlConnection conn = new NpgsqlConnection(Connstring);
            conn.Open();
            using NpgsqlCommand cmd = new NpgsqlCommand(sql, conn);
            return Convert.ToInt64(cmd.ExecuteScalar());
        }
    }

    [CollectionDefinition("Database")]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
    }

    [Collection("Database")]
    public abstract class DatabaseTest
    {
        protected DatabaseTest(DatabaseFixture db)
        {
            CultureInfo.CurrentCulture = new CultureInfo("id-ID");
            CultureInfo.CurrentUICulture = new CultureInfo("id-ID");
            Db = db;
            if (db.IsAvailable)
            {
                db.ResetDatabase();
                AppData.Logout();
            }
        }

        protected DatabaseFixture Db { get; }

        protected void RequireDatabase()
        {
            Skip.IfNot(Db.IsAvailable, Db.UnavailableReason);
        }
    }
}
