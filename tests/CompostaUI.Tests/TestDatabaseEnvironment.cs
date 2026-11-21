using System.Runtime.CompilerServices;
using Npgsql;

namespace Composta.UITests
{
    public static class TestDatabaseEnvironment
    {
        private const string DefaultTestConnstring =
            "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=composta_test";

        private const string DisabledDatabaseName = "composta_disabled_test";

        public static string Connstring { get; } =
            Environment.GetEnvironmentVariable("COMPOSTA_TEST_DB") ?? DefaultTestConnstring;

        public static string DatabaseName { get; } =
            new NpgsqlConnectionStringBuilder(Connstring).Database ?? string.Empty;

        public static bool IsTestDatabase { get; } =
            DatabaseName.EndsWith("_test", StringComparison.OrdinalIgnoreCase);

        [ModuleInitializer]
        internal static void PointApplicationToTestDatabase()
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(Connstring);
            if (!IsTestDatabase)
            {
                builder.Database = DisabledDatabaseName;
            }
            Environment.SetEnvironmentVariable("COMPOSTA_DB", builder.ConnectionString);
        }
    }
}
