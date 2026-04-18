using Jeff32819DLL;
using Newtonsoft.Json;
using WebRequesterDll;
using WebRequesterDll.Models;

const string cacheFolder = @"t:\test-web-requestor";
using var log = new JLog.FileLogger(@"t:\web-requester-logs\log.txt");
try
{
    var requestorConfig = new RequesterConfig(cacheFolder, new Uri(TestWebsites.Jeff32819));
    var response = await Requester.GetFromWeb(requestorConfig, log);
    Console.WriteLine(JsonConvert.SerializeObject(response.Info, Formatting.Indented));
    Console.WriteLine();
    Console.WriteLine($"HTML content length = {response.Content.Length}");
}
catch (Exception ex)
{
    log.Write(ex.Message);
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