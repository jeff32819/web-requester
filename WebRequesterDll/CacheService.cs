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
    /// <param name="cacheMode"></param>
    public CacheService(string url, string cacheFolder, MyEnum.CacheMode cacheMode)
    {
        if (!Directory.Exists(cacheFolder))
        {
            throw new Exception("Cache folder does not exist");
        }
        try
        {
            Directory.CreateDirectory(cacheFolder);
        }
        catch (Exception ex)
        {
            throw new Exception($"Cannot create folder {cacheFolder}", ex);
        }
        var uri = new Uri(url);
        var basePath = Path.Combine(cacheFolder, uri.Host);
        Directory.CreateDirectory(basePath);
        var hash = url.ToMd5Hash();
        CacheInfo = new CacheInfoModel
        {
            Hash = hash,
            JsonPath = Path.Combine(basePath, $"{hash}.json"),
            HtmlPath = Path.Combine(basePath, $"{hash}.html")
        };
    }
    public MyEnum.CacheMode CacheMode { get; set; }
    public CacheInfoModel CacheInfo { get; }
    
    /// <summary>
    ///     Save json object to file
    /// </summary>
    /// <param name="request"></param>
    public void Save(WebResponseResult request)
    {
        File.WriteAllText(CacheInfo.HtmlPath, request.Content);
        File.WriteAllText(CacheInfo.JsonPath, JsonConvert.SerializeObject(request.Info, Formatting.Indented));
    }

    /// <summary>
    ///     Get the file content
    /// </summary>
    /// <returns></returns>
    public WebResponseResult Read()
    {
        var info = JsonConvert.DeserializeObject<WebResponseInfo>(File.ReadAllText(CacheInfo.JsonPath));
        if (info == null)
        {
            throw new Exception("Failed to deserialize cache properties from JSON file");
        }

        return new WebResponseResult
        {
            IsCached = true,
            Content = File.ReadAllText(CacheInfo.HtmlPath),
            Info = info
        };
    }

    /// <summary>
    ///     Do all files exist?
    /// </summary>
    /// <returns></returns>
    public bool Exists()
    {
        return File.Exists(CacheInfo.HtmlPath) && File.Exists(CacheInfo.JsonPath);
    }
}