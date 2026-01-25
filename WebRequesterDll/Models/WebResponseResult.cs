namespace WebRequesterDll.Models;

public class WebResponseResult : IWebResponseResult
{
    public bool IsCached { get; set; }

    public string Content { get; set; } = "";
    public WebReponseInfo Info { get; set; } = null!;
}