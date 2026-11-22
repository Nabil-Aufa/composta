using LiveChartsCore.SkiaSharpView;
using Composta.ViewModels;
using Composta.Models;

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
        public void UpdateFromTimeline_EmptyTimeline_ShowsEmptyState()
        {
            var vm = new Co2eChartViewModel();

            vm.UpdateFromTimeline(new List<CO2eTimelinePoint>());

            Assert.True(vm.HasNoData);
            Assert.Empty(vm.Series);
            Assert.Empty(vm.XAxes[0].Labels!);
        }

        [Fact]
        public void UpdateFromTimeline_PlotsCumulativeValuesInDateOrder()
        {
            var vm = new Co2eChartViewModel();
            var timeline = new List<CO2eTimelinePoint>
            {
                new CO2eTimelinePoint(new DateTime(2026, 11, 16), 0.5, 1.5),
                new CO2eTimelinePoint(new DateTime(2026, 11, 15), 1.0, 1.0)
            };

            vm.UpdateFromTimeline(timeline);

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
