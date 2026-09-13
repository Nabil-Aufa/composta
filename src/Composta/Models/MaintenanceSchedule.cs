using Composta.Enums;

namespace Composta.Models
{
    public class MaintenanceSchedule
    {
        private string _id;
        private string _batchId;
        private TaskType _taskType;
        private DateTime _dueDate;
        private bool _isCompleted;

        public MaintenanceSchedule()
        {
            _id = Guid.NewGuid().ToString();
            _batchId = string.Empty;
            _taskType = TaskType.Turn;
            _dueDate = DateTime.Now;
            _isCompleted = false;
        }

        public MaintenanceSchedule(TaskType taskType, DateTime dueDate) : this()
        {
            _taskType = taskType;
            _dueDate = dueDate;
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string BatchId
        {
            get { return _batchId; }
            set { _batchId = value; }
        }

        public TaskType TaskType
        {
            get { return _taskType; }
            set { _taskType = value; }
        }

        public DateTime DueDate
        {
            get { return _dueDate; }
            set { _dueDate = value; }
        }

        public bool IsCompleted
        {
            get { return _isCompleted; }
            set { _isCompleted = value; }
        }

        public void MarkCompleted()
        {
            _isCompleted = true;
        }

        public bool IsOverdue()
        {
            return !_isCompleted && DateTime.Now > _dueDate;
        }
    }
}
