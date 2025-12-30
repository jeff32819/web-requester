using System.Text;

namespace WebRequesterDll.Models
{

    public class CharsetParser
    {
        public CharsetParser(HttpResponseMessage httpResponse)
        {
            var charset = httpResponse.Content.Headers.ContentType?.CharSet;
            if (string.IsNullOrWhiteSpace(charset))
            {
                return;
            }
            RawEncoding = charset;
            var contentType = httpResponse.Content.Headers.ContentType;
            try
            {
                Encoding = Encoding.GetEncoding(charset.Trim('"'));
            }
            catch (ArgumentException)
            {
                // Log here
            }
            // List of known invalid charsets to normalize
            var invalidCharsets = new[] { "utf8", "utf8mb4", "utf-8mb4" };
            if (!invalidCharsets.Any(c => charset.Equals(c, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
            if (contentType == null || string.IsNullOrEmpty(contentType.MediaType))
            {
                return;
            }
            var newContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType.MediaType)
            {
                CharSet = "utf-8"
            };
            httpResponse.Content.Headers.ContentType = newContentType;
            Encoding = Encoding.GetEncoding(newContentType.CharSet);
            EncodingWasFixed = true;
        }

        public bool EncodingWasFixed { get; set; }
        public bool IsValid => Encoding != null;
        public string RawEncoding { get; set; } = "";
        public Encoding? Encoding { get; set; }
    }
}
