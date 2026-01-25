using System.Net;

namespace WebRequesterDll.Models
{
    public class HttpReponseResult
    {

        public string ErrorMessage { get; set; } = "";
        public HttpStatusCode? HttpStatusCode { get; set; }
        public HttpErrorCodeEnum ErrorCode { get; set; } = HttpErrorCodeEnum.None;
        public enum HttpErrorCodeEnum
        {
            None,
            DnsFailure,
            Timeout,
            ConnectionError,
            SslError,
            HttpError,
            Unexpected,
            Unknown
        }
    }
}
