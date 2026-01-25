namespace WebRequesterDll.Models
{
    public interface IWebReponseProps
    {
        string CharSet { get; }
        string MediaType { get; }
        public Dictionary<string, string> ResponseHeaders { get; }
        public Dictionary<string, string> ContentHeaders { get; }
    }
}
