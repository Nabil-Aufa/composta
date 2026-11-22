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
            ScheduleRepository = new MaintenanceScheduleRepository(Connstring);

            string? weatherKey = Environment.GetEnvironmentVariable("COMPOSTA_WEATHER_KEY");
            WeatherService = CreateWeatherService(weatherKey);
            CarbonCalculator = new CarbonCalculatorService();
            Advisor = new CompostAdvisorService(WeatherService);

            CurrentUser = new User();
            IsLoggedIn = false;
            Categories = new List<WasteCategory>();
        }

        public static string Connstring { get; }

        public static UserRepository UserRepository { get; }
        public static WasteCategoryRepository CategoryRepository { get; }
        public static CompostBatchRepository BatchRepository { get; }
        public static MaintenanceScheduleRepository ScheduleRepository { get; }

        public static IWeatherService WeatherService { get; }
        public static ICarbonCalculator CarbonCalculator { get; }
        public static CompostAdvisorService Advisor { get; }

        public static User CurrentUser { get; private set; }
        public static bool IsLoggedIn { get; private set; }
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
            if (!IsLoggedIn)
            {
                throw new InvalidOperationException("Belum ada profil yang masuk. Pilih profil terlebih dahulu.");
            }

            Login(CurrentUser.Id);
        }

        public static List<User> GetUsers()
        {
            return UserRepository.GetAll();
        }

        public static User RegisterUser(string name, string email)
        {
            string? error = UserValidator.Validate(name, email);
            if (error != null)
            {
                throw new ArgumentException(error);
            }

            string normalizedEmail = UserValidator.NormalizeEmail(email);
            if (UserRepository.GetByEmail(normalizedEmail) != null)
            {
                throw new InvalidOperationException("Email " + normalizedEmail + " sudah terdaftar.");
            }

            User user = new User(name.Trim(), normalizedEmail);
            UserRepository.Add(user);
            return user;
        }

        public static void Login(string userId)
        {
            User user = UserRepository.GetById(userId)
                ?? throw new InvalidOperationException("User tidak ditemukan.");

            foreach (CompostBatch batch in BatchRepository.GetByUser(user.Id))
            {
                BatchStatus oldStatus = batch.Status;
                batch.UpdateProgress();
                if (batch.Status != oldStatus)
                {
                    BatchRepository.Update(batch, user.Id);
                }

                batch.Schedules = ScheduleRepository.GetByBatch(batch.Id);

                user.AddBatch(batch);
            }

            Categories = CategoryRepository.GetAll();
            CurrentUser = user;
            IsLoggedIn = true;
        }

        public static void Logout()
        {
            CurrentUser = new User();
            IsLoggedIn = false;
        }

        public static IWeatherService CreateWeatherService(string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new OfflineWeatherService();
            }
            return new OpenWeatherMapService(apiKey.Trim());
        }

        public static void CreateBatchWithDefaultSchedules(CompostBatch batch)
        {
            ArgumentNullException.ThrowIfNull(batch);
            batch.GenerateDefaultSchedules();
            BatchRepository.Add(batch);
        }

        public static List<MaintenanceSchedule> CompleteSchedule(CompostBatch batch, MaintenanceSchedule schedule)
        {
            return BatchRepository.CompleteScheduleWithEarlier(batch, schedule);
        }

        public static List<MaintenanceSchedule> GetDueOrOverdueSchedules()
        {
            return ScheduleRepository.GetDueOrOverdueByUser(CurrentUser.Batches);
        }

        public static bool SaveRecommendationIfChanged(CompostRecommendation recommendation)
        {
            ArgumentNullException.ThrowIfNull(recommendation);

            CompostRecommendation? latest = ScheduleRepository.GetRecommendationsByBatch(recommendation.BatchId).FirstOrDefault();
            if (latest != null && latest.Message == recommendation.Message && latest.Priority == recommendation.Priority)
            {
                return false;
            }

            ScheduleRepository.AddRecommendation(recommendation);
            return true;
        }

        public static List<CompostRecommendation> GetBatchRecommendations(string batchId)
        {
            return ScheduleRepository.GetRecommendationsByBatch(batchId);
        }

        public static List<CO2eTimelinePoint> GetCO2eTimeline()
        {
            return BatchRepository.GetCO2eTimeline(CurrentUser.Id);
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
