using Newtonsoft.Json;
using WebRequesterDll;

var report = await DomainAuditor.AuditDomainAsync("jeff32819.com");

if (report != null)
{
    Console.WriteLine($"Discovered Primary URL: {report.DiscoveredPrimaryUrl ?? "None Found"}");
    Console.WriteLine($"HTML Canonical Tag Found: {report.HtmlCanonicalTagValue ?? "NONE DETECTED"}");
    Console.WriteLine($"Has SEO Issue? {report.HasCanonicalizationIssue}");
    Console.WriteLine();
    foreach (var trace in report.Traces)
    {
        var sslInfo = trace.SslExpirationDate.HasValue
            ? $"SSL Expires: {trace.SslExpirationDate.Value.ToShortDateString()}"
            : "No SSL/HTTP";


        Console.WriteLine($"{trace.StartingUrl}");
        Console.WriteLine($"\t-- {sslInfo}");
        Console.WriteLine($"\t-- IsSslValid: {trace.IsSslValid}");
        
        if (trace.HopCount > 0)
        {
            Console.WriteLine($"\t-- (Redirected in {trace.HopCount} hop)");
            Console.WriteLine($"\t-- Ended at: {trace.FinalResolvedUrl}");
            Console.WriteLine($"\t-- (Status: {trace.FinalStatusCode})");
        }
        else
        {
            Console.WriteLine($"\t-- (Loaded directly with Status: {trace.FinalStatusCode})");
        }
        Console.WriteLine();
    }

//    Console.WriteLine(report.CanonicalMessage);
}
var jsonString = JsonConvert.SerializeObject(report, Formatting.Indented);

// Write to file
File.WriteAllText("t:\\audit-report.json", jsonString);
Console.ReadLine();


WebRequesterEvents.ProcessCompleted += (s, msg) => Console.WriteLine("------------ " + msg);
try
{
    var startUri = new Uri(TestWebsites.Jeff32819);
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