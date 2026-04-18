using Newtonsoft.Json;
using WebRequesterDll;
using WebRequesterDll.Models;

WebRequesterEvents.ProcessCompleted += (s, msg) => Console.WriteLine("------------ " + msg);
try
{
    var requestorConfig = new RequesterConfig(new Uri(TestWebsites.Jeff32819));
    var response = await Requester.GetFromWeb(requestorConfig);
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