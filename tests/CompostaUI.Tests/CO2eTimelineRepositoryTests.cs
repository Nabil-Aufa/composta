using Composta.Data;
using Composta.Enums;
using Composta.Models;
using Composta.Services;

namespace Composta.UITests
{
    public class CO2eTimelineRepositoryTests : DatabaseTest
    {
        public CO2eTimelineRepositoryTests(DatabaseFixture db) : base(db)
        {
        }

        [SkippableFact]
        public void GetCO2eTimeline_CalculatesDailyAndCumulativeConsistentWithCarbonCalculator()
        {
            RequireDatabase();

            CompostBatchRepository repo = new CompostBatchRepository(Db.Connstring);
            CarbonCalculatorService calculator = new CarbonCalculatorService();

            List<CO2eTimelinePoint> timeline = repo.GetCO2eTimeline(DatabaseFixture.NabilId);
            List<WasteItem> allUserItems = repo.GetByUser(DatabaseFixture.NabilId)
                .SelectMany(b => b.WasteItems)
                .ToList();

            Assert.NotEmpty(timeline);

            for (int i = 1; i < timeline.Count; i++)
            {
                Assert.True(timeline[i].Date > timeline[i - 1].Date);
            }

            double expectedTotal = calculator.CalculateCO2eSaved(allUserItems);
            Assert.Equal(expectedTotal, timeline.Last().CumulativeCO2e);

            foreach (CO2eTimelinePoint point in timeline)
            {
                List<WasteItem> itemsOnDate = allUserItems
                    .Where(w => w.DateAdded.Date == point.Date.Date)
                    .ToList();
                Assert.Equal(calculator.CalculateCO2eSaved(itemsOnDate), point.DailyCO2e);
            }
        }

        [SkippableFact]
        public void GetCO2eTimeline_AggregatesSameDayAndUsesDefaultFactorWhenCategoryNull()
        {
            RequireDatabase();

            UserRepository userRepo = new UserRepository(Db.Connstring);
            User user = new User("Timeline Tester", "timeline@composta.id");
            userRepo.Add(user);

            CompostBatchRepository repo = new CompostBatchRepository(Db.Connstring);
            WasteCategoryRepository catRepo = new WasteCategoryRepository(Db.Connstring);
            WasteCategory leavesCategory = catRepo.GetAll().First(c => c.Name == "Daun Kering & Ranting");

            CompostBatch batch = new CompostBatch(user.Id, new DateTime(2026, 11, 1), 30);
            repo.Add(batch);

            // Hari 1: item tanpa kategori (fallback default factor = 0.5) -> 2.0 * 0.5 = 1.00
            CompostableWaste item1 = new CompostableWaste("Sisa Dapur", 2.0, null, BrownGreenType.Green, 60);
            item1.DateAdded = new DateTime(2026, 11, 1, 9, 0, 0);
            repo.AddWasteItem(batch, item1);

            // Hari 1 (jam berbeda): Daun Kering (factor = 0.3) -> 1.0 * 0.3 = 0.30 -> total hari 1 = 1.30
            CompostableWaste item2 = new CompostableWaste("Daun", 1.0, leavesCategory, BrownGreenType.Brown, 30);
            item2.DateAdded = new DateTime(2026, 11, 1, 15, 0, 0);
            repo.AddWasteItem(batch, item2);

            // Hari 2: Daun Kering (factor = 0.3) -> 4.0 * 0.3 = 1.20 -> akumulatif = 2.50
            CompostableWaste item3 = new CompostableWaste("Ranting", 4.0, leavesCategory, BrownGreenType.Brown, 30);
            item3.DateAdded = new DateTime(2026, 11, 2, 10, 0, 0);
            repo.AddWasteItem(batch, item3);

            List<CO2eTimelinePoint> timeline = repo.GetCO2eTimeline(user.Id);

            Assert.Equal(2, timeline.Count);
            Assert.Equal(new DateTime(2026, 11, 1), timeline[0].Date);
            Assert.Equal(1.30, timeline[0].DailyCO2e);
            Assert.Equal(1.30, timeline[0].CumulativeCO2e);

            Assert.Equal(new DateTime(2026, 11, 2), timeline[1].Date);
            Assert.Equal(1.20, timeline[1].DailyCO2e);
            Assert.Equal(2.50, timeline[1].CumulativeCO2e);
        }
    }
}