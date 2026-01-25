using WebRequestDll.Models;

namespace WebRequesterDll.Models;

public class WebResponseResult : IWebResponseResult
{
    public bool IsCached { get; set; }
    public string Content { get; set; } = "";
    public WebReponseProps Properties { get; set; } = null!;
    public HttpReponseResult HttpResponse { get; set; } = null!;
}