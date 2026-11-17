using Composta.Models;

namespace Composta.Interfaces
{
    public interface ICarbonCalculator
    {
        double CalculateCO2eSaved(List<WasteItem> items);

        double CalculateBatchImpact(CompostBatch batch);
    }
}
