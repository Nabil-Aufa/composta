using Composta.Models;

namespace Composta.UITests
{
    public class BadgeRepositoryTests : DatabaseTest
    {
        public BadgeRepositoryTests(DatabaseFixture db) : base(db)
        {
        }

        [SkippableFact]
        public void GetBadgesByUser_ReturnsSeededBadgesWithStatus()
        {
            RequireDatabase();

            List<UserBadgeStatus> statuses = AppData.BadgeRepository.GetBadgesByUser(DatabaseFixture.DemoUserId);

            Assert.True(statuses.Count >= 4);
            Assert.Contains(statuses, s => s.Badge.Criteria == "BATCH:1");
            Assert.Contains(statuses, s => s.Badge.Criteria == "WEIGHT:10");
            Assert.Contains(statuses, s => s.Badge.Criteria == "CO2E:5");
            Assert.Contains(statuses, s => s.Badge.Criteria == "HARVEST:1");
        }

        [SkippableFact]
        public void AwardBadge_IsIdempotentAndDoesNotDuplicate()
        {
            RequireDatabase();

            UserBadgeStatus target = AppData.BadgeRepository
                .GetBadgesByUser(DatabaseFixture.DemoUserId)
                .First(s => !s.IsEarned);

            UserBadge award = new UserBadge(DatabaseFixture.DemoUserId, target.Badge.Id);
            Assert.True(AppData.BadgeRepository.AwardBadge(award));
            Assert.False(AppData.BadgeRepository.AwardBadge(award));

            long count = Db.Count(
                $"SELECT COUNT(*) FROM user_badges WHERE user_id = '{DatabaseFixture.DemoUserId}' AND badge_id = '{target.Badge.Id}'");
            Assert.Equal(1, count);
        }

        [SkippableFact]
        public void EvaluateAndAwardBadges_UnlocksEligibleBadgesOnce()
        {
            RequireDatabase();

            AppData.Login(DatabaseFixture.DemoUserId);
            List<Badge> firstPass = AppData.CheckAndAwardBadges();

            Assert.Contains(firstPass, b => b.Criteria == "BATCH:1");
            Assert.Empty(AppData.CheckAndAwardBadges());
        }
    }
}
