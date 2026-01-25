namespace WebRequesterDll
{
    public class MyEnum
    {
        public enum CacheMode
        {
            UseCacheIfExists = 0,
            ForceRefresh = 1
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
