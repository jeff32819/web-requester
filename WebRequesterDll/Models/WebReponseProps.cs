using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WebRequesterDll.Models
{
    public class WebReponseProps : IWebReponseProps
    {
        /// <summary>
        /// Starting url
        /// </summary>
        public required string StartUrl { get; set; }

        /// <summary>
        /// Final url after redirects (if any)
        /// </summary>
        public string FinalUrl { get; set; } = string.Empty;
        public Uri? FinalUri => string.IsNullOrEmpty(FinalUrl) ? null : new Uri(FinalUrl);
        /// <summary>
        /// Will compare start url from final url and indicate if there was a redirect. This will work with either GetFromWeb or GetFromWebWithRedirects
        /// </summary>
        public bool IsRedirected => StartUrl != FinalUrl;
        /// <summary>
        /// If called by GetFromWebWithRedirects method, contains the full redirect chain.
        /// </summary>
        public List<string> RedirectChain { get; set; } = new();

        public long ContentLength { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public HttpStatusCode? StatusCodeEnum { get; set; }
        public string CharSet { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public Dictionary<string, string> ResponseHeaders { get; set; } = new();
        public Dictionary<string, string> ContentHeaders { get; set; } = new();
        public CachInfoModel? CachInfo { get; set; }
    }
}
