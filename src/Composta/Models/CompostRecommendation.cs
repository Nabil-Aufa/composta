namespace Composta.Models
{
    public class CompostRecommendation
    {
        private string _id;
        private string _batchId;
        private string _message;
        private string _priority;
        private DateTime _createdAt;

        public CompostRecommendation()
        {
            _id = Guid.NewGuid().ToString();
            _batchId = string.Empty;
            _message = string.Empty;
            _priority = "Low";
            _createdAt = DateTime.Now;
        }

        public CompostRecommendation(string batchId, string message, string priority) : this()
        {
            _batchId = batchId;
            _message = message;
            _priority = priority;
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

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public string Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        public DateTime CreatedAt
        {
            get { return _createdAt; }
            set { _createdAt = value; }
        }

        public string ToDisplayText()
        {
            return $"[{_priority}] {_createdAt:dd MMM yyyy HH:mm} - {_message}";
        }
    }
}
