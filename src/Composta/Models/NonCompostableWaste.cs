namespace Composta.Models
{
    public class NonCompostableWaste : WasteItem
    {
        private string _reason;

        public NonCompostableWaste() : base()
        {
            _reason = string.Empty;
        }

        public NonCompostableWaste(string name, double weight, WasteCategory? category, string reason)
            : base(name, weight, category)
        {
            _reason = reason;
        }

        public string Reason
        {
            get { return _reason; }
            set { _reason = value; }
        }

        public override double GetCO2eFactor()
        {
            return 0;
        }

        public string GetDisposalAdvice()
        {
            if (string.IsNullOrWhiteSpace(_reason))
            {
                return $"{Name} tidak dapat dikomposkan. Pisahkan dan buang ke tempat sampah anorganik atau bank sampah.";
            }

            return $"{Name} tidak dapat dikomposkan karena {_reason}. Pisahkan dan buang ke tempat sampah anorganik atau bank sampah.";
        }
    }
}
