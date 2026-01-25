using System.Net;
// ReSharper disable UnusedMember.Global

namespace WebRequesterDll
{
    public static class Ext
    {
        // string ? int (safe)
        public static int ToHttpStatusCode(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return -1;

            // Try parse numeric string
            if (int.TryParse(value, out var numeric))
            {
                return Enum.IsDefined(typeof(HttpStatusCode), numeric)
                    ? numeric
                    : -1;
            }

            // Try parse enum name
            if (Enum.TryParse<HttpStatusCode>(value, ignoreCase: true, out var code))
            {
                return (int)code;
            }

            return -1;
        }

        // int ? string (safe)
        public static string ToHttpStatusCode(this int value)
        {
            return Enum.IsDefined(typeof(HttpStatusCode), value) ? ((HttpStatusCode)value).ToString() : "unknown";
        }
    }
}
