using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Composta.Models;
using Composta.Interfaces;

namespace Composta.ViewModels
{
    public partial class Co2eChartViewModel : ObservableObject
    {
        private static readonly SKColor Forest = new SKColor(0x24, 0x4B, 0x38);
        private static readonly SKColor Muted = new SKColor(0x65, 0x70, 0x63);
        private static readonly SKColor Line = new SKColor(0xCF, 0xD8, 0xC6);

        [ObservableProperty]
        private ISeries[] _series = Array.Empty<ISeries>();

        [ObservableProperty]
        private bool _hasNoData = true;

        [ObservableProperty]
        private Axis[] _xAxes = new[]
        {
            CreateXAxis(Array.Empty<string>())
        };

        public Axis[] YAxes { get; } = new[]
        {
            new Axis
            {
                Labeler = value => $"{value:0.##} kg",
                LabelsPaint = new SolidColorPaint(Muted),
                SeparatorsPaint = new SolidColorPaint(Line) { StrokeThickness = 1 },
                TextSize = 11,
                MinLimit = 0
            }
        };

        private static Axis CreateXAxis(string[] labels)
        {
            return new Axis
            {
                Labels = labels,
                LabelsPaint = new SolidColorPaint(Muted),
                SeparatorsPaint = null,
                TextSize = 11
            };
        }

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
                        LineSmoothness = 0,
                        Stroke = new SolidColorPaint(Forest) { StrokeThickness = 2 },
                        GeometryStroke = new SolidColorPaint(Forest) { StrokeThickness = 2 },
                        GeometryFill = new SolidColorPaint(Forest),
                        DataLabelsPaint = new SolidColorPaint(Forest),
                        DataLabelsSize = 11,
                        DataLabelsPosition = DataLabelsPosition.Top,
                        DataLabelsFormatter = point => point.Coordinate.PrimaryValue.ToString("0.#")
                    }
                };
            XAxes = new[]
            {
                CreateXAxis(Array.Empty<string>())
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
                CreateXAxis(dailyTotals
                    .Select(day => day.Date.ToString("dd MMM"))
                    .ToArray())
            };
        }
    }
}
