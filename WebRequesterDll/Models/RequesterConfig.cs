using Jeff32819DLL.MiscCore20;

namespace WebRequesterDll.Models;

public class RequesterConfig
{
    public RequesterConfig(string baseCacheFolder, Uri uri)
    {
        StartUri = uri;
    }
    public Uri StartUri { get; }
}