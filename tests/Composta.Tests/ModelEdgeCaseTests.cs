using Composta.Enums;
using Composta.Models;

namespace Composta.Tests
{
    public class ModelEdgeCaseTests
    {
        [Fact]
        public void IsOverdue_DueEarlierToday_IsNotOverdue()
        {
            MaintenanceSchedule schedule = new MaintenanceSchedule(TaskType.Turn, DateTime.Today.AddMinutes(1));

            Assert.False(schedule.IsOverdue());
        }

        [Fact]
        public void IsOverdue_DueYesterday_IsOverdue()
        {
            MaintenanceSchedule schedule = new MaintenanceSchedule(TaskType.Turn, DateTime.Today.AddDays(-1).AddHours(23));

            Assert.True(schedule.IsOverdue());
        }

        [Theory]
        [InlineData("Drizzle")]
        [InlineData("Thunderstorm")]
        [InlineData("Gerimis")]
        [InlineData("Badai petir")]
        public void IsRainy_DrizzleAndThunderstorm_CountAsRain(string condition)
        {
            WeatherData weather = new WeatherData(28, 80, 0, condition, DateTime.Now);

            Assert.True(weather.IsRainy());
        }
    }
}
