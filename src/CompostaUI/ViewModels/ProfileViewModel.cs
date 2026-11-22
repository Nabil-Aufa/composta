using Composta.Interfaces;
using Composta.Models;

namespace Composta.ViewModels
{
    public class ProfileViewModel
    {
        public ProfileViewModel(User user, ICarbonCalculator calculator)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(calculator);
            Name = user.Name;
            Email = user.Email;
            JoinedAtText = "Bergabung " + user.JoinedAt.ToString("dd MMM yyyy");
            TotalWeightText = user.Batches.Sum(b => b.GetTotalWeight()).ToString("0.##") + " kg";
            TotalCo2eText = calculator.CalculateCO2eSaved(user.Batches.SelectMany(b => b.WasteItems).ToList()).ToString("0.##") + " kg CO2e";
            BatchCountText = user.Batches.Count.ToString();
        }

        public string Name { get; }
        public string Email { get; }
        public string JoinedAtText { get; }
        public string TotalWeightText { get; }
        public string TotalCo2eText { get; }
        public string BatchCountText { get; }
    }
}
