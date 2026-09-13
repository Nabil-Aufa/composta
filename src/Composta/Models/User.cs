using System.Text.Json.Serialization;
using Composta.Enums;

namespace Composta.Models
{
    public class User
    {
        private string _id;
        private string _name;
        private string _email;
        private DateTime _joinedAt;
        private List<CompostBatch> _batches;

        public User()
        {
            _id = Guid.NewGuid().ToString();
            _name = string.Empty;
            _email = string.Empty;
            _joinedAt = DateTime.Now;
            _batches = new List<CompostBatch>();
        }

        public User(string name, string email) : this()
        {
            _name = name;
            _email = email;
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        public DateTime JoinedAt
        {
            get { return _joinedAt; }
            set { _joinedAt = value; }
        }

        [JsonIgnore]
        public List<CompostBatch> Batches
        {
            get { return _batches; }
            set { _batches = value ?? new List<CompostBatch>(); }
        }

        public void AddBatch(CompostBatch batch)
        {
            ArgumentNullException.ThrowIfNull(batch);
            batch.UserId = _id;
            _batches.Add(batch);
        }

        public List<CompostBatch> GetActiveBatches()
        {
            return _batches.Where(b => b.Status != BatchStatus.Archived).ToList();
        }

        public double GetTotalCO2eSaved()
        {
            double total = _batches
                .SelectMany(b => b.WasteItems)
                .Sum(item => item.Weight * item.GetCO2eFactor());
            return Math.Round(total, 2);
        }
    }
}
