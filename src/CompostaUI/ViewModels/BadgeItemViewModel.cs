using Composta.Models;

namespace Composta.ViewModels
{
    public class BadgeItemViewModel
    {
        public BadgeItemViewModel(UserBadgeStatus status, User user)
        {
            ArgumentNullException.ThrowIfNull(status);
            ArgumentNullException.ThrowIfNull(user);

            Badge = status.Badge;
            IsUnlocked = status.IsEarned;
            Icon = GetIcon(Badge.GetCriteriaType());
            DescriptionText = GetDescription(Badge);
            StatusText = IsUnlocked
                ? "Diperoleh " + status.EarnedAt!.Value.ToString("dd MMM yyyy")
                : GetProgressText(Badge, user);
        }

        public Badge Badge { get; }
        public bool IsUnlocked { get; }
        public bool IsLocked => !IsUnlocked;
        public string Name => Badge.Name;
        public string Icon { get; }
        public string DescriptionText { get; }
        public string StatusText { get; }

        private static string GetIcon(string criteriaType)
        {
            switch (criteriaType)
            {
                case "BATCH":
                    return "🌱";
                case "HARVEST":
                    return "🧺";
                case "WEIGHT":
                    return "⚖";
                case "CO2E":
                    return "🌍";
                default:
                    return "🏅";
            }
        }

        private static string GetDescription(Badge badge)
        {
            if (!badge.TryGetTarget(out double target))
            {
                return "Kriteria belum diatur.";
            }

            string value = target.ToString("0.##");
            switch (badge.GetCriteriaType())
            {
                case "BATCH":
                    return "Buat " + value + " batch kompos.";
                case "HARVEST":
                    return "Panen " + value + " batch kompos.";
                case "WEIGHT":
                    return "Olah " + value + " kg sampah yang dapat dikompos.";
                case "CO2E":
                    return "Cegah " + value + " kg emisi CO₂e.";
                default:
                    return "Kriteria belum dikenali.";
            }
        }

        private static string GetProgressText(Badge badge, User user)
        {
            double? progress = badge.GetProgress(user);
            if (!progress.HasValue || !badge.TryGetTarget(out double target))
            {
                return "Belum diperoleh";
            }

            string unit = badge.GetCriteriaType() switch
            {
                "WEIGHT" => " kg",
                "CO2E" => " kg CO₂e",
                _ => " batch"
            };
            double shown = Math.Min(progress.Value, target);
            return "Belum diperoleh • " + shown.ToString("0.##") + " / " + target.ToString("0.##") + unit;
        }
    }
}
