using Composta.Models;

namespace Composta.Interfaces
{
    public interface IRecommendationEngine
    {
        CompostRecommendation GenerateRecommendation(CompostBatch batch, WeatherData weather);
    }
}
