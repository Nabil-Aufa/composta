using Composta.Enums;
using Composta.Models;
using Composta.Services;
using Composta.ViewModels;

namespace Composta.UITests
{
    public class BadgeItemViewModelTests
    {
        private static readonly WasteCategory Sayur = new WasteCategory("Sayur", 0.5, true);

        private static User UserWithCompost(double kg)
        {
            User user = new User("Nayla", "nayla@example.com");
            CompostBatch batch = new CompostBatch();
            batch.AddWasteItem(new CompostableWaste("Sayur", kg, Sayur, BrownGreenType.Green, 60));
            user.AddBatch(batch);
            return user;
        }

        [Fact]
        public void EarnedBadge_ShowsEarnedDateAndIsNotLocked()
        {
            Badge badge = new Badge("Batch Pertama", "BATCH:1", "");
            BadgeItemViewModel item = new BadgeItemViewModel(
                new UserBadgeStatus(badge, new DateTime(2026, 11, 22, 9, 0, 0)), UserWithCompost(1));

            Assert.True(item.IsUnlocked);
            Assert.False(item.IsLocked);
            Assert.Equal("Batch Pertama", item.Name);
            Assert.Equal("Buat 1 batch kompos.", item.DescriptionText);
            Assert.Equal("Diperoleh " + new DateTime(2026, 11, 22).ToString("dd MMM yyyy"), item.StatusText);
            Assert.Equal("🌱", item.Icon);
        }

        [Fact]
        public void LockedBadge_ShowsProgressTowardTarget()
        {
            Badge badge = new Badge("10 kg Sampah", "WEIGHT:10", "");
            BadgeItemViewModel item = new BadgeItemViewModel(new UserBadgeStatus(badge, null), UserWithCompost(6.5));

            Assert.True(item.IsLocked);
            Assert.Equal("Olah 10 kg sampah yang dapat dikompos.", item.DescriptionText);
            Assert.Equal("Belum diperoleh • " + 6.5.ToString("0.##") + " / 10 kg", item.StatusText);
        }

        [Fact]
        public void LockedBadge_ProgressNeverExceedsTarget()
        {
            Badge badge = new Badge("Panen Pertama", "HARVEST:1", "");
            User user = UserWithCompost(2);
            user.Batches[0].Status = BatchStatus.Archived;
            user.AddBatch(new CompostBatch { Status = BatchStatus.Archived });

            BadgeItemViewModel item = new BadgeItemViewModel(new UserBadgeStatus(badge, null), user);

            Assert.Equal("Belum diperoleh • 1 / 1 batch", item.StatusText);
        }

        [Fact]
        public void UnknownCriteria_DoesNotCrash()
        {
            Badge badge = new Badge("Misteri", "UNKNOWN:3", "");
            BadgeItemViewModel item = new BadgeItemViewModel(new UserBadgeStatus(badge, null), new User());

            Assert.Equal("🏅", item.Icon);
            Assert.Equal("Kriteria belum dikenali.", item.DescriptionText);
            Assert.Equal("Belum diperoleh", item.StatusText);
        }

        [Fact]
        public void Profile_ListsEarnedBadgesFirstWithSummary()
        {
            User user = UserWithCompost(3);
            List<UserBadgeStatus> badges = new List<UserBadgeStatus>
            {
                new UserBadgeStatus(new Badge("10 kg Sampah", "WEIGHT:10", ""), null),
                new UserBadgeStatus(new Badge("Batch Pertama", "BATCH:1", ""), DateTime.Today),
                new UserBadgeStatus(new Badge("Panen Pertama", "HARVEST:1", ""), null)
            };

            ProfileViewModel profile = new ProfileViewModel(user, new CarbonCalculatorService(), badges, null);

            Assert.Equal("Batch Pertama", profile.Badges[0].Name);
            Assert.Equal("1 dari 3 badge diperoleh", profile.BadgeSummaryText);
            Assert.False(profile.HasBadgeError);
        }

        [Fact]
        public void Profile_ShowsErrorWhenBadgesCouldNotBeLoaded()
        {
            ProfileViewModel profile = new ProfileViewModel(new User("Nayla", "nayla@example.com"),
                new CarbonCalculatorService(), Array.Empty<UserBadgeStatus>(), "Tidak dapat terhubung ke server PostgreSQL.");

            Assert.True(profile.HasBadgeError);
            Assert.Empty(profile.Badges);
            Assert.Equal("Koleksi badge gagal dimuat.", profile.BadgeSummaryText);
        }

        [Fact]
        public void Profile_WithoutBadgeData_HasEmptyCollection()
        {
            ProfileViewModel profile = new ProfileViewModel(new User("Nayla", "nayla@example.com"), new CarbonCalculatorService());

            Assert.Empty(profile.Badges);
            Assert.Equal("0 dari 0 badge diperoleh", profile.BadgeSummaryText);
        }
    }
}
