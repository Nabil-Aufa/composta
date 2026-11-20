using Composta.Enums;
using Composta.Models;

namespace Composta.Tests
{
    public class CompostBatchTests
    {
        [Fact]
        public void CalculateRatio_DividesBrownByGreen()
        {
            Assert.Equal(2, TestData.BatchWithRatio(6, 3).CalculateRatio());
            Assert.Equal(0.6, TestData.BatchWithRatio(3, 5).CalculateRatio());
        }

        [Fact]
        public void CalculateRatio_WithoutGreen_ReturnsBrownWeight()
        {
            Assert.Equal(4, TestData.BatchWithRatio(4, 0).CalculateRatio());
            Assert.Equal(0, TestData.NewBatch().CalculateRatio());
        }

        [Fact]
        public void CalculateRatio_IgnoresNonCompostableWaste()
        {
            CompostBatch batch = TestData.BatchWithRatio(4, 2);
            batch.AddWasteItem(new NonCompostableWaste("Kaleng", 10, TestData.Plastic, "Logam"));

            Assert.Equal(2, batch.CalculateRatio());
        }

        [Fact]
        public void AddWasteItem_UpdatesStoredRatio()
        {
            CompostBatch batch = TestData.NewBatch();
            batch.AddWasteItem(TestData.Green(2));
            batch.AddWasteItem(TestData.Brown(5));

            Assert.Equal(2.5, batch.BrownGreenRatio);
        }

        [Fact]
        public void UpdateProgress_BeforeHalfway_IsActive()
        {
            CompostBatch batch = TestData.NewBatch(startedDaysAgo: 10, maturityDays: 60);

            batch.UpdateProgress();

            Assert.Equal(BatchStatus.Active, batch.Status);
        }

        [Fact]
        public void UpdateProgress_AfterHalfway_IsCuring()
        {
            CompostBatch batch = TestData.NewBatch(startedDaysAgo: 40, maturityDays: 60);

            batch.UpdateProgress();

            Assert.Equal(BatchStatus.Curing, batch.Status);
        }

        [Fact]
        public void UpdateProgress_AfterMaturityDate_IsReady()
        {
            CompostBatch batch = TestData.NewBatch(startedDaysAgo: 61, maturityDays: 60);

            batch.UpdateProgress();

            Assert.Equal(BatchStatus.Ready, batch.Status);
        }

        [Fact]
        public void UpdateProgress_DoesNotChangeArchivedBatch()
        {
            CompostBatch batch = TestData.NewBatch(startedDaysAgo: 5, maturityDays: 60);
            batch.Status = BatchStatus.Archived;

            batch.UpdateProgress();

            Assert.Equal(BatchStatus.Archived, batch.Status);
        }

        [Fact]
        public void IsReadyToHarvest_FollowsMaturityDateAndStatus()
        {
            Assert.False(TestData.NewBatch(startedDaysAgo: 10).IsReadyToHarvest());
            Assert.True(TestData.NewBatch(startedDaysAgo: 61).IsReadyToHarvest());

            CompostBatch archived = TestData.NewBatch(startedDaysAgo: 61);
            archived.Status = BatchStatus.Archived;
            Assert.False(archived.IsReadyToHarvest());

            CompostBatch markedReady = TestData.NewBatch(startedDaysAgo: 1);
            markedReady.Status = BatchStatus.Ready;
            Assert.True(markedReady.IsReadyToHarvest());
        }

        [Fact]
        public void GetTotalWeight_SumsAllItems()
        {
            CompostBatch batch = TestData.BatchWithRatio(2.25, 1.5);
            batch.AddWasteItem(new NonCompostableWaste("Botol", 1, TestData.Plastic, "Plastik"));

            Assert.Equal(4.75, batch.GetTotalWeight());
        }

        [Fact]
        public void AddSchedule_LinksScheduleToBatch()
        {
            CompostBatch batch = TestData.NewBatch();
            MaintenanceSchedule schedule = new MaintenanceSchedule(TaskType.Water, DateTime.Now.AddDays(1));

            batch.AddSchedule(schedule);

            Assert.Equal(batch.Id, schedule.BatchId);
            Assert.Single(batch.Schedules);
        }

        [Fact]
        public void NegativeWeight_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TestData.Green(-1));
        }

        [Fact]
        public void MoistureContent_IsClampedBetweenZeroAndHundred()
        {
            Assert.Equal(100, TestData.Green(1, moisture: 150).MoistureContent);
            Assert.Equal(0, TestData.Green(1, moisture: -10).MoistureContent);
        }
    }
}
