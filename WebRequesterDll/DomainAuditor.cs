using System.Net;
using System.Net.Security;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WebRequesterDll;

public static class DomainAuditor
{
    private const string SslExpiryKey = "SslExpiry";
    private static readonly HttpClient _scraperClient;

    static DomainAuditor()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (message.RequestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // 1. Verify if the certificate has policy issues (expired, host mismatch, untrusted root)
                        var isValid = errors == SslPolicyErrors.None;

                        if (isValid && cert != null && DateTime.TryParse(cert.GetExpirationDateString(), out var expiry))
                        {
                            // PASS: Store the valid timestamp
                            message.Options.Set(new HttpRequestOptionsKey<DateTime>(SslExpiryKey), expiry);
                        }
                        else
                        {
                            // FAIL: Pass MinValue as a sentinel flag meaning "SSL exists but is broken/untrusted"
                            message.Options.Set(new HttpRequestOptionsKey<DateTime>(SslExpiryKey), DateTime.MinValue);
                        }
                    }
                    catch
                    {
                        // Fail-safe protection fallback
                        message.Options.Set(new HttpRequestOptionsKey<DateTime>(SslExpiryKey), DateTime.MinValue);
                    }
                }

                return true; // Return true to prevent HttpClient from throwing a terminal WebException
            }
        };

        _scraperClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(6) };
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

        // 1. Strip out non-printable ASCII/control characters if your raw inputs have junk data
        var sanitizedInput = Regex.Replace(rawInput, @"[^\x20-\x7E]", "").Trim();

        // 2. Use the clean TryParse pattern to safely extract the host root
        if (!DomainName.TryParse(sanitizedInput, out var extractedHost))
        {
            Console.WriteLine($"❌ Error: Could not parse a valid domain host from raw input: '{rawInput}'");
            return null;
        }

        // 3. (Optional) Strip "www." if your engine treats "www.domain.com" and "domain.com" as the same base identity
        var baseDomain = extractedHost.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? extractedHost.Substring(4)
            : extractedHost;

        if (string.IsNullOrWhiteSpace(baseDomain))
        {
            Console.WriteLine("❌ Error: Sanitized domain resulted in an empty string.");
            return null;
        }

        // Create the report with our pristine base domain name
        var report = new DomainAuditReport { BaseDomain = baseDomain };

        // This remains perfectly safe and predictable now
        var variants = new[]
        {
            $"http://{baseDomain}/",
            $"https://{baseDomain}/",
            $"http://www.{baseDomain}/",
            $"https://www.{baseDomain}/"
        };

        // --- CONCURRENT NETWORK TRACE ---

        // 1. Kick off all 4 network trace tasks at the exact same time
        var traceTasks = variants.Select(TraceUrlAsync).ToList();

        // 2. Await them all concurrently
        var completedTraces = await Task.WhenAll(traceTasks);

        // 3. Process the results together once they are all finished
        foreach (var trace in completedTraces)
        {
            report.Traces.Add(trace);

            if (trace is not { IsUnreachable: false, FinalStatusCode: 200 } || string.IsNullOrEmpty(trace.FinalResolvedUrl))
            {
                continue;
            }

            var normalizedDest = trace.FinalResolvedUrl.ToLower().TrimEnd('/');
            if (!report.UniqueDestinations.Contains(normalizedDest))
            {
                report.UniqueDestinations.Add(normalizedDest);
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
        const int maxHops = 6;

        try
        {
            while (trace.HopCount < maxHops)
            {
                using var response = await _scraperClient.GetAsync(currentUrl);
                trace.FinalStatusCode = (int)response.StatusCode;

                // Extract the SSL value captured during the handshake layer
                if (response.RequestMessage.Options.TryGetValue(new HttpRequestOptionsKey<DateTime>(SslExpiryKey), out var expiryDate))
                {
                    trace.SslExpirationDate = expiryDate;
                }

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
            trace.FinalResolvedUrl = currentUrl;

            // Handle complete connection/DNS dropouts (Status 0 errors)
            if (currentUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && trace.SslExpirationDate == null)
            {
                // If an HTTPS trace crashed out at socket level before dropping options, it failed SSL handshake/reachability entirely
                trace.SslExpirationDate = DateTime.MinValue;
            }
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
    public EndpointTrace SslExpirationTrace { get; set; }
    public string BaseDomain { get; set; }
    public bool HasCanonicalizationIssue { get; set; }
    public string DiscoveredPrimaryUrl { get; set; }
    public List<string> UniqueDestinations { get; set; } = new();
    public List<EndpointTrace> Traces { get; set; } = new();
    public string HtmlCanonicalTagValue { get; set; }
    public bool CanonicalTagMatchesDestination { get; set; }


    /// <summary>
    ///     Returns unified SSL health data ONLY if there is exactly 1 unique destination.
    ///     Returns null if there are canonicalization splits or zero site resolution.
    /// </summary>
    public ConsolidatedSslDetails ConsolidatedSsl
    {
        get
        {
            if (UniqueDestinations.Count != 1 || string.IsNullOrEmpty(DiscoveredPrimaryUrl))
            {
                return null; // Grouped atomic nullability state
            }

            // Locate the exact trace path that resolved to the single primary winning endpoint
            var primaryTrace = Traces.FirstOrDefault(t =>
                t.FinalResolvedUrl != null &&
                t.FinalResolvedUrl.ToLower().TrimEnd('/') == DiscoveredPrimaryUrl.ToLower().TrimEnd('/'));

            if (primaryTrace == null)
            {
                return null;
            }

            return new ConsolidatedSslDetails
            {
                DomainName = !string.IsNullOrEmpty(primaryTrace.FinalResolvedUrl)
                    ? new Uri(primaryTrace.FinalResolvedUrl).Host
                    : BaseDomain,
                IsSslValid = primaryTrace.IsSslValid,
                ExpirationDate = primaryTrace.SslExpirationDate,
                ExpiresInDays = primaryTrace.SslExpirationDate == null || primaryTrace.SslExpirationDate == DateTime.MinValue
                    ? null
                    : (int)(primaryTrace.SslExpirationDate.Value - DateTime.UtcNow).TotalDays
            };
        }
    }

    // =========================================================================
    // NEW: ENCAPSULATED HTML CANONICAL CONTAINER
    // =========================================================================

    /// <summary>
    ///     Gets the encapsulated evaluation and message details for the HTML Canonical tag check.
    /// </summary>
    public HtmlCanonicalDetails HtmlCanonical => new(
        DiscoveredPrimaryUrl,
        HtmlCanonicalTagValue,
        CanonicalTagMatchesDestination
    );
}

public class HtmlCanonicalDetails
{
    // Constructor handles the logic cleanly upon instantiation
    public HtmlCanonicalDetails(string discoveredPrimaryUrl, string htmlCanonicalTagValue, bool canonicalTagMatchesDestination)
    {
        // 1. Calculate the evaluation status
        if (string.IsNullOrEmpty(discoveredPrimaryUrl))
        {
            Evaluation = CanonicalTagStatus.NoSiteResolution;
        }
        else if (string.IsNullOrEmpty(htmlCanonicalTagValue))
        {
            Evaluation = CanonicalTagStatus.Missing;
        }
        else
        {
            Evaluation = canonicalTagMatchesDestination
                ? CanonicalTagStatus.Match
                : CanonicalTagStatus.Mismatch;
        }

        // 2. Generate the message
        Message = Evaluation switch
        {
            CanonicalTagStatus.NoSiteResolution => "FAIL: Could not establish a secure connection to any domain variant. HTML verification skipped.",
            CanonicalTagStatus.Missing => "WARNING: Missing HTML canonical tag entirely. If server redirects fail, duplicate content will occur.",
            CanonicalTagStatus.Match => "PASS: The HTML canonical tag perfectly matches the target endpoint.",
            CanonicalTagStatus.Mismatch => $"CRITICAL MISMATCH: The HTML tag points to '{htmlCanonicalTagValue}' but the browser landed on '{discoveredPrimaryUrl}'. This heavily confuses search indexers!",
            _ => "Unknown canonical verification status state encountered."
        };
    }

    // Expose the raw evaluated values for the API/Database
    public CanonicalTagStatus Evaluation { get; }
    public string EvaluationString => Evaluation.ToString();
    public string Message { get; }
}

/// <summary>
///     Holds consolidated metrics for single-destination domains.
/// </summary>
public class ConsolidatedSslDetails
{
    public string DomainName { get; set; }
    public bool? IsSslValid { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int? ExpiresInDays { get; set; }
}

public enum CanonicalTagStatus
{
    NoSiteResolution,
    Missing,
    Match,
    Mismatch
}

public class EndpointTrace
{
    public string StartingUrl { get; set; }
    public string FinalResolvedUrl { get; set; }
    public int HopCount { get; set; }
    public int FinalStatusCode { get; set; }
    public bool IsUnreachable { get; set; }
    public string ErrorMessage { get; set; }

    // Master Model Properties
    public DateTime? SslExpirationDate { get; set; }

    // Nullable Expression-Bodied shorthand logic
    // Null = Plain text HTTP route (No evaluation checked)
    // False = Encountered bad/mismatched/unreachable certificate configuration (MinValue)
    // True = Clean, valid handshake resolved
    public bool? IsSslValid => SslExpirationDate == null ? null : SslExpirationDate.Value != DateTime.MinValue;
}