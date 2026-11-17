using Composta.Enums;
using Composta.Models;
using Composta.Services;

namespace Composta.Tests
{
    public class CarbonCalculatorServiceTests
    {
        private readonly CarbonCalculatorService _calculator = new CarbonCalculatorService();

        [Fact]
        public void CompostableWaste_UsesCategoryFactor()
        {
            List<WasteItem> items = new List<WasteItem> { TestData.Green(4), TestData.Brown(10) };

            double result = _calculator.CalculateCO2eSaved(items);

            Assert.Equal(4 * 0.5 + 10 * 0.3, result, 2);
        }

        [Fact]
        public void NonCompostableWaste_IsNotCounted()
        {
            List<WasteItem> items = new List<WasteItem>
            {
                TestData.Green(2),
                new NonCompostableWaste("Botol plastik", 5, TestData.Plastic, "Plastik")
            };

            double result = _calculator.CalculateCO2eSaved(items);

            Assert.Equal(1.0, result, 2);
        }

        [Fact]
        public void WasteWithoutCategory_UsesDefaultFactor()
        {
            List<WasteItem> items = new List<WasteItem>
            {
                new CompostableWaste("Campuran", 3, null, BrownGreenType.Green, 60)
            };

            Assert.Equal(1.5, _calculator.CalculateCO2eSaved(items), 2);
            Assert.Equal(0.9, new CarbonCalculatorService(0.3).CalculateCO2eSaved(items), 2);
        }

        [Fact]
        public void NullOrEmptyList_ReturnsZero()
        {
            Assert.Equal(0, _calculator.CalculateCO2eSaved(null!));
            Assert.Equal(0, _calculator.CalculateCO2eSaved(new List<WasteItem>()));
        }

        [Fact]
        public void BatchImpact_SumsAllItemsInBatch()
        {
            CompostBatch batch = TestData.BatchWithRatio(5, 2);

            Assert.Equal(5 * 0.3 + 2 * 0.5, _calculator.CalculateBatchImpact(batch), 2);
            Assert.Throws<ArgumentNullException>(() => _calculator.CalculateBatchImpact(null!));
        }

        [Fact]
        public void Result_IsRoundedToTwoDecimals()
        {
            List<WasteItem> items = new List<WasteItem> { TestData.Green(1.111) };

            Assert.Equal(0.56, _calculator.CalculateCO2eSaved(items));
        }
    }
}
