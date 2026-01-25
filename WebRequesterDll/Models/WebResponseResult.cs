using WebRequestDll.Models;

namespace WebRequesterDll.Models;

public class WebResponseResult : IWebResponseResult
{
    public bool IsCached { get; set; }
    public HttpReponseStatus ResponseStatus { get; set; }
    public string Content { get; set; } = "";
    public WebReponseProps Properties { get; set; } = null!;
}