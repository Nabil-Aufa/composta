using System.Text.Json.Serialization;

namespace Composta.Models
{
    [JsonDerivedType(typeof(CompostableWaste), "compostable")]
    [JsonDerivedType(typeof(NonCompostableWaste), "noncompostable")]
    public abstract class WasteItem
    {
        private string _id;
        private string _name;
        private double _weight;
        private DateTime _dateAdded;
        private WasteCategory? _category;

        protected WasteItem()
        {
            _id = Guid.NewGuid().ToString();
            _name = string.Empty;
            _weight = 0;
            _dateAdded = DateTime.Now;
            _category = null;
        }

        protected WasteItem(string name, double weight, WasteCategory? category) : this()
        {
            _name = name;
            Weight = weight;
            _category = category;
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

        public double Weight
        {
            get { return _weight; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(Weight), "Berat sampah tidak boleh negatif.");
                }
                _weight = value;
            }
        }

        public DateTime DateAdded
        {
            get { return _dateAdded; }
            set { _dateAdded = value; }
        }

        public WasteCategory? Category
        {
            get { return _category; }
            set { _category = value; }
        }

        public abstract double GetCO2eFactor();

        public virtual string Describe()
        {
            string categoryName = _category != null ? _category.Name : "Tanpa kategori";
            return $"{_name} - {_weight:0.##} kg ({categoryName})";
        }
    }
}
