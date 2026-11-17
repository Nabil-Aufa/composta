using System.Globalization;
using Composta.Enums;

namespace Composta.Models
{
    public class Badge
    {
        private string _id;
        private string _name;
        private string _criteria;
        private string _iconPath;

        public Badge()
        {
            _id = Guid.NewGuid().ToString();
            _name = string.Empty;
            _criteria = string.Empty;
            _iconPath = string.Empty;
        }

        public Badge(string name, string criteria, string iconPath) : this()
        {
            _name = name;
            _criteria = criteria;
            _iconPath = iconPath;
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

        public string Criteria
        {
            get { return _criteria; }
            set { _criteria = value; }
        }

        public string IconPath
        {
            get { return _iconPath; }
            set { _iconPath = value; }
        }

        public bool IsUnlocked(User user)
        {
            if (user == null || string.IsNullOrWhiteSpace(_criteria))
            {
                return false;
            }

            string[] parts = _criteria.Split(':');
            if (parts.Length != 2 ||
                !double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double target))
            {
                return false;
            }

            switch (parts[0].Trim().ToUpperInvariant())
            {
                case "CO2E":
                    return user.GetTotalCO2eSaved() >= target;
                case "BATCH":
                    return user.Batches.Count >= target;
                case "HARVEST":
                    return user.Batches.Count(b => b.Status == BatchStatus.Ready || b.Status == BatchStatus.Archived) >= target;
                default:
                    return false;
            }
        }
    }
}
