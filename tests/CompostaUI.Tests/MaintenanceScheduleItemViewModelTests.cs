using Composta.Enums;
using Composta.Models;
using Composta.ViewModels;

namespace Composta.UITests
{
    public class MaintenanceScheduleItemViewModelTests
    {
        [Theory]
        [InlineData(-3, true)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(30, false)]
        public void CanComplete_OnlyFromDueDate(int daysFromToday, bool expected)
        {
            MaintenanceScheduleItemViewModel item = new MaintenanceScheduleItemViewModel(
                new MaintenanceSchedule(TaskType.Water, DateTime.Today.AddDays(daysFromToday)));

            Assert.Equal(expected, item.CanComplete);
        }

        [Fact]
        public void CanComplete_IsFalseWhenAlreadyCompleted()
        {
            MaintenanceSchedule schedule = new MaintenanceSchedule(TaskType.Turn, DateTime.Today.AddDays(-1));
            schedule.MarkCompleted();

            Assert.False(new MaintenanceScheduleItemViewModel(schedule).CanComplete);
        }
    }
}
