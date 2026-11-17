using Composta.Enums;

namespace Composta.Models
{
    public class CompostableWaste : WasteItem
    {
        private const double MinimumMoisture = 40;

        private BrownGreenType _brownGreenType;
        private double _moistureContent;

        public CompostableWaste() : base()
        {
            _brownGreenType = BrownGreenType.Green;
            _moistureContent = 50;
        }

        public CompostableWaste(string name, double weight, WasteCategory? category, BrownGreenType brownGreenType, double moistureContent)
            : base(name, weight, category)
        {
            _brownGreenType = brownGreenType;
            MoistureContent = moistureContent;
        }

        public BrownGreenType BrownGreenType
        {
            get { return _brownGreenType; }
            set { _brownGreenType = value; }
        }

        public double MoistureContent
        {
            get { return _moistureContent; }
            set { _moistureContent = Math.Clamp(value, 0, 100); }
        }

        public override double GetCO2eFactor()
        {
            return Category != null ? Category.GetFactor() : 0;
        }

        public bool NeedsWater()
        {
            return _moistureContent < MinimumMoisture;
        }

        public override string Describe()
        {
            return $"{base.Describe()} [{_brownGreenType}, kelembapan {_moistureContent:0.#}%]";
        }
    }
}
