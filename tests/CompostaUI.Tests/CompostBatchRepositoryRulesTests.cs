using Composta.Data;
using Composta.Enums;
using Composta.Models;

namespace Composta.UITests
{
    public class CompostBatchRepositoryRulesTests
    {
        private readonly CompostBatchRepository _repo =
            new CompostBatchRepository("Host=127.0.0.1;Port=1;Database=tidak_dipakai");

        [Theory]
        [InlineData(BatchStatus.Curing)]
        [InlineData(BatchStatus.Ready)]
        [InlineData(BatchStatus.Archived)]
        public void AddWasteItem_RejectsBatchThatIsNotActive(BatchStatus status)
        {
            CompostBatch batch = new CompostBatch("user-1", DateTime.Now.AddDays(-40), 60) { Status = status };
            CompostableWaste item = new CompostableWaste("Daun", 1, null, BrownGreenType.Brown, 50);

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => _repo.AddWasteItem(batch, item));

            Assert.Contains("Aktif", ex.Message);
            Assert.Empty(batch.WasteItems);
        }

        [Theory]
        [InlineData(BatchStatus.Active)]
        [InlineData(BatchStatus.Curing)]
        [InlineData(BatchStatus.Archived)]
        public void Harvest_RejectsBatchThatIsNotReady(BatchStatus status)
        {
            CompostBatch batch = new CompostBatch("user-1", DateTime.Now.AddDays(-70), 60) { Status = status };

            Assert.Throws<InvalidOperationException>(() => _repo.Harvest(batch));
            Assert.Equal(status, batch.Status);
        }
    }
}
