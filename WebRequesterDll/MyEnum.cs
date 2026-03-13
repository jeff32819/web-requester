namespace WebRequesterDll
{
    public static class MyEnum
    {
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
