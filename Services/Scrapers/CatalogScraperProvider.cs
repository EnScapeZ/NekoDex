using NekoDex.Core.Enums;
using NekoDex.Core.Models;
using AppSettings = NekoDex.AppSettings;

namespace NekoDex.Services.Scrapers
{
    /// <summary>
    /// Bindet einen bereits suchbaren Katalog an <see cref="IScraperProvider"/>,
    /// ohne den bestehenden Suchpfad in <c>MainWindow</c> zu ersetzen.
    /// </summary>
    public abstract class CatalogScraperProvider : IScraperProvider
    {
        protected abstract string SiteId { get; }

        public abstract SourceProvider Provider { get; }

        public abstract string BaseUrl { get; }

        public virtual bool SupportsTagSearch => true;

        public virtual string BuildSearchUrl(string query, int page) =>
            NekoDex.ApiSearchService.BuildSearchUrl(SiteId, query ?? string.Empty, Math.Max(1, page));

        public virtual async Task<IEnumerable<GalleryResult>> SearchAsync(
            string query,
            int page,
            AppSettings settings,
            CancellationToken cancellationToken = default)
        {
            using var api = new NekoDex.ApiSearchService();
            api.ExtraArgs = settings.ExtraArgs;
            api.FlareSolverrUrl = string.IsNullOrWhiteSpace(settings.FlareSolverrUrl) ? null : settings.FlareSolverrUrl;
            api.WorkingDirectory = string.IsNullOrWhiteSpace(settings.LibraryPath) ? null : settings.LibraryPath;
            api.LoadCookies(SessionCookies(settings));

            var hits = new List<GalleryResult>();
            await foreach (var hit in api.SearchStreamAsync(
                SiteId,
                Array.Empty<string>(),
                query,
                Math.Max(1, page),
                cancellationToken))
            {
                hits.Add(new GalleryResult
                {
                    Title = hit.Title,
                    CoverUrl = hit.CoverUrl,
                    GalleryUrl = hit.GalleryUrl,
                    GalleryId = hit.GalleryId,
                    PageCount = hit.PageCount,
                    Tags = hit.Tags,
                    SourceSite = hit.SourceSite,
                });
            }

            return hits;
        }

        private static IEnumerable<NekoDex.StoredHttpCookie> SessionCookies(AppSettings settings)
        {
            var cookies = new List<NekoDex.StoredHttpCookie>();
            Add(cookies, "e-hentai.org", "ipb_member_id", settings.EHentaiIpbMemberId);
            Add(cookies, "e-hentai.org", "ipb_pass_hash", settings.EHentaiIpbPassHash);
            Add(cookies, "e-hentai.org", "cf_clearance", settings.EHentaiCfClearance);
            Add(cookies, "exhentai.org", "ipb_member_id", settings.EHentaiIpbMemberId);
            Add(cookies, "exhentai.org", "ipb_pass_hash", settings.EHentaiIpbPassHash);
            Add(cookies, "exhentai.org", "cf_clearance", settings.EHentaiCfClearance);
            return cookies;
        }

        private static void Add(List<NekoDex.StoredHttpCookie> cookies, string domain, string name, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            cookies.Add(new NekoDex.StoredHttpCookie(domain, name, value.Trim(), "/"));
        }
    }
}
