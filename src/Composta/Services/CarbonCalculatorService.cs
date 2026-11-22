using Composta.Interfaces;
using Composta.Models;

namespace Composta.Services
{
    public class CarbonCalculatorService : ICarbonCalculator
    {
        private double _defaultFactor;

        public const double FoodWasteAvoidedFactor = 0.717;

        public CarbonCalculatorService() : this(FoodWasteAvoidedFactor)
        {
        }

        public CarbonCalculatorService(double defaultFactor)
        {
            _defaultFactor = defaultFactor;
        }

        public double DefaultFactor
        {
            get { return _defaultFactor; }
            set { _defaultFactor = value; }
        }

        public double CalculateCO2eSaved(List<WasteItem> items)
        {
            if (items == null)
            {
                return 0;
            }

            double total = 0;
            foreach (WasteItem item in items)
            {
                if (item is NonCompostableWaste)
                {
                    continue;
                }

                double factor = item.Category != null ? item.GetCO2eFactor() : _defaultFactor;

                total += item.Weight * factor;
            }

            return Math.Round(total, 2);
        }

        public double CalculateBatchImpact(CompostBatch batch)
        {
            ArgumentNullException.ThrowIfNull(batch);
            return CalculateCO2eSaved(batch.WasteItems);
        }
    }
}
