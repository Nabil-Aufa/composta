using Composta.Data;
using Composta.Enums;
using Composta.Models;

namespace Composta.UITests
{
    public class MaintenanceScheduleRepositoryTests : DatabaseTest
    {
        public MaintenanceScheduleRepositoryTests(DatabaseFixture db) : base(db)
        {
        }

        [SkippableFact]
        public void CreateBatchWithDefaultSchedules_PersistsWithoutDuplicateKey()
        {
            RequireDatabase();

            CompostBatch batch = new CompostBatch(DatabaseFixture.NabilId, DateTime.Now, 14);
            AppData.CreateBatchWithDefaultSchedules(batch);

            MaintenanceScheduleRepository repo = new MaintenanceScheduleRepository(Db.Connstring);
            List<MaintenanceSchedule> saved = repo.GetByBatch(batch.Id);

            Assert.Equal(batch.Schedules.Count, saved.Count);
            Assert.Contains(saved, s => s.TaskType == TaskType.Harvest);
        }

        [SkippableFact]
        public void InsertCompleteAndDelete_WorksAgainstPostgres()
        {
            RequireDatabase();

            CompostBatch batch = new CompostBatch(DatabaseFixture.NabilId, DateTime.Now, 30);
            AppData.BatchRepository.Add(batch);

            MaintenanceScheduleRepository repo = new MaintenanceScheduleRepository(Db.Connstring);
            MaintenanceSchedule schedule = new MaintenanceSchedule(TaskType.Water, DateTime.Now.AddDays(1));
            schedule.BatchId = batch.Id;

            repo.Add(schedule);
            MaintenanceSchedule? fetched = repo.GetById(schedule.Id);
            Assert.NotNull(fetched);
            Assert.False(fetched.IsCompleted);

            repo.CompleteSchedule(schedule);
            MaintenanceSchedule? completed = repo.GetById(schedule.Id);
            Assert.NotNull(completed);
            Assert.True(completed.IsCompleted);

            repo.Delete(schedule.Id);
            Assert.Null(repo.GetById(schedule.Id));
        }

        [SkippableFact]
        public void Delete_NonExistentSchedule_ThrowsInvalidOperationException()
        {
            RequireDatabase();

            MaintenanceScheduleRepository repo = new MaintenanceScheduleRepository(Db.Connstring);
            Assert.Throws<InvalidOperationException>(() => repo.Delete("id-tidak-ada"));
        }
    }
}