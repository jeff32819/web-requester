using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace WebRequesterDll.Models
{
    /// <summary>
    /// Parser, Dictionary and Container for Response Headers
    /// </summary>
    public class ResponseHeaderContainer
    {
        #region constructors

        /// <summary>
        /// Constructor for response headers, passing in JSON from database
        /// </summary>
        /// <param name="jsonFromDatabase"></param>
        public ResponseHeaderContainer(string jsonFromDatabase)
        {
            var tmp = JsonConvert.DeserializeObject<Dictionary<string, IEnumerable<string>>>(jsonFromDatabase);
            if (tmp == null)
            {
                return;
            }
            Dictionary = tmp;
        }
        /// <summary>
        /// constructor for response headers, passing in HttpResponseHeaders
        /// </summary>
        /// <param name="responseHeaders"></param>
        public ResponseHeaderContainer(HttpResponseHeaders? responseHeaders)
        {
            if (responseHeaders == null)
                return;

            foreach (var item in responseHeaders)
            {
                Dictionary.Add(item.Key, item.Value is List<string> list ? list : item.Value.ToList());
            }
        }

        /// <summary>
        /// Constructor for content headers, but passing in HttpContentHeaders
        /// </summary>
        /// <param name="contentHeaders"></param>
        public ResponseHeaderContainer(HttpContentHeaders? contentHeaders)
        {
            if (contentHeaders == null)
                return;

            foreach (var item in contentHeaders)
            {
                Dictionary.Add(item.Key, item.Value is List<string> list ? list : item.Value.ToList());
            }
        }

        #endregion

        /// <summary>
        /// Convert the internal dictionary to JSON for storage or logging
        /// </summary>
        public string ToJson => JsonConvert.SerializeObject(Dictionary, Formatting.None);

        /// <summary>
        /// Dictionary of headers
        /// </summary>
        public readonly Dictionary<string, IEnumerable<string>> Dictionary = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Print to console for debugging
        /// </summary>
        public void PrintToConsole()
        {
            foreach (var item in Dictionary)
            {
                Console.WriteLine(item.Key);
                foreach (var val in item.Value)
                {
                    Console.WriteLine($"\t- {val}");
                }
            }
        }

    }
}
