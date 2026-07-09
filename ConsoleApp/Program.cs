using Newtonsoft.Json;
using WebRequesterDll;
using WebRequesterDll.Models;



// Use await in asynchronous pipelines:
DomainAuditReport report = await BulkDomainAuditor.AuditDomainAsync("seeworthyconsulting.com");

if (report != null)
{
    Console.WriteLine($"Domain: {report.BaseDomain}");
    Console.WriteLine($"Primary URL: {report.DiscoveredPrimaryUrl ?? "None Found"}");
    Console.WriteLine($"Has SEO Issue? {report.HasCanonicalizationIssue}");
    foreach (var trace in report.Traces)
    {
        if (trace.HopCount > 0)
        {
            // Clarify that it redirected to a final, healthy page
            Console.WriteLine($"-> Start: {trace.StartingUrl} --(Redirected in {trace.HopCount} hop)--> Ended at: {trace.FinalResolvedUrl} (Status: {trace.FinalStatusCode})");
        }
        else
        {
            // No hops, it just loaded directly
            Console.WriteLine($"-> Start: {trace.StartingUrl} (Loaded directly with Status: {trace.FinalStatusCode})");
        }
    }
}
Console.WriteLine($"Discovered Primary URL: {report.DiscoveredPrimaryUrl}");
Console.WriteLine($"HTML Canonical Tag Found: {report.HtmlCanonicalTagValue ?? "NONE DETECTED"}");

if (string.IsNullOrEmpty(report.HtmlCanonicalTagValue))
{
    Console.WriteLine("⚠️ WARNING: Missing HTML canonical tag entirely. If server redirects fail, duplicate content will occur.");
}
else if (report.CanonicalTagMatchesDestination)
{
    Console.WriteLine("✅ PASS: The HTML canonical tag perfectly matches the target endpoint.");
}
else
{
    Console.WriteLine($"🚨 CRITICAL MISMATCH: The HTML tag points to '{report.HtmlCanonicalTagValue}' but the browser landed on '{report.DiscoveredPrimaryUrl}'. This heavily confuses search indexers!");
}
Console.ReadLine();




WebRequesterEvents.ProcessCompleted += (s, msg) => Console.WriteLine("------------ " + msg);
try
{
    var startUri =new Uri(TestWebsites.Jeff32819);
    var response = await Requester.GetFromWeb(startUri);
    Console.WriteLine(JsonConvert.SerializeObject(response.Info, Formatting.Indented));
    Console.WriteLine();
    Console.WriteLine($"HTML content length = {response.Content.Length}");
}
catch (Exception ex)
{
    WebRequesterEvents.RaiseProcessCompleted($"Exception = {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("------------------- press any key to exit");
Console.ReadKey();


internal static class TestWebsites
{
    public const string TestBadLink = "https://test-bad-link.com/";
    public const string JumpsStartFitOrlando = "https://www..com/";
    public const string SeeworthyConsulting = "https://seeworthyconsulting.com/";
    public const string HomeControlFreak = "https://homecontrolfreak.com/";
    public const string Tesla = "https://www.tesla.com/powerwall";
    public const string CreativeFabrica = "https://www.creativefabrica.com/";
    public const string Jeff32819 = "https://jeff32819.com/";
    public const string JeffMathews = "https://jeffmathews.com/";
    public const string Localhost = "http://localhost/";
    public const string SebastianMoving = "https://sebastianmoving.com";
}