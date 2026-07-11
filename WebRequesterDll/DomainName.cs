namespace WebRequesterDll;

public static class DomainName
{
    /// <summary>
    ///     Safely attempts to extract the host from a URL string without throwing exceptions.
    /// </summary>
    /// <param name="url">The raw URL or domain string to parse.</param>
    /// <param name="domain">When successful, contains the host name; otherwise, string.Empty.</param>
    /// <returns>True if a host was successfully extracted; otherwise, false.</returns>
    public static bool TryParse(string url, out string domain)
    {
        domain = string.Empty;

        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }
        // Normalize if missing a scheme so raw domains don't fail parsing
        var urlToParse = url;
        if (!url.Contains("://") && !url.StartsWith("//"))
        {
            urlToParse = $"https://{url}";
        }
        // Use TryCreate to avoid hidden exception allocations under the hood
        if (!Uri.TryCreate(urlToParse, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            return false;
        }
        domain = uri.Host;
        return true;

    }
}