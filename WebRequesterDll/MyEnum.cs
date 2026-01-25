namespace WebRequesterDll
{
    public class MyEnum
    {
        public enum CacheMode
        {
            ForceRefresh,
            UseCacheIfExists
        }

        public enum RequestErrorCodeEnum
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
