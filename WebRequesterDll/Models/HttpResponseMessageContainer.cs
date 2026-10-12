namespace WebRequesterDll.Models
{
    public class HttpResponseMsg
    {
        public HttpResponseMessage? ResponseMessage { get; set; }
        public HttpResponseStatus ResponseStatus { get; set; } = new();
    }
}
