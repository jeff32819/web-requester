using System.Text;

namespace WebRequesterDll.Models
{

    public class CharsetParser
    {
        private static readonly HashSet<string> s_invalidCharsets = new(StringComparer.OrdinalIgnoreCase)
        {
            "utf8",
            "utf8mb4",
            "utf-8mb4"
        };

        public CharsetParser(HttpResponseMessage httpResponse)
        {
            var contentType = httpResponse.Content.Headers.ContentType;
            var charset = contentType?.CharSet;

            if (string.IsNullOrWhiteSpace(charset))
            {
                return;
            }

            RawEncoding = charset;
            var cleanCharset = charset.Trim('"');

            try
            {
                Encoding = Encoding.GetEncoding(cleanCharset);
            }
            catch (ArgumentException)
            {
                // Log here: The initial charset was not recognized by the system.
            }

            // Normalize known non-standard charsets to "utf-8".
            if (!s_invalidCharsets.Contains(cleanCharset))
            {
                return;
            }

            if (string.IsNullOrEmpty(contentType?.MediaType))
            {
                return;
            }

            var newContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType.MediaType)
            {
                CharSet = "utf-8"
            };

            httpResponse.Content.Headers.ContentType = newContentType;
            Encoding = Encoding.UTF8;
            EncodingWasFixed = true;
        }

        public bool EncodingWasFixed { get; set; }
        public bool IsValid => Encoding != null;
        public string RawEncoding { get; set; } = "";
        public Encoding? Encoding { get; set; }
    }
}
