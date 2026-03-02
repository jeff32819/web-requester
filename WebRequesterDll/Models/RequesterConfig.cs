using Jeff32819DLL.MiscCore20;

namespace WebRequesterDll.Models;

public class RequesterConfig
{
    public RequesterConfig(string baseCacheFolder, Uri uri)
    {
        var hash = uri.ToMd5Hash();
        Cache = new CacheModel(baseCacheFolder, uri);
        Directory.CreateDirectory(Cache.Folder);
        StartUri = uri;
    }
    public Uri StartUri { get; }
    public CacheModel Cache { get; }

    public class CacheModel
    {
        public CacheModel(string baseCacheFolder, Uri uri)
        {
            var hash = uri.ToMd5Hash();
            Hash = hash;
            Folder = Path.Combine(baseCacheFolder, uri.Host);
            Json = Path.Combine(baseCacheFolder, uri.Host, $"{hash}.json");
            Html = Path.Combine(baseCacheFolder, uri.Host, $"{hash}.html");
        }
        public string Folder { get; set; }

        /// <summary>
        ///     Has for url
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        ///     JSON file path
        /// </summary>
        public string Json { get; set; }

        /// <summary>
        ///     HTML file path
        /// </summary>
        public string Html { get; set; }
    }
}