using Composta.Enums;

namespace Composta.Models
{
    public class CompostBatch
    {
        private string _id;
        private string _userId;
        private DateTime _startDate;
        private BatchStatus _status;
        private double _brownGreenRatio;
        private DateTime _estimatedMaturityDate;
        private List<WasteItem> _wasteItems;
        private List<MaintenanceSchedule> _schedules;

        public CompostBatch()
        {
            _id = Guid.NewGuid().ToString();
            _userId = string.Empty;
            _startDate = DateTime.Now;
            _status = BatchStatus.Active;
            _brownGreenRatio = 0;
            _estimatedMaturityDate = _startDate.AddDays(60);
            _wasteItems = new List<WasteItem>();
            _schedules = new List<MaintenanceSchedule>();
        }

        public CompostBatch(string userId, DateTime startDate, int maturityDays) : this()
        {
            _userId = userId;
            _startDate = startDate;
            _estimatedMaturityDate = startDate.AddDays(maturityDays);
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string UserId
        {
            get { return _userId; }
            set { _userId = value; }
        }

        public DateTime StartDate
        {
            get { return _startDate; }
            set { _startDate = value; }
        }

        public BatchStatus Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public double BrownGreenRatio
        {
            get { return _brownGreenRatio; }
            set { _brownGreenRatio = value; }
        }

        public DateTime EstimatedMaturityDate
        {
            get { return _estimatedMaturityDate; }
            set { _estimatedMaturityDate = value; }
        }

        public List<WasteItem> WasteItems
        {
            get { return _wasteItems; }
            set { _wasteItems = value ?? new List<WasteItem>(); }
        }

        public List<MaintenanceSchedule> Schedules
        {
            get { return _schedules; }
            set { _schedules = value ?? new List<MaintenanceSchedule>(); }
        }

        public void AddWasteItem(WasteItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            _wasteItems.Add(item);
            _brownGreenRatio = CalculateRatio();
        }

        public void AddSchedule(MaintenanceSchedule schedule)
        {
            ArgumentNullException.ThrowIfNull(schedule);
            schedule.BatchId = _id;
            _schedules.Add(schedule);
        }

        public double CalculateRatio()
        {
            double brown = _wasteItems
                .OfType<CompostableWaste>()
                .Where(w => w.BrownGreenType == BrownGreenType.Brown)
                .Sum(w => w.Weight);
            double green = _wasteItems
                .OfType<CompostableWaste>()
                .Where(w => w.BrownGreenType == BrownGreenType.Green)
                .Sum(w => w.Weight);

            if (green <= 0)
            {
                return Math.Round(brown, 2);
            }

            return Math.Round(brown / green, 2);
        }

        public void UpdateProgress()
        {
            if (_status == BatchStatus.Archived)
            {
                return;
            }

            _brownGreenRatio = CalculateRatio();

            DateTime now = DateTime.Now;
            double totalDays = (_estimatedMaturityDate - _startDate).TotalDays;
            double elapsedDays = (now - _startDate).TotalDays;

            if (totalDays <= 0 || now >= _estimatedMaturityDate)
            {
                _status = BatchStatus.Ready;
            }
            else if (elapsedDays >= totalDays / 2)
            {
                _status = BatchStatus.Curing;
            }
            else
            {
                _status = BatchStatus.Active;
            }
        }

        public double GetTotalWeight()
        {
            return Math.Round(_wasteItems.Sum(w => w.Weight), 2);
        }

        public bool IsReadyToHarvest()
        {
            return _status == BatchStatus.Ready ||
                   (_status != BatchStatus.Archived && DateTime.Now >= _estimatedMaturityDate);
        }
    }
}
