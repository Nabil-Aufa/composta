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
            if (user == null || !TryGetTarget(out double target))
            {
                return false;
            }

            double? progress = GetProgress(user);
            return progress.HasValue && progress.Value >= target;
        }

        public string GetCriteriaType()
        {
            string[] parts = (_criteria ?? string.Empty).Split(':');
            return parts.Length == 2 ? parts[0].Trim().ToUpperInvariant() : string.Empty;
        }

        public bool TryGetTarget(out double target)
        {
            target = 0;
            string[] parts = (_criteria ?? string.Empty).Split(':');
            return parts.Length == 2 &&
                   double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out target);
        }

        public double? GetProgress(User user)
        {
            if (user == null)
            {
                return null;
            }

            switch (GetCriteriaType())
            {
                case "CO2E":
                    return user.GetTotalCO2eSaved();
                case "BATCH":
                    return user.Batches.Count;
                case "HARVEST":
                    return user.Batches.Count(b => b.Status == BatchStatus.Archived);
                case "WEIGHT":
                    return user.GetTotalCompostableWeight();
                default:
                    return null;
            }
        }
    }
}
