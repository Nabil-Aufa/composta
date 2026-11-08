using Composta.Data;
using Composta.Enums;
using Composta.Interfaces;
using Composta.Models;
using Composta.Services;
using Npgsql;

namespace Composta
{
    public static class AppData
    {
        public const string Location = "Yogyakarta";

        private const string DefaultConnstring =
            "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=composta";

        static AppData()
        {
            Connstring = Environment.GetEnvironmentVariable("COMPOSTA_DB") ?? DefaultConnstring;

            UserRepository = new UserRepository(Connstring);
            CategoryRepository = new WasteCategoryRepository(Connstring);
            BatchRepository = new CompostBatchRepository(Connstring);

            WeatherService = new OfflineWeatherService();
            CarbonCalculator = new CarbonCalculatorService();
            Advisor = new CompostAdvisorService(WeatherService);

            CurrentUser = new User();
            Categories = new List<WasteCategory>();
        }

        public static string Connstring { get; }

        public static UserRepository UserRepository { get; }
        public static WasteCategoryRepository CategoryRepository { get; }
        public static CompostBatchRepository BatchRepository { get; }

        public static IWeatherService WeatherService { get; }
        public static ICarbonCalculator CarbonCalculator { get; }
        public static CompostAdvisorService Advisor { get; }

        public static User CurrentUser { get; private set; }
        public static List<WasteCategory> Categories { get; private set; }

        public static string DatabaseInfo
        {
            get
            {
                NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(Connstring);
                return builder.Database + "@" + builder.Host + ":" + builder.Port;
            }
        }

        public static void Load()
        {
            User user = UserRepository.GetAll().FirstOrDefault()
                ?? throw new InvalidOperationException("Tabel users masih kosong.");

            foreach (CompostBatch batch in BatchRepository.GetByUser(user.Id))
            {
                BatchStatus oldStatus = batch.Status;
                batch.UpdateProgress();
                if (batch.Status != oldStatus)
                {
                    BatchRepository.Update(batch);
                }

                user.AddBatch(batch);
            }

            Categories = CategoryRepository.GetAll();
            CurrentUser = user;
        }

        public static string GetBatchName(CompostBatch batch)
        {
            return "Batch " + (CurrentUser.Batches.IndexOf(batch) + 1);
        }

        public static string GetStatusText(BatchStatus status)
        {
            switch (status)
            {
                case BatchStatus.Active:
                    return "Aktif";
                case BatchStatus.Curing:
                    return "Pematangan";
                case BatchStatus.Ready:
                    return "Siap Panen";
                default:
                    return "Sudah Dipanen";
            }
        }
    }
}
