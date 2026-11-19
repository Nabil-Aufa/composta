
using CommunityToolkit.Mvvm.ComponentModel;
using Composta.Models;
using Composta.Enums;

namespace Composta.ViewModels
{
    public partial class MaintenanceScheduleItemViewModel : ObservableObject
    {
        public MaintenanceSchedule Schedule { get; }

        public MaintenanceScheduleItemViewModel(MaintenanceSchedule schedule)
        {
            Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        }

        public string TaskName => Schedule.TaskType switch
        {
            TaskType.Turn => "Aduk kompos",
            TaskType.Water => "Siram kompos",
            TaskType.CheckMoisture => "Periksa kelembapan",
            TaskType.Harvest => "Panen kompos",
            _ => "Perawatan lainnya"
        };

        public string DueDateText => Schedule.DueDate.ToString("dd MMM yyyy");

        public bool IsCompleted => Schedule.IsCompleted;

        public bool IsOverdue => Schedule.IsOverdue();

        public string StatusText =>
            IsCompleted ? "Selesai" :
            IsOverdue ? "Terlambat" : "Belum selesai";

        public bool IsPending => !IsCompleted && !IsOverdue;

        public bool CanComplete => !IsCompleted;
    }
}
