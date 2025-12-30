namespace WebRequesterDll.Models
{
    public interface IWebReponseProps
    {


        /// <summary>
        /// Starting url
        /// </summary>
        string StartUrl { get; set; }

        /// <summary>
        /// Final url after redirects (if any)
        /// </summary>
        string? FinalUrl { get; set; }

        /// <summary>
        /// Will compare start url from final url and indicate if there was a redirect.
        /// This will work with either GetFromWeb or GetFromWebWithRedirects
        /// </summary>
        bool IsRedirected { get; }

        /// <summary>
        /// If called by GetFromWebWithRedirects method, contains the full redirect chain.
        /// </summary>
        List<string> RedirectChain { get; set; }
        /// <summary>
        /// If the content is saved to a file, this is the path to that file.
        /// </summary>
        string PathToContent { get; set; }

        long ContentLength { get; }
        int StatusCode { get; }

        /// <summary>
        /// Maybe use later, there are some sites that have a invalid charset that need to be fixed
        /// </summary>
        //CharsetParser? CharsetParsed { get; set; } 




        string CharSet { get; }
        string MediaType { get; }
        public Dictionary<string, string> ResponseHeaders { get; }
        public Dictionary<string, string> ContentHeaders { get; }
    }
}
