using Microsoft.Extensions.Localization;
using Moq;
using src.Shared.Resources;

namespace InteractionService.Tests.Helpers
{
    public static class MockHelper
    {
        public static IStringLocalizer<SharedResources> CreateMockLocalizer()
        {
            var mockLocalizer = new Mock<IStringLocalizer<SharedResources>>();
            
            mockLocalizer.Setup(x => x[It.IsAny<string>()])
                .Returns((string key) => new LocalizedString(key, key));
                
            mockLocalizer.Setup(x => x[It.IsAny<string>(), It.IsAny<object[]>()])
                .Returns((string key, object[] args) => new LocalizedString(key, key));

            return mockLocalizer.Object;
        }
    }
}
