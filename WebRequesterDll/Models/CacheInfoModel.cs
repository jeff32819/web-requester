namespace WebRequesterDll.Models
{
    public class CacheInfoModel
    {
        /// <summary>
        ///     Has for url
        /// </summary>
        public string Hash { get; set; } = "";

        /// <summary>
        ///     JSON file path
        /// </summary>
        public string JsonPath { get; set; } = "";

        /// <summary>
        ///     HTML file path
        /// </summary>
        public string HtmlPath { get; set; } = "";
    }
}
