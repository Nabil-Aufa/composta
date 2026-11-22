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

        [SkippableFact]
        public void DuplicateEmail_IsReportedAsAlreadyRegistered()
        {
            RequireDatabase();

            Exception ex = Capture(() => new UserRepository(Db.Connstring)
                .Add(new Composta.Models.User("Nabil Lain", "nabil@composta.id")));
            string message = DatabaseErrorMessage.From(ex, "composta_test@localhost");

            Assert.Equal(UniqueViolationState, Assert.IsType<PostgresException>(ex).SqlState);
            Assert.StartsWith("Email tersebut sudah terdaftar.", message);
            Assert.Equal(2, Db.Count("SELECT COUNT(*) FROM users"));
        }

        [SkippableFact]
        public void MissingOwner_IsReportedAndBatchIsRolledBack()
        {
            RequireDatabase();

            Composta.Models.CompostBatch batch = new Composta.Models.CompostBatch("user-tidak-ada", DateTime.Now, 60);
            Exception ex = Capture(() => new CompostBatchRepository(Db.Connstring).Add(batch));
            string message = DatabaseErrorMessage.From(ex, "composta_test@localhost");

            Assert.StartsWith("Data terkait tidak ditemukan di database", message);
            Assert.Equal(5, Db.Count("SELECT COUNT(*) FROM compost_batches"));
            Assert.Equal(1, Db.Count("SELECT COUNT(*) FROM maintenance_schedules"));
        }

        [SkippableFact]
        public void RatioTooLarge_IsReportedAsNumberTooLarge()
        {
            RequireDatabase();

            Composta.Models.CompostBatch batch = new Composta.Models.CompostBatch(DatabaseFixture.DemoUserId, DateTime.Now, 60)
            {
                BrownGreenRatio = 1000
            };
            Exception ex = Capture(() => new CompostBatchRepository(Db.Connstring).Add(batch));

            Assert.Equal("Angka yang dimasukkan terlalu besar untuk disimpan.",
                DatabaseErrorMessage.From(ex, "composta_test@localhost"));
            Assert.Equal(5, Db.Count("SELECT COUNT(*) FROM compost_batches"));
        }

        [SkippableFact]
        public void NameTooLong_IsReportedAsTextTooLong()
        {
            RequireDatabase();

            Exception ex = Capture(() => new UserRepository(Db.Connstring)
                .Add(new Composta.Models.User(new string('a', 101), "panjang@composta.id")));

            Assert.Equal("Teks yang dimasukkan terlalu panjang untuk disimpan.",
                DatabaseErrorMessage.From(ex, "composta_test@localhost"));
        }

        [SkippableFact]
        public void NegativeWeight_IsReportedAsRuleViolation()
        {
            RequireDatabase();

            Exception ex = Capture(() =>
            {
                using NpgsqlConnection conn = new NpgsqlConnection(Db.Connstring);
                conn.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand(
                    "SELECT st_insert_waste_item('w-negatif', 'b8d0e6f2-4a1b-4c2d-9e3f-000000000003', NULL, " +
                    "'compostable', 'Uji', -1, NOW()::timestamp, 'Brown', 50, NULL)", conn);
                cmd.ExecuteScalar();
            });

            Assert.Equal("Data tidak memenuhi aturan database, periksa kembali isian yang dimasukkan.",
                DatabaseErrorMessage.From(ex, "composta_test@localhost"));
            Assert.Equal(11, Db.Count("SELECT COUNT(*) FROM waste_items"));
        }

        [SkippableFact]
        public void UpdatingMissingBatch_StillReportsNotFound()
        {
            RequireDatabase();

            Composta.Models.CompostBatch batch = new Composta.Models.CompostBatch(DatabaseFixture.DemoUserId, DateTime.Now, 60);
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => new CompostBatchRepository(Db.Connstring).Update(batch));

            Assert.Equal("Gagal memperbarui batch di database.", DatabaseErrorMessage.From(ex, "composta_test@localhost"));
        }

        private const string UniqueViolationState = "23505";

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
