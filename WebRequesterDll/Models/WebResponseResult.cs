namespace WebRequesterDll.Models;

public class WebResponseResult : IWebResponseResult
{
    public bool IsCached { get; set; }

    public string Content { get; set; } = "";
    public WebResponseInfo Info { get; set; } = null!;
}