using Jeff32819DLL.MiscCore20;

namespace WebRequesterDll.Models;

public class RequestorConfig
{
    public RequestorConfig(string baseFolder, Uri uri)
    {
        var hash = uri.ToMd5Hash();
        Cache = new CacheModel
        {
            Hash = hash,
            Folder = Path.Combine(baseFolder, uri.Host),
            Json = Path.Combine(baseFolder, uri.Host, $"{hash}.json"),
            Html = Path.Combine(baseFolder, uri.Host, $"{hash}.html")
        };
        Directory.CreateDirectory(Cache.Folder);
        StartUri = uri;
    }
    public Uri StartUri { get; }
    public CacheModel Cache { get; }

    public class CacheModel
    {
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