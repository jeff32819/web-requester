using WebRequestDll.Models;

namespace WebRequesterDll.Models;

public class WebResponseResult : IWebResponseResult
{
    public string Content { get; set; }
    public WebReponseProps Properties { get; set; } = null!;
}