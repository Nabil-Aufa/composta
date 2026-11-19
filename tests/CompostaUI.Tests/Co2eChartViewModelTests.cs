using Composta.Enums;
using LiveChartsCore.SkiaSharpView;
using Composta.ViewModels;
using Composta.Models;
using Composta.Services;

namespace Composta.UITests
{
    public class Co2eChartViewModelTests
    {
        [Fact]
        public void UpdateChart_EmptyData_ShowsEmptyState()
        {
            var vm = new Co2eChartViewModel();

            vm.UpdateChart(Array.Empty<double>());

            Assert.True(vm.HasNoData);
            Assert.Empty(vm.Series);
        }

        [Fact]
        public void UpdateChart_WithData_HidesEmptyState()
        {
            var vm = new Co2eChartViewModel();

            vm.UpdateChart(new double[] { 0.5, 1.3, 1.7 });

            Assert.False(vm.HasNoData);
            Assert.Single(vm.Series);
        }

        [Fact]
        public void UpdateFromBatches_EmptyBatches_ShowsEmptyState()
        {
            var vm = new Co2eChartViewModel();
            var calculator = new CarbonCalculatorService();

            vm.UpdateFromBatches(
                new List<CompostBatch>(),
                calculator
            );

            Assert.True(vm.HasNoData);
            Assert.Empty(vm.Series);
        }

        [Fact]
        public void UpdateFromBatches_CalculatesCumulativeCo2eCorrectly()
        {
            var vm = new Co2eChartViewModel();
            var calculator = new CarbonCalculatorService();

            var category = new WasteCategory("Organik", 0.5, true);
            var nonCompostableCategory = new WasteCategory("Plastik", 0, false);

            var batch = new CompostBatch();

            // Sengaja memasukkan tanggal yang lebih baru terlebih dahulu
            var waste2 = new CompostableWaste(
                "Daun", 1.0, category, BrownGreenType.Brown, 50);
            waste2.DateAdded = new DateTime(2026, 11, 16);

            var waste1 = new CompostableWaste(
                "Sisa sayuran", 2.0, category, BrownGreenType.Green, 50);
            waste1.DateAdded = new DateTime(2026, 11, 15);

            var plastic = new NonCompostableWaste(
                "Plastik", 10.0, nonCompostableCategory, "Tidak dapat dikomposkan");
            plastic.DateAdded = new DateTime(2026, 11, 17);

            batch.AddWasteItem(waste2);
            batch.AddWasteItem(waste1);
            batch.AddWasteItem(plastic);

            vm.UpdateFromBatches(new[] { batch }, calculator);

            Assert.False(vm.HasNoData);

            var series = Assert.IsType<LineSeries<double>>(Assert.Single(vm.Series));
            Assert.Equal(new double[] { 1.0, 1.5 }, series.Values?.ToArray());

            Assert.Equal(
                new[]
                {
                    new DateTime(2026, 11, 15).ToString("dd MMM"),
                    new DateTime(2026, 11, 16).ToString("dd MMM")
                },
                vm.XAxes[0].Labels
            );
        }
    }
}
