namespace WebRequesterDll.Models
{
    public class UrlModel
    {
        /// <summary>
        /// Starting url
        /// </summary>
        public required string Start { get; set; }

        /// <summary>
        /// Final url after redirects (if any)
        /// </summary>
        public string Final { get; set; } = string.Empty;
        /// <summary>
        /// Will compare start url from final url and indicate if there was a redirect. This will work with either GetFromWeb or GetFromWebWithRedirects
        /// </summary>
        public bool IsRedirected => Start != Final;
        /// <summary>
        /// If called by GetFromWebWithRedirects method, contains the full redirect chain.
        /// </summary>
        public List<string> RedirectChain { get; set; } = new();
    }
}
