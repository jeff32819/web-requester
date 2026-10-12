using Newtonsoft.Json;

using WebRequesterDll;


const string domainName = "jeff32819.com";
var report = await DomainAuditor.AuditDomainAsync(domainName);
if (report == null)
{
    Console.WriteLine($"Could not audit '{domainName}' - no valid, publicly routable host could be resolved.");
    return;
}

Console.WriteLine($"Discovered Primary URL: {report.DiscoveredPrimaryUrl ?? "None Found"}");
Console.WriteLine($"HTML Canonical Tag Found: {report.HtmlCanonicalTagValue ?? "NONE DETECTED"}");
Console.WriteLine($"Has SEO Issue? {report.HasCanonicalizationIssue}");
Console.WriteLine();
foreach (var trace in report.Traces)
{
    Console.WriteLine($"{trace.StartingUrl}");

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


/*** GET PAGE BELOW *********************************************************************/


var reportPath = Path.Combine(AppContext.BaseDirectory, "audit-report.json");
File.WriteAllText(reportPath, JsonConvert.SerializeObject(report, Formatting.Indented));
Console.WriteLine($"Report written to {reportPath}");
Console.WriteLine();

WebRequesterEvents.ProcessCompleted += (s, msg) => Console.WriteLine("------------ " + msg);
try
{
    var startUri = new Uri("https://jeff32819.com/");
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