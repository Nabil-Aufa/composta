using Composta.Interfaces;
using Composta.Models;

namespace Composta.ViewModels
{
    public class ProfileViewModel
    {
        public ProfileViewModel(User user, ICarbonCalculator calculator)
            : this(user, calculator, Array.Empty<UserBadgeStatus>(), null)
        {
        }

        public ProfileViewModel(User user, ICarbonCalculator calculator,
                                IEnumerable<UserBadgeStatus> badges, string? badgeErrorText)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(calculator);
            ArgumentNullException.ThrowIfNull(badges);
            Name = user.Name;
            Email = user.Email;
            JoinedAtText = "Bergabung " + user.JoinedAt.ToString("dd MMM yyyy");
            TotalWeightText = user.Batches.Sum(b => b.GetTotalWeight()).ToString("0.##") + " kg";
            TotalCo2eText = calculator.CalculateCO2eSaved(user.Batches.SelectMany(b => b.WasteItems).ToList()).ToString("0.##") + " kg CO2e";
            BatchCountText = user.Batches.Count.ToString();

            Badges = badges
                .Select(b => new BadgeItemViewModel(b, user))
                .OrderByDescending(b => b.IsUnlocked)
                .ToList();
            BadgeErrorText = badgeErrorText ?? string.Empty;
            BadgeSummaryText = HasBadgeError
                ? "Koleksi badge gagal dimuat."
                : Badges.Count(b => b.IsUnlocked) + " dari " + Badges.Count + " badge diperoleh";
        }

        public string Name { get; }
        public string Email { get; }
        public string JoinedAtText { get; }
        public string TotalWeightText { get; }
        public string TotalCo2eText { get; }
        public string BatchCountText { get; }

        public IReadOnlyList<BadgeItemViewModel> Badges { get; }
        public string BadgeSummaryText { get; }
        public string BadgeErrorText { get; }
        public bool HasBadgeError => !string.IsNullOrEmpty(BadgeErrorText);
    }
}
