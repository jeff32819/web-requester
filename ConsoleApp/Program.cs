using Jeff32819DLL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using WebRequesterData;
using WebRequesterDll;
using WebRequesterDll.Models;







var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMyDatabaseServices(@"Data Source=(local)\dev14;Initial Catalog=WebRequester;Integrated Security=True;Encrypt=False;");
using IHost host = builder.Build();
var dbSvc = host.Services.GetRequiredService<IDatabaseService>();
var count = await dbSvc.PingAsync();
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
    var response = await Requester.GetFromWeb(requestorConfig, MyEnum.CacheMode.UseCacheIfExists, log);
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