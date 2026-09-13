namespace Composta.Models
{
    public class WeatherData
    {
        private double _temperature;
        private double _humidity;
        private double _rainfallMm;
        private string _condition;
        private DateTime _retrievedAt;

        public WeatherData()
        {
            _condition = string.Empty;
            _retrievedAt = DateTime.Now;
        }

        public WeatherData(double temperature, double humidity, double rainfallMm, string condition, DateTime retrievedAt)
        {
            _temperature = temperature;
            _humidity = humidity;
            _rainfallMm = rainfallMm;
            _condition = condition;
            _retrievedAt = retrievedAt;
        }

        public double Temperature
        {
            get { return _temperature; }
            set { _temperature = value; }
        }

        public double Humidity
        {
            get { return _humidity; }
            set { _humidity = value; }
        }

        public double RainfallMm
        {
            get { return _rainfallMm; }
            set { _rainfallMm = value; }
        }

        public string Condition
        {
            get { return _condition; }
            set { _condition = value; }
        }

        public DateTime RetrievedAt
        {
            get { return _retrievedAt; }
            set { _retrievedAt = value; }
        }

        public bool IsRainy()
        {
            return _rainfallMm > 0 ||
                   _condition.Contains("rain", StringComparison.OrdinalIgnoreCase) ||
                   _condition.Contains("hujan", StringComparison.OrdinalIgnoreCase);
        }
    }
}
