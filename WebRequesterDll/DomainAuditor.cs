using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WebRequesterDll;

public static class DomainAuditor
{
    private static readonly HttpClient _scraperClient;

    static DomainAuditor()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            // Certs are not evaluated here - we still want to trace/canonicalize a host even if its
            // certificate is expired or untrusted. SSL verification is a separate concern (SslVerify).
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        _scraperClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(6) };
        _scraperClient.DefaultRequestHeaders.Clear();
        _scraperClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _scraperClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        _scraperClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
        _scraperClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
    }

    public static async Task<DomainAuditReport> AuditDomainAsync(string rawInput, CancellationToken cancellationToken = default)
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
            WebRequesterEvents.RaiseProcessCompleted($"Error: Could not parse a valid domain host from raw input: '{rawInput}'");
            return null;
        }

        // 3. (Optional) Strip "www." if your engine treats "www.domain.com" and "domain.com" as the same base identity
        var baseDomain = extractedHost.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? extractedHost.Substring(4)
            : extractedHost;

        if (string.IsNullOrWhiteSpace(baseDomain))
        {
            WebRequesterEvents.RaiseProcessCompleted("Error: Sanitized domain resulted in an empty string.");
            return null;
        }

        // Refuse to probe hosts that only resolve to loopback/private/link-local addresses (e.g. localhost,
        // 169.254.169.254 cloud metadata, RFC1918 ranges) so this can't be used as an SSRF vector against
        // internal infrastructure if it's ever fed untrusted input.
        if (await ResolvesOnlyToDisallowedAddressesAsync(baseDomain, cancellationToken))
        {
            WebRequesterEvents.RaiseProcessCompleted($"Error: Refusing to audit '{baseDomain}' - it resolves only to a private, loopback, or link-local address.");
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
        var traceTasks = variants.Select(v => TraceUrlAsync(v, cancellationToken)).ToList();

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
            await AuditHtmlCanonicalTagAsync(report, cancellationToken);
        }

        return report;
    }

    private static async Task<EndpointTrace> TraceUrlAsync(string url, CancellationToken cancellationToken)
    {
        var trace = new EndpointTrace { StartingUrl = url };
        var currentUrl = url;
        const int maxHops = 6;

        try
        {
            while (trace.HopCount < maxHops)
            {
                using var response = await _scraperClient.GetAsync(currentUrl, cancellationToken);
                trace.FinalStatusCode = (int)response.StatusCode;

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
        }

        return trace;
    }

    private static async Task AuditHtmlCanonicalTagAsync(DomainAuditReport report, CancellationToken cancellationToken)
    {
        try
        {
            var html = await _scraperClient.GetStringAsync(report.DiscoveredPrimaryUrl, cancellationToken);
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

    /// <summary>
    ///     Resolves <paramref name="host"/> and reports whether every address it resolves to is loopback,
    ///     private, or link-local (i.e. there is no publicly routable address worth probing).
    /// </summary>
    /// <remarks>
    ///     This is a resolve-then-check guard, not a connect-time one, so it does not fully protect against
    ///     DNS-rebinding (a host resolving to a public address here but a private one at actual connect time).
    ///     It stops the common case: a caller feeding in "localhost", an internal hostname, or a raw private/
    ///     loopback IP literal.
    /// </remarks>
    private static async Task<bool> ResolvesOnlyToDisallowedAddressesAsync(string host, CancellationToken cancellationToken)
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            return addresses.Length == 0 || addresses.All(IsDisallowedAddress);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested == false)
        {
            // Can't resolve it - nothing to audit, but that's a DNS failure, not an SSRF concern.
            return false;
        }
    }

    /// <summary>
    ///     Internal (rather than private) so it can be unit tested directly without hitting real DNS - see
    ///     WebRequesterDll.Tests.
    /// </summary>
    internal static bool IsDisallowedAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
        {
            return true;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            var v6Bytes = address.GetAddressBytes();
            if ((v6Bytes[0] & 0xfe) == 0xfc) // fc00::/7 - unique local address
            {
                return true;
            }
        }

        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return false;
        }

        var b = address.GetAddressBytes();
        return b[0] switch
        {
            0 => true, // 0.0.0.0/8
            10 => true, // 10.0.0.0/8 (private)
            127 => true, // 127.0.0.0/8 (loopback)
            169 when b[1] == 254 => true, // 169.254.0.0/16 (link-local, incl. cloud metadata)
            172 when b[1] is >= 16 and <= 31 => true, // 172.16.0.0/12 (private)
            192 when b[1] == 168 => true, // 192.168.0.0/16 (private)
            100 when b[1] is >= 64 and <= 127 => true, // 100.64.0.0/10 (carrier-grade NAT)
            _ => false
        };
    }
}

public class DomainAuditReport
{
    public int Version { get; set; } = 1;
    public string BaseDomain { get; set; }
    public bool HasCanonicalizationIssue { get; set; }
    public string DiscoveredPrimaryUrl { get; set; }
    public List<string> UniqueDestinations { get; set; } = new();
    public List<EndpointTrace> Traces { get; set; } = new();
    public string HtmlCanonicalTagValue { get; set; }
    public bool CanonicalTagMatchesDestination { get; set; }

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
}