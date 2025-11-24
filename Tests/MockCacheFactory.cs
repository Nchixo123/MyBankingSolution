using BankingSystem.Application.Caching;
using Moq;

namespace Tests;

public static class MockCacheFactory
{
    public static Mock<ICacheService> CreateCacheService()
    {
        var mockCache = new Mock<ICacheService>();

        // Setup TryGetValue to always return false (cache miss)
        mockCache.Setup(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<object?>.IsAny))
            .Returns(false);

        // Setup Set methods
        mockCache.Setup(x => x.Set(It.IsAny<string>(), It.IsAny<object>()));
        mockCache.Setup(x => x.Set(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()));
        mockCache.Setup(x => x.Set(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan?>()));

        // Setup Remove
        mockCache.Setup(x => x.Remove(It.IsAny<string>()));

        // Setup RemoveByPattern
        mockCache.Setup(x => x.RemoveByPattern(It.IsAny<string>()));

        return mockCache;
    }
}
