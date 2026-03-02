using Jeff32819DLL;
using Newtonsoft.Json;
using WebRequesterDll.Models;

namespace WebRequesterDll;

public class CacheService
{
    /// <summary>
    ///     Cache file config constructor
    /// </summary>
    /// <param name="requestorConfig"></param>
    public CacheService(RequesterConfig requestorConfig, JLog.FileLogger log)
    {
        try
        {
            Directory.CreateDirectory(requestorConfig.Cache.Folder);
        }
        catch (Exception ex)
        {
            throw new Exception($"Cannot create folder {requestorConfig.Cache.Folder}", ex);
        }

        RequesterConfig = requestorConfig;
        Log = log;
    }

    public JLog.FileLogger Log { get; }
    public RequesterConfig RequesterConfig { get; }

    /// <summary>
    ///     Save json object to file
    /// </summary>
    /// <param name="request"></param>
    /// 0
    public void Save(WebResponseResult request)
    {
        Log.Write($"Saving HTML to: {RequesterConfig.Cache.Html}");
        Log.Write($"Saving JSON to: {RequesterConfig.Cache.Json}");

        File.WriteAllText(RequesterConfig.Cache.Html, request.Content);
        File.WriteAllText(RequesterConfig.Cache.Json, JsonConvert.SerializeObject(request.Info, Formatting.Indented));
    }

    /// <summary>
    ///     Get the file content
    /// </summary>
    /// <returns></returns>
    public WebResponseResult Read()
    {
        Log.Write($"Parsing JSON from: {RequesterConfig.Cache.Json}");
        var info = JsonConvert.DeserializeObject<WebResponseInfo>(File.ReadAllText(RequesterConfig.Cache.Json));
        Log.Write($"Successfully parsed JSON from: {RequesterConfig.Cache.Json}");
        if (info != null)
        {
            return new WebResponseResult
            {
                IsCached = true,
                Content = File.ReadAllText(RequesterConfig.Cache.Html),
                Info = info
            };
        }
        Log.Write($"Info - parsed json is null");
        throw new Exception("Failed to deserialize cache properties from JSON file");
    }

    /// <summary>
    ///     Do all files exist?
    /// </summary>
    /// <returns></returns>
    public bool Exists()
    {
        return File.Exists(RequesterConfig.Cache.Html) && File.Exists(RequesterConfig.Cache.Json);
    }
}