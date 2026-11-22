using Composta;
using Composta.Interfaces;
using Composta.Services;
using Xunit;

namespace CompostaUI.Tests
{
    public class AppDataWeatherServiceTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateWeatherService_WhenApiKeyMissingOrWhitespace_ReturnsOfflineWeatherService(string? apiKey)
        {
            IWeatherService service = AppData.CreateWeatherService(apiKey);

            Assert.IsType<OfflineWeatherService>(service);
        }

        [Fact]
        public void CreateWeatherService_WhenApiKeyProvided_ReturnsOpenWeatherMapService()
        {
            IWeatherService service = AppData.CreateWeatherService("test-api-key");

            Assert.IsType<OpenWeatherMapService>(service);
        }
    }
}