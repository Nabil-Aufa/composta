using Composta.Data;
using Composta.Enums;
using Composta.Models;

namespace Composta.UITests
{
    public class BatchOwnershipTests : DatabaseTest
    {
        public BatchOwnershipTests(DatabaseFixture db) : base(db)
        {
        }

        private CompostBatchRepository Repository => new CompostBatchRepository(Db.Connstring);

        private CompostBatch DemoBatch(BatchStatus? status = null)
        {
            return Repository.GetByUser(DatabaseFixture.DemoUserId)
                .First(b => status == null || b.Status == status);
        }

        private long BatchCount(string batchId)
        {
            return Db.Count("SELECT COUNT(*) FROM compost_batches WHERE id = '" + batchId + "'");
        }

        [SkippableFact]
        public void Delete_ByOtherLoggedInUser_IsRejected()
        {
            RequireDatabase();
            CompostBatch batch = DemoBatch();

            Assert.Throws<InvalidOperationException>(() => Repository.Delete(batch, DatabaseFixture.NabilId));
            Assert.Equal(1, BatchCount(batch.Id));
        }

        [SkippableFact]
        public void Delete_ByOwner_RemovesBatch()
        {
            RequireDatabase();
            CompostBatch batch = DemoBatch();

            Repository.Delete(batch, DatabaseFixture.DemoUserId);

            Assert.Equal(0, BatchCount(batch.Id));
        }

        [SkippableFact]
        public void DeleteById_RemovesBatch()
        {
            RequireDatabase();
            CompostBatch batch = DemoBatch();

            Repository.Delete(batch.Id);

            Assert.Equal(0, BatchCount(batch.Id));
            Assert.Throws<InvalidOperationException>(() => Repository.Delete(batch.Id));
        }

        [SkippableFact]
        public void Update_ByOtherLoggedInUser_IsRejected()
        {
            RequireDatabase();
            CompostBatch batch = DemoBatch();
            DateTime original = batch.EstimatedMaturityDate;
            batch.EstimatedMaturityDate = original.AddDays(10);

            Assert.Throws<InvalidOperationException>(() => Repository.Update(batch, DatabaseFixture.NabilId));
            CompostBatch reloaded = Repository.GetByUser(DatabaseFixture.DemoUserId).Single(b => b.Id == batch.Id);
            Assert.Equal(original, reloaded.EstimatedMaturityDate);
        }

        [SkippableFact]
        public void AddWasteItem_ByOtherLoggedInUser_IsRejectedAndRolledBack()
        {
            RequireDatabase();
            CompostBatch batch = DemoBatch(BatchStatus.Active);
            long itemsBefore = Db.Count("SELECT COUNT(*) FROM waste_items WHERE batch_id = '" + batch.Id + "'");
            CompostableWaste item = new CompostableWaste("Sisa sayur", 1, null, BrownGreenType.Green, 60);

            Assert.Throws<InvalidOperationException>(() => Repository.AddWasteItem(batch, item, DatabaseFixture.NabilId));
            Assert.Equal(itemsBefore, Db.Count("SELECT COUNT(*) FROM waste_items WHERE batch_id = '" + batch.Id + "'"));
            Assert.DoesNotContain(item, batch.WasteItems);
        }
    }
}
