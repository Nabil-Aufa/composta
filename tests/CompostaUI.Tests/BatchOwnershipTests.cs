using Composta.Data;
using Composta.Models;

namespace Composta.UITests
{
    public class BatchOwnershipTests : DatabaseTest
    {
        public BatchOwnershipTests(DatabaseFixture db) : base(db)
        {
        }

        private CompostBatchRepository Repository => new CompostBatchRepository(Db.Connstring);

        private CompostBatch DemoBatch()
        {
            return Repository.GetByUser(DatabaseFixture.DemoUserId).First();
        }

        private CompostBatch WithOwner(CompostBatch batch, string userId)
        {
            return new CompostBatch
            {
                Id = batch.Id,
                UserId = userId,
                StartDate = batch.StartDate,
                EstimatedMaturityDate = batch.EstimatedMaturityDate,
                Status = batch.Status
            };
        }

        [SkippableFact]
        public void Delete_ByOtherUser_IsRejected()
        {
            RequireDatabase();
            CompostBatch batch = DemoBatch();

            Assert.Throws<InvalidOperationException>(() => Repository.Delete(WithOwner(batch, DatabaseFixture.NabilId)));
            Assert.Equal(1, Db.Count("SELECT COUNT(*) FROM compost_batches WHERE id = '" + batch.Id + "'"));
        }

        [SkippableFact]
        public void Delete_ByOwner_RemovesBatch()
        {
            RequireDatabase();
            CompostBatch batch = DemoBatch();

            Repository.Delete(batch);

            Assert.Equal(0, Db.Count("SELECT COUNT(*) FROM compost_batches WHERE id = '" + batch.Id + "'"));
        }

        [SkippableFact]
        public void Update_ByOtherUser_IsRejected()
        {
            RequireDatabase();
            CompostBatch batch = DemoBatch();
            CompostBatch foreign = WithOwner(batch, DatabaseFixture.NabilId);
            foreign.EstimatedMaturityDate = batch.EstimatedMaturityDate.AddDays(10);

            Assert.Throws<InvalidOperationException>(() => Repository.Update(foreign));
            CompostBatch reloaded = Repository.GetByUser(DatabaseFixture.DemoUserId).Single(b => b.Id == batch.Id);
            Assert.Equal(batch.EstimatedMaturityDate, reloaded.EstimatedMaturityDate);
        }

        [Fact]
        public void DeleteById_IsNotSupported()
        {
            CompostBatchRepository repository = new CompostBatchRepository("Host=127.0.0.1;Port=1;Database=tidak_dipakai");

            Assert.Throws<NotSupportedException>(() => repository.Delete("batch-1"));
        }
    }
}
