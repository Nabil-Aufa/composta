using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Composta.Models;
using Composta.Interfaces;

namespace Composta.ViewModels
{
    public partial class Co2eChartViewModel : ObservableObject
    {
        [ObservableProperty]
        private ISeries[] _series = Array.Empty<ISeries>();

        [ObservableProperty]
        private bool _hasNoData = true;

        [ObservableProperty]
        private Axis[] _xAxes = new[]
        {
            new Axis
            {
                Labels = Array.Empty<string>()
            }
        };

        public Axis[] YAxes { get; } = new[]
        {
            new Axis
            {
                Labeler = value => $"{value:0.##} kg"
            }
        };

        public void UpdateChart(IEnumerable<double> cumulativeValues)
        {
            double[] values = cumulativeValues.ToArray();

            HasNoData = values.Length == 0;

            Series = HasNoData
                ? Array.Empty<ISeries>()
                : new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values,
                        Fill = null,
                        GeometrySize = 8,
                        LineSmoothness = 0
                    }
                };
            XAxes = new[]
            {
                new Axis { Labels = Array.Empty<string>() }
            };
        }

        public void UpdateFromBatches(
            IEnumerable<CompostBatch> batches,
            ICarbonCalculator calculator)
        {
            var dailyTotals = batches
                .SelectMany(batch => batch.WasteItems)
                .Where(item => item is CompostableWaste)
                .GroupBy(item => item.DateAdded.Date)
                .OrderBy(group => group.Key)
                .Select(group => new
                {
                    Date = group.Key,
                    DailyCo2e = calculator.CalculateCO2eSaved(group.ToList())
                })
                .ToList();

            double cumulative = 0;

            double[] cumulativeValues = dailyTotals
                .Select(day =>
                {
                    cumulative += day.DailyCo2e;
                    return Math.Round(cumulative, 2);
                })
                .ToArray();

            UpdateChart(cumulativeValues);
            XAxes = new[]
            {
                new Axis
                {
                    Labels = dailyTotals
                        .Select(day => day.Date.ToString("dd MMM"))
                        .ToArray()
                }
            };
        }
    }
}
