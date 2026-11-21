namespace Composta.Models
{
    public class CO2eTimelinePoint
    {
        public CO2eTimelinePoint(DateTime date, double dailyCO2e, double cumulativeCO2e)
        {
            Date = date.Date;
            DailyCO2e = Math.Round(dailyCO2e, 2);
            CumulativeCO2e = Math.Round(cumulativeCO2e, 2);
        }

        public DateTime Date { get; }
        public double DailyCO2e { get; }
        public double CumulativeCO2e { get; }
    }
}