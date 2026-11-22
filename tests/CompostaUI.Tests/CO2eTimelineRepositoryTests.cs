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

            
            CompostableWaste item1 = new CompostableWaste("Sisa Dapur", 2.0, null, BrownGreenType.Green, 60);
            item1.DateAdded = new DateTime(2026, 11, 1, 9, 0, 0);
            repo.AddWasteItem(batch, item1);

            
            CompostableWaste item2 = new CompostableWaste("Daun", 1.0, leavesCategory, BrownGreenType.Brown, 30);
            item2.DateAdded = new DateTime(2026, 11, 1, 15, 0, 0);
            repo.AddWasteItem(batch, item2);

            
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

        [SkippableFact]
        public void GetCO2eTimeline_QuarterKgWithoutCategory_UsesBankersRoundingConsistentWithCalculator()
        {
            RequireDatabase();

            UserRepository userRepo = new UserRepository(Db.Connstring);
            User user = new User("Rounding Tester", "rounding@composta.id");
            userRepo.Add(user);

            CompostBatchRepository repo = new CompostBatchRepository(Db.Connstring);
            CarbonCalculatorService calculator = new CarbonCalculatorService();

            CompostBatch batch = new CompostBatch(user.Id, new DateTime(2026, 11, 5), 30);
            repo.Add(batch);

            CompostableWaste item = new CompostableWaste("Sisa Buah", 0.25, null, BrownGreenType.Green, 60);
            item.DateAdded = new DateTime(2026, 11, 5, 10, 0, 0);
            repo.AddWasteItem(batch, item);

            List<CO2eTimelinePoint> timeline = repo.GetCO2eTimeline(user.Id);
            double expected = calculator.CalculateCO2eSaved(new List<WasteItem> { item });

            Assert.Single(timeline);
            Assert.Equal(0.12, expected);
            Assert.Equal(expected, timeline[0].DailyCO2e);
            Assert.Equal(expected, timeline[0].CumulativeCO2e);
        }
    }
}
