using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WebRequesterDll;

public class BulkDomainAuditor
{
    private static readonly HttpClient _scraperClient;

    static BulkDomainAuditor()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _scraperClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(6)
        };

        // --- BYPASS FIREWALLS: Mimic a real desktop Chrome browser ---
        _scraperClient.DefaultRequestHeaders.Clear();
        _scraperClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _scraperClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        _scraperClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
        _scraperClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
    }

    public static async Task<DomainAuditReport> AuditDomainAsync(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return null;
        }

        // 1. STRIP ALL INVISIBLE AND WRONG CHARACTERS (including \u00A0 and tabs)
        var sanitized = Regex.Replace(rawInput, @"[^\x20-\x7E]", ""); // Removes non-ASCII
        sanitized = sanitized.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");

        // 2. Extract base host name cleanly
        var baseDomain = sanitized.Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("www.", "", StringComparison.OrdinalIgnoreCase)
            .Split('/')[0]
            .Trim();

        if (string.IsNullOrWhiteSpace(baseDomain))
        {
            Console.WriteLine("❌ Error: Sanitized domain resulted in an empty string.");
            return null;
        }

        var report = new DomainAuditReport { BaseDomain = baseDomain };

        var variants = new[]
        {
            $"http://{baseDomain}/",
            $"https://{baseDomain}/",
            $"http://www.{baseDomain}/",
            $"https://www.{baseDomain}/"
        };

        foreach (var url in variants)
        {
            var trace = await TraceUrlAsync(url);
            report.Traces.Add(trace);

            if (!trace.IsUnreachable && trace.FinalStatusCode == 200 && !string.IsNullOrEmpty(trace.FinalResolvedUrl))
            {
                var normalizedDest = trace.FinalResolvedUrl.ToLower().TrimEnd('/');
                if (!report.UniqueDestinations.Contains(normalizedDest))
                {
                    report.UniqueDestinations.Add(normalizedDest);
                }
            }
        }

        if (report.UniqueDestinations.Count > 1)
        {
            report.HasCanonicalizationIssue = true;
        }

        report.DiscoveredPrimaryUrl = report.UniqueDestinations.FirstOrDefault();

        if (!string.IsNullOrEmpty(report.DiscoveredPrimaryUrl))
        {
            await AuditHtmlCanonicalTagAsync(report);
        }

        return report;
    }

    private static async Task<EndpointTrace> TraceUrlAsync(string url)
    {
        var trace = new EndpointTrace { StartingUrl = url };
        var currentUrl = url;
        const int maxHops = 6; // Safety circuit-breaker

        try
        {
            while (trace.HopCount < maxHops)
            {
                using var response = await _scraperClient.GetAsync(currentUrl);
                trace.FinalStatusCode = (int)response.StatusCode;

                // Handle all types of redirects (301, 302, 307, 308)
                if (response.StatusCode == HttpStatusCode.MovedPermanently ||
                    response.StatusCode == HttpStatusCode.Found ||
                    response.StatusCode == (HttpStatusCode)307 ||
                    response.StatusCode == (HttpStatusCode)308)
                {
                    trace.HopCount++;
                    var nextUrl = response.Headers.Location?.ToString();

                    if (string.IsNullOrEmpty(nextUrl) || nextUrl == currentUrl)
                    {
                        break;
                    }

                    // Fix relative URLs
                    if (!nextUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        nextUrl = new Uri(new Uri(currentUrl), nextUrl).ToString();
                    }

                    currentUrl = nextUrl;
                }
                else
                {
                    trace.FinalResolvedUrl = currentUrl;
                    break;
                }
            }

            if (trace.HopCount >= maxHops)
            {
                trace.IsUnreachable = true;
                trace.ErrorMessage = "Infinite redirect loop detected.";
            }
        }
        catch (Exception ex)
        {
            trace.IsUnreachable = true;
            trace.ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            trace.FinalResolvedUrl = currentUrl; // Keep track of where it died
        }

        return trace;
    }

    private static async Task AuditHtmlCanonicalTagAsync(DomainAuditReport report)
    {
        try
        {
            var html = await _scraperClient.GetStringAsync(report.DiscoveredPrimaryUrl);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var canonicalNode = doc.DocumentNode.SelectSingleNode("//link[@rel='canonical']");

            if (canonicalNode != null)
            {
                var hrefValue = canonicalNode.GetAttributeValue("href", "")?.Trim();
                report.HtmlCanonicalTagValue = hrefValue;

                if (!string.IsNullOrEmpty(hrefValue))
                {
                    var normalizedTag = hrefValue.ToLower().TrimEnd('/');
                    var normalizedDest = report.DiscoveredPrimaryUrl.ToLower().TrimEnd('/');

                    if (normalizedTag == normalizedDest)
                    {
                        report.CanonicalTagMatchesDestination = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            report.HtmlCanonicalTagValue = $"Error parsing tag: {ex.Message}";
        }
    }
}

public class DomainAuditReport
{
    public string BaseDomain { get; set; }
    public bool HasCanonicalizationIssue { get; set; }
    public string DiscoveredPrimaryUrl { get; set; }
    public List<string> UniqueDestinations { get; set; } = new();
    public List<EndpointTrace> Traces { get; set; } = new();
    public string HtmlCanonicalTagValue { get; set; }
    public bool CanonicalTagMatchesDestination { get; set; }
}

public class EndpointTrace
{
    public string StartingUrl { get; set; }
    public string FinalResolvedUrl { get; set; }
    public int HopCount { get; set; }
    public int FinalStatusCode { get; set; }
    public bool IsUnreachable { get; set; }
    public string ErrorMessage { get; set; }
}