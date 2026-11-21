using Composta.Enums;
using Composta.Models;
using Composta.ViewModels;

namespace Composta.UITests
{
    public class BatchItemViewModelTests
    {
        private static readonly DateTime Start = new DateTime(2026, 11, 1);

        private static CompostBatch CreateBatch(BatchStatus status = BatchStatus.Active)
        {
            return new CompostBatch
            {
                StartDate = Start,
                EstimatedMaturityDate = Start.AddDays(30),
                Status = status
            };
        }

        [Theory]
        [InlineData(0, "Hari ke-1 dari 30")]
        [InlineData(9, "Hari ke-10 dari 30")]
        [InlineData(29, "Hari ke-30 dari 30")]
        public void DayTextCountsFromStartDate(int daysSinceStart, string expected)
        {
            BatchItemViewModel vm = new BatchItemViewModel(CreateBatch(), Start.AddDays(daysSinceStart));

            Assert.Equal(expected, vm.DayText);
        }

        [Fact]
        public void DayTextIsClampedToLastDayAfterMaturity()
        {
            BatchItemViewModel vm = new BatchItemViewModel(CreateBatch(), Start.AddDays(45));

            Assert.Equal("Hari ke-30 dari 30", vm.DayText);
        }

        [Fact]
        public void DayTextIsClampedToFirstDayBeforeStart()
        {
            BatchItemViewModel vm = new BatchItemViewModel(CreateBatch(), Start.AddDays(-3));

            Assert.Equal("Hari ke-1 dari 30", vm.DayText);
        }

        [Fact]
        public void DayTextUsesAtLeastOneDayWhenMaturityNotAfterStart()
        {
            CompostBatch batch = CreateBatch();
            batch.EstimatedMaturityDate = Start;

            BatchItemViewModel vm = new BatchItemViewModel(batch, Start.AddDays(5));

            Assert.Equal("Hari ke-1 dari 1", vm.DayText);
        }

        [Theory]
        [InlineData(BatchStatus.Active, false, false)]
        [InlineData(BatchStatus.Curing, true, false)]
        [InlineData(BatchStatus.Ready, true, true)]
        [InlineData(BatchStatus.Archived, true, true)]
        public void StageFlagsFollowStatus(BatchStatus status, bool curingOrLater, bool readyOrLater)
        {
            BatchItemViewModel vm = new BatchItemViewModel(CreateBatch(status), Start);

            Assert.Equal(curingOrLater, vm.IsCuringOrLater);
            Assert.Equal(readyOrLater, vm.IsReadyOrLater);
        }
    }
}
