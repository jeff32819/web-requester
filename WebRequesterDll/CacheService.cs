using Newtonsoft.Json;

using WebRequesterDll.Models;

namespace WebRequesterDll;

public class CacheService
{
    /// <summary>
    ///     Cache file config constructor
    /// </summary>
    /// <param name="requestorConfig"></param>
    public CacheService(RequestorConfig requestorConfig)
    {
        try
        {
            Directory.CreateDirectory(requestorConfig.Cache.Folder);
        }
        catch (Exception ex)
        {
            throw new Exception($"Cannot create folder {requestorConfig.Cache.Folder}", ex);
        }
        RequestorConfig = requestorConfig;
    }

    public RequestorConfig RequestorConfig { get; }

    /// <summary>
    ///     Save json object to file
    /// </summary>
    /// <param name="request"></param>0
    public void Save(WebResponseResult request)
    {
        File.WriteAllText(RequestorConfig.Cache.Html, request.Content);
        File.WriteAllText(RequestorConfig.Cache.Json, JsonConvert.SerializeObject(request.Info, Formatting.Indented));
    }

    /// <summary>
    ///     Get the file content
    /// </summary>
    /// <returns></returns>
    public WebResponseResult Read()
    {
        var info = JsonConvert.DeserializeObject<WebResponseInfo>(File.ReadAllText(RequestorConfig.Cache.Json));
        if (info == null)
        {
            throw new Exception("Failed to deserialize cache properties from JSON file");
        }

        return new WebResponseResult
        {
            IsCached = true,
            Content = File.ReadAllText(RequestorConfig.Cache.Html),
            Info = info
        };
    }

    /// <summary>
    ///     Do all files exist?
    /// </summary>
    /// <returns></returns>
    public bool Exists()
    {
        return File.Exists(RequestorConfig.Cache.Html) && File.Exists(RequestorConfig.Cache.Json);
    }
}