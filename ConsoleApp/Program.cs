using Jeff32819DLL;
using Newtonsoft.Json;
using WebRequesterDll;
using WebRequesterDll.Models;


//await dbSvc.PageGetAsync("http://example.com");

////await dbSvc.PageLinkAddAsync();

//Console.WriteLine(count);


//return;


const string cacheFolder = @"t:\test-web-requestor";
const string domainName = "https://jeff32819.com/";
//const string domainName = "https://jeffmathews.com/";
//const string domainName = "https://www.jumpstartfitorlando.com/";
//const string domainName = "https://seeworthyconsulting.com/";
//const string domainName = "https://homecontrolfreak.com/";
//const string domainName = "https://www.tesla.com/powerwall";
//const string domainName = "https://www.creativefabrica.com/";


using var log = new JLog.FileLogger("t:\\web-requester-logs\\log.txt");
try
{
    var requestorConfig = new RequesterConfig(cacheFolder, new Uri(domainName));
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