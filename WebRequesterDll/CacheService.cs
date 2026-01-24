using Jeff32819DLL.MiscCore20;
using Newtonsoft.Json;
using WebRequesterDll.Models;

namespace WebRequesterDll;

public class CacheService
{
    /// <summary>
    ///     Cache file config constructor
    /// </summary>
    /// <param name="url"></param>
    /// <param name="cacheFolder"></param>
    public CacheService(string url, string cacheFolder)
    {
        if (!Directory.Exists(cacheFolder))
        {
            throw new Exception("Cache folder does not exist");
        }

        var uri = new Uri(url);
        var basePath = Path.Combine(cacheFolder, uri.Host);
        Directory.CreateDirectory(basePath);
        var hash = url.ToMd5Hash();
        CachInfo = new CachInfoModel
        {
            Hash = hash,
            JsonPath = Path.Combine(basePath, $"{hash}.json"),
            HtmlPath = Path.Combine(basePath, $"{hash}.html")
        };
    }

    public CachInfoModel CachInfo { get; }
    
    /// <summary>
    ///     Save json object to file
    /// </summary>
    /// <param name="request"></param>
    public void Save(WebResponseResult request)
    {
        File.WriteAllText(CachInfo.HtmlPath, request.Content);
        File.WriteAllText(CachInfo.JsonPath, JsonConvert.SerializeObject(request.Properties, Formatting.Indented));
    }

    /// <summary>
    ///     Get the file content
    /// </summary>
    /// <returns></returns>
    public WebResponseResult Read()
    {
        var properties = JsonConvert.DeserializeObject<WebReponseProps>(File.ReadAllText(CachInfo.JsonPath));
        if (properties == null)
        {
            throw new Exception("Failed to deserialize cache properties from JSON file");
        }

        return new WebResponseResult
        {
            IsCached = true,
            Content = File.ReadAllText(CachInfo.HtmlPath),
            Properties = properties
        };
    }

    /// <summary>
    ///     Do all files exist?
    /// </summary>
    /// <returns></returns>
    public bool Exists()
    {
        return File.Exists(CachInfo.HtmlPath) && File.Exists(CachInfo.JsonPath);
    }
}