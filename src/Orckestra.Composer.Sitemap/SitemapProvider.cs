using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Orckestra.Composer.CompositeC1.Sitemap;
using Orckestra.Composer.Sitemap.Config;

using static Orckestra.Composer.Utils.MessagesHelper.ArgumentException;

namespace Orckestra.Composer.Sitemap
{
    public class SitemapProvider : ISitemapProvider
    {
        public ISitemapEntryProvider EntryProvider { get; }

        public int NumberOfEntriesPerSitemap { get; }
        public string SitemapFilePrefix { get; }

        public SitemapProvider(ISitemapEntryProvider entryProvider, ISitemapProviderConfig config, IC1SitemapConfiguration mainConfig)
        {
            if (config == null) { throw new ArgumentNullException(nameof(config)); }
            if (mainConfig.NumberOfEntriesPerFile < 1) throw new ArgumentOutOfRangeException(
                nameof(mainConfig), mainConfig.NumberOfEntriesPerFile, GetMessageOfZeroNegative(nameof(mainConfig.NumberOfEntriesPerFile)));

            EntryProvider = entryProvider;
            NumberOfEntriesPerSitemap = mainConfig.NumberOfEntriesPerFile;
            SitemapFilePrefix = config.SitemapFilePrefix;
        }

        public IEnumerable<Models.Sitemap> GenerateSitemaps(SitemapParams sitemapParams)
        {
            if (string.IsNullOrWhiteSpace(sitemapParams.BaseUrl)) { throw new ArgumentException(GetMessageOfNullWhiteSpace(nameof(sitemapParams.BaseUrl)), nameof(sitemapParams)); }
            if (string.IsNullOrWhiteSpace(sitemapParams.Scope)) { throw new ArgumentException(GetMessageOfNullWhiteSpace(nameof(sitemapParams.Scope)), nameof(sitemapParams)); }
            if (sitemapParams.Culture == null) {throw new ArgumentException(GetMessageOfNull(nameof(sitemapParams.Culture)), nameof(sitemapParams)); }

            var iterationIndex = 1;
            var offset = 0;

            do
            {
                var entries = EntryProvider.GetEntriesAsync(
                    sitemapParams,
                    culture: sitemapParams.Culture,
                    offset: offset,
                    count: NumberOfEntriesPerSitemap
                ).Result;

                var isEntriesNotEnough = entries.Count() < NumberOfEntriesPerSitemap;

                if (entries.Any())
                {
                    yield return new Models.Sitemap
                    {
                        Name = isEntriesNotEnough && iterationIndex == 1 ? GetSitemapName(sitemapParams.Culture) : GetSitemapName(sitemapParams.Culture, iterationIndex),
                        Entries = entries.ToArray(),
                    };

                    offset += NumberOfEntriesPerSitemap;
                    iterationIndex += 1;

                    if (isEntriesNotEnough)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            while (true);
        }

        public virtual bool IsMatch(string sitemapFilename)
        {
            if (sitemapFilename == null)
            {
                return false;
            }

            // Source: http://stackoverflow.com/questions/3962543/how-can-i-validate-a-culture-code-with-a-regular-expression
            var cultureRegex = "[a-z]{2,3}(?:-[A-Z]{2,3}(?:-(?:Cyrl|Latn))?)?";

            return Regex.IsMatch(sitemapFilename, $@"sitemap-{cultureRegex}-{SitemapFilePrefix}");
        }

        protected virtual string GetSitemapName(CultureInfo culture, int index)
        {
            return $"sitemap-{culture.Name}-{SitemapFilePrefix}-{index}.xml";
        }

        protected virtual string GetSitemapName(CultureInfo culture)
        {
            return $"sitemap-{culture.Name}-{SitemapFilePrefix}.xml";
        }
    }
}
