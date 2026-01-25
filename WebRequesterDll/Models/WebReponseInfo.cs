namespace WebRequesterDll.Models
{
    public class WebReponseInfo : IWebReponseProps
    {
        public UrlModel Url { get; set; }


        public HttpReponseStatus Status { get; set; } = new();


        // public long ContentLength { get; set; } // this value is not reliable and not really needed.
        public string CharSet { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public Dictionary<string, string> ResponseHeaders { get; set; } = new();
        public Dictionary<string, string> ContentHeaders { get; set; } = new();
        public CacheInfoModel Cache { get; set; }
    }
}
