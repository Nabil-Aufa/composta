namespace Composta.Models
{
    public class WasteCategory
    {
        private string _id;
        private string _name;
        private double _co2eFactorPerKg;
        private bool _isCompostable;

        public WasteCategory()
        {
            _id = Guid.NewGuid().ToString();
            _name = string.Empty;
            _co2eFactorPerKg = 0;
            _isCompostable = true;
        }

        public WasteCategory(string name, double co2eFactorPerKg, bool isCompostable) : this()
        {
            _name = name;
            _co2eFactorPerKg = co2eFactorPerKg;
            _isCompostable = isCompostable;
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

        public double Co2eFactorPerKg
        {
            get { return _co2eFactorPerKg; }
            set { _co2eFactorPerKg = value; }
        }

        public bool IsCompostable
        {
            get { return _isCompostable; }
            set { _isCompostable = value; }
        }

        public double GetFactor()
        {
            return _isCompostable ? _co2eFactorPerKg : 0;
        }
    }
}
