using Composta.Enums;
using Composta.Models;

namespace Composta.ViewModels
{
    public class BatchItemViewModel
    {
        public BatchItemViewModel(CompostBatch batch)
        {
            Batch = batch ?? throw new ArgumentNullException(nameof(batch));
            Name = AppData.GetBatchName(batch);
            StatusText = AppData.GetStatusText(batch.Status);
            Detail = batch.GetTotalWeight().ToString("0.##") + " kg  •  rasio brown/green "
                + batch.CalculateRatio().ToString("0.##") + "  •  matang "
                + batch.EstimatedMaturityDate.ToString("dd MMM yyyy");

            double totalDays = (batch.EstimatedMaturityDate - batch.StartDate).TotalDays;
            double elapsedDays = (DateTime.Now - batch.StartDate).TotalDays;
            Progress = totalDays > 0 ? Math.Clamp(elapsedDays / totalDays * 100, 0, 100) : 100;
        }

        public CompostBatch Batch { get; }

        public string Name { get; }

        public string StatusText { get; }

        public string Detail { get; }

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
    }
}
