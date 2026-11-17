using Composta.Enums;
using Composta.Models;
using Composta.Services;

namespace Composta.Tests
{
    public class CompostAdvisorServiceTests
    {
        private static CompostRecommendation Recommend(CompostBatch batch, WeatherData weather)
        {
            CompostAdvisorService advisor = new CompostAdvisorService(new FakeWeatherService(weather));
            return advisor.GenerateRecommendation(batch, weather);
        }

        [Fact]
        public void IdealRatioAndNeutralWeather_GivesLowPriorityAndGoodCondition()
        {
            CompostRecommendation result = Recommend(TestData.BatchWithRatio(5, 2), TestData.NeutralWeather());

            Assert.Equal("Low", result.Priority);
            Assert.Contains("Kondisi kompos sudah baik", result.Message);
        }

        [Fact]
        public void LowRatio_AsksForMoreBrownMaterial()
        {
            CompostRecommendation result = Recommend(TestData.BatchWithRatio(1, 4), TestData.NeutralWeather());

            Assert.Contains("terlalu rendah", result.Message);
            Assert.Contains("bahan brown", result.Message);
            Assert.Equal("Medium", result.Priority);
        }

        [Fact]
        public void HighRatio_AsksForMoreGreenMaterial()
        {
            CompostRecommendation result = Recommend(TestData.BatchWithRatio(10, 2), TestData.NeutralWeather());

            Assert.Contains("terlalu tinggi", result.Message);
            Assert.Contains("bahan green", result.Message);
            Assert.Equal("Low", result.Priority);
        }

        [Fact]
        public void RainyWeather_AsksToCoverCompost()
        {
            WeatherData rain = new WeatherData(26, 80, 5, "Hujan Ringan", DateTime.Now);

            CompostRecommendation result = Recommend(TestData.BatchWithRatio(5, 2), rain);

            Assert.Contains("Sedang hujan", result.Message);
            Assert.Equal("Medium", result.Priority);
        }

        [Fact]
        public void HotAndDryWeather_AsksToWaterCompost()
        {
            WeatherData hotDry = new WeatherData(35, 40, 0, "Cerah", DateTime.Now);

            CompostRecommendation result = Recommend(TestData.BatchWithRatio(5, 2), hotDry);

            Assert.Contains("panas dan kering", result.Message);
        }

        [Fact]
        public void HumidWeather_AsksToTurnCompost()
        {
            WeatherData humid = new WeatherData(28, 90, 0, "Berawan", DateTime.Now);

            CompostRecommendation result = Recommend(TestData.BatchWithRatio(5, 2), humid);

            Assert.Contains("Kelembapan udara tinggi", result.Message);
        }

        [Fact]
        public void ColdWeather_AsksToKeepCompostWarm()
        {
            WeatherData cold = new WeatherData(10, 60, 0, "Cerah", DateTime.Now);

            CompostRecommendation result = Recommend(TestData.BatchWithRatio(5, 2), cold);

            Assert.Contains("Suhu rendah", result.Message);
        }

        [Fact]
        public void DryMaterial_AsksToAddWater()
        {
            CompostRecommendation result = Recommend(TestData.BatchWithRatio(5, 2, moisture: 20), TestData.NeutralWeather());

            Assert.Contains("terlalu kering", result.Message);
        }

        [Fact]
        public void DryMaterialDuringRain_DoesNotAskToAddWater()
        {
            WeatherData rain = new WeatherData(26, 80, 5, "Hujan", DateTime.Now);

            CompostRecommendation result = Recommend(TestData.BatchWithRatio(5, 2, moisture: 20), rain);

            Assert.DoesNotContain("terlalu kering", result.Message);
        }

        [Fact]
        public void OverdueSchedule_IsReported()
        {
            CompostBatch batch = TestData.BatchWithRatio(5, 2);
            batch.AddSchedule(new MaintenanceSchedule(TaskType.Turn, DateTime.Now.AddDays(-2)));

            CompostRecommendation result = Recommend(batch, TestData.NeutralWeather());

            Assert.Contains("1 jadwal perawatan yang terlewat", result.Message);
            Assert.Equal("Medium", result.Priority);
        }

        [Fact]
        public void CompletedSchedule_IsNotReportedAsOverdue()
        {
            CompostBatch batch = TestData.BatchWithRatio(5, 2);
            MaintenanceSchedule schedule = new MaintenanceSchedule(TaskType.Water, DateTime.Now.AddDays(-2));
            schedule.MarkCompleted();
            batch.AddSchedule(schedule);

            CompostRecommendation result = Recommend(batch, TestData.NeutralWeather());

            Assert.DoesNotContain("terlewat", result.Message);
        }

        [Fact]
        public void MatureBatch_IsReadyToHarvestWithHighPriority()
        {
            CompostBatch batch = TestData.NewBatch(startedDaysAgo: 70, maturityDays: 60);
            batch.AddWasteItem(TestData.Brown(5));

            CompostRecommendation result = Recommend(batch, TestData.NeutralWeather());

            Assert.Equal("High", result.Priority);
            Assert.Contains("siap dipanen", result.Message);
            Assert.Equal(BatchStatus.Ready, batch.Status);
        }

        [Fact]
        public void EmptyBatch_AsksToAddMaterial()
        {
            CompostRecommendation result = Recommend(TestData.NewBatch(), TestData.NeutralWeather());

            Assert.Contains("Batch masih kosong", result.Message);
            Assert.Equal("Low", result.Priority);
        }

        [Fact]
        public void SeveralProblems_GiveHighPriority()
        {
            WeatherData rain = new WeatherData(26, 80, 5, "Hujan", DateTime.Now);

            CompostRecommendation result = Recommend(TestData.BatchWithRatio(1, 4), rain);

            Assert.Equal("High", result.Priority);
            Assert.Contains("terlalu rendah", result.Message);
            Assert.Contains("Sedang hujan", result.Message);
        }

        [Fact]
        public void RecommendationByLocation_UsesWeatherService()
        {
            WeatherData cold = new WeatherData(10, 60, 0, "Cerah", DateTime.Now);
            CompostAdvisorService advisor = new CompostAdvisorService(new FakeWeatherService(cold));

            CompostRecommendation result = advisor.GenerateRecommendation(TestData.BatchWithRatio(5, 2), "Yogyakarta");

            Assert.Contains("Suhu rendah", result.Message);
        }

        [Fact]
        public void NullArguments_AreRejected()
        {
            CompostAdvisorService advisor = new CompostAdvisorService(new FakeWeatherService(TestData.NeutralWeather()));

            Assert.Throws<ArgumentNullException>(() => advisor.GenerateRecommendation(null!, TestData.NeutralWeather()));
            Assert.Throws<ArgumentNullException>(() => advisor.GenerateRecommendation(TestData.NewBatch(), (WeatherData)null!));
            Assert.Throws<ArgumentNullException>(() => new CompostAdvisorService(null!));
        }
    }
}
