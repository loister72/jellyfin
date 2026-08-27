using MediaBrowser.Providers.Plugins.Tmdb;
using Xunit;

namespace Jellyfin.Providers.Tests.Tmdb
{
    public static class TmdbClientManagerTests
    {
        [Fact]
        public static void BuildItemCacheKey_IncludesCountryCode()
        {
            var usKey = TmdbClientManager.BuildItemCacheKey("movie", 11, "en", "en,null", "US");
            var gbKey = TmdbClientManager.BuildItemCacheKey("movie", 11, "en", "en,null", "GB");

            Assert.NotEqual(usKey, gbKey);
        }

        [Fact]
        public static void BuildItemCacheKey_IncludesImageLanguages()
        {
            var englishImages = TmdbClientManager.BuildItemCacheKey("series", 11, "en-US", "en,null", "US");
            var germanImages = TmdbClientManager.BuildItemCacheKey("series", 11, "en-US", "de,null", "US");

            Assert.NotEqual(englishImages, germanImages);
        }

        [Fact]
        public static void BuildItemCacheKey_UsesNormalizedLanguage()
        {
            var argentineSpanish = TmdbClientManager.BuildItemCacheKey("movie", 11, "es-419", "es,null", "AR");
            var mexicanSpanish = TmdbClientManager.BuildItemCacheKey("movie", 11, "es-419", "es,null", "MX");

            Assert.NotEqual(argentineSpanish, mexicanSpanish);
        }

        [Fact]
        public static void BuildItemCacheKey_IncludesAdditionalRequestParts()
        {
            var originalOrder = TmdbClientManager.BuildItemCacheKey("episode", 11, "en-US", "en,null", "US", 1, 2, "originalAirDate");
            var dvdOrder = TmdbClientManager.BuildItemCacheKey("episode", 11, "en-US", "en,null", "US", 1, 2, "dvd");

            Assert.NotEqual(originalOrder, dvdOrder);
        }

        [Fact]
        public static void BuildRequestCacheKey_AvoidsSeparatorCollisions()
        {
            var firstKey = TmdbClientManager.BuildRequestCacheKey("search", "Star-Wars", "en-US");
            var secondKey = TmdbClientManager.BuildRequestCacheKey("search", "Star", "Wars-en-US");

            Assert.NotEqual(firstKey, secondKey);
        }
    }
}
