namespace ThriveTest.Engine.Caching.Tests;

using Xunit;

public class DiskCacheTests
{
    [Theory]
    [InlineData(42UL)]
    [InlineData(745234765UL)]
    [InlineData(8965478436743512356UL)]
    public void DiskCache_RoundTripHashPath(ulong hash)
    {
        var temp = new byte[128];
        var path = CachePaths.GenerateCachePath(hash, CacheItemType.Png, temp);

        Assert.NotEmpty(path);
        Assert.StartsWith(Constants.CACHE_IMAGES_FOLDER, path);
        Assert.EndsWith(".png", path);

        var parsed = CachePaths.ParseCachePath(path, (Constants.CACHE_IMAGES_FOLDER + '/').Length, temp);

        Assert.Equal(hash, parsed);
    }
}
