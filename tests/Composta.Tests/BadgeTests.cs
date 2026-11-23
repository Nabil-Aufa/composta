using Composta.Enums;
using Composta.Models;

namespace Composta.Tests
{
    public class BadgeTests
    {
        [Fact]
        public void BatchCriteria_UnlocksWhenUserHasEnoughBatches()
        {
            User user = new User("Nabil", "nabil@example.com");
            Badge badge = new Badge("Batch Pertama", "BATCH:1", "badge_first_batch.png");

            Assert.False(badge.IsUnlocked(user));

            user.AddBatch(new CompostBatch(user.Id, DateTime.Today, 30));
            Assert.True(badge.IsUnlocked(user));
        }

        [Fact]
        public void HarvestCriteria_OnlyCountsArchivedBatches()
        {
            User user = new User("Nabil", "nabil@example.com");
            Badge badge = new Badge("Panen Pertama", "HARVEST:1", "badge_first_harvest.png");

            CompostBatch readyBatch = new CompostBatch(user.Id, DateTime.Today.AddDays(-30), 30);
            readyBatch.Status = BatchStatus.Ready;
            user.AddBatch(readyBatch);

            Assert.False(badge.IsUnlocked(user));

            readyBatch.Status = BatchStatus.Archived;
            Assert.True(badge.IsUnlocked(user));
        }

        [Fact]
        public void WeightCriteria_OnlyCountsCompostableWaste()
        {
            User user = new User("Nabil", "nabil@example.com");
            Badge badge = new Badge("10 kg Sampah", "WEIGHT:10", "badge_10kg_waste.png");

            CompostBatch batch = new CompostBatch(user.Id, DateTime.Today, 30);
            batch.AddWasteItem(new NonCompostableWaste("Botol plastik", 15, TestData.Plastic, "Plastik"));
            batch.AddWasteItem(TestData.Green(6));
            user.AddBatch(batch);

            Assert.Equal(6, user.GetTotalCompostableWeight(), 2);
            Assert.False(badge.IsUnlocked(user));

            batch.AddWasteItem(TestData.Brown(4));
            Assert.Equal(10, user.GetTotalCompostableWeight(), 2);
            Assert.True(badge.IsUnlocked(user));
        }

        [Fact]
        public void Co2eCriteria_UsesCarbonCalculatorService()
        {
            User user = new User("Nabil", "nabil@example.com");
            Badge badge = new Badge("5 kg CO2e Dicegah", "CO2E:5", "badge_5kg_co2e.png");

            CompostBatch batch = new CompostBatch(user.Id, DateTime.Today, 30);
            batch.AddWasteItem(TestData.Green(8));
            user.AddBatch(batch);

            Assert.False(badge.IsUnlocked(user));

            batch.AddWasteItem(TestData.Green(2));
            Assert.True(badge.IsUnlocked(user));
        }

        [Fact]
        public void InvalidOrNullInputs_ReturnFalse()
        {
            User user = new User("Nabil", "nabil@example.com");
            user.AddBatch(new CompostBatch(user.Id, DateTime.Today, 30));

            Assert.False(new Badge("Kosong", "", "").IsUnlocked(user));
            Assert.False(new Badge("Format Salah", "INVALID", "").IsUnlocked(user));
            Assert.False(new Badge("Tipe Asing", "UNKNOWN:1", "").IsUnlocked(user));
            Assert.False(new Badge("Null User", "BATCH:1", "").IsUnlocked(null!));
        }

        [Fact]
        public void UserBadgeStatus_ReflectsEarnedState()
        {
            Badge badge = new Badge("Batch Pertama", "BATCH:1", "badge_first_batch.png");

            UserBadgeStatus locked = new UserBadgeStatus(badge, null);
            Assert.False(locked.IsEarned);
            Assert.Null(locked.EarnedAt);

            DateTime earnedTime = new DateTime(2026, 11, 22, 10, 0, 0);
            UserBadgeStatus unlocked = new UserBadgeStatus(badge, earnedTime);
            Assert.True(unlocked.IsEarned);
            Assert.Equal(earnedTime, unlocked.EarnedAt);
        }
    }
}
