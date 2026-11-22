using Composta.Enums;
using Composta.Models;

namespace Composta.ViewModels
{
    public class BatchItemViewModel
    {
        public BatchItemViewModel(CompostBatch batch) : this(batch, DateTime.Today)
        {
        }

        public BatchItemViewModel(CompostBatch batch, DateTime today)
        {
            Batch = batch ?? throw new ArgumentNullException(nameof(batch));
            Name = AppData.GetBatchName(batch);
            StatusText = AppData.GetStatusText(batch.Status);
            WeightText = batch.GetTotalWeight().ToString("0.##") + " kg";
            RatioText = batch.CalculateRatio().ToString("0.##");
            MaturityText = batch.EstimatedMaturityDate.ToString("dd MMM yyyy");
            Detail = batch.GetTotalWeight().ToString("0.##") + " kg  •  rasio brown/green "
                + batch.CalculateRatio().ToString("0.##") + "  •  matang "
                + batch.EstimatedMaturityDate.ToString("dd MMM yyyy");

            double totalDays = (batch.EstimatedMaturityDate.Date - batch.StartDate.Date).TotalDays;
            double elapsedDays = (today.Date - batch.StartDate.Date).TotalDays;
            Progress = totalDays > 0 ? Math.Clamp(elapsedDays / totalDays * 100, 0, 100) : 100;

            int totalDayCount = Math.Max(1, (batch.EstimatedMaturityDate.Date - batch.StartDate.Date).Days);
            int currentDay = Math.Clamp((today.Date - batch.StartDate.Date).Days + 1, 1, totalDayCount);
            DayText = "Hari ke-" + currentDay + " dari " + totalDayCount;
        }

        public string DayText { get; }

        public CompostBatch Batch { get; }

        public string Name { get; }

        public string StatusText { get; }

        public string Detail { get; }

        public string WeightText { get; }

        public string RatioText { get; }

        public string MaturityText { get; }

        public double Progress { get; }

        public string SelectionLabel
        {
            get { return Name + " (" + StatusText + ")"; }
        }

        public bool IsActive
        {
            get { return Batch.Status == BatchStatus.Active; }
        }

        public bool IsCuring
        {
            get { return Batch.Status == BatchStatus.Curing; }
        }

        public bool IsReady
        {
            get { return Batch.Status == BatchStatus.Ready; }
        }

        public bool IsArchived
        {
            get { return Batch.Status == BatchStatus.Archived; }
        }

        public bool IsCuringOrLater
        {
            get { return Batch.Status >= BatchStatus.Curing; }
        }

        public bool IsReadyOrLater
        {
            get { return Batch.Status >= BatchStatus.Ready; }
        }
    }
}
