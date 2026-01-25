using Newtonsoft.Json;
using WebRequesterDll;

const string cacheFolder = @"X:\website-link-validator";
const string domainName = "https://jeff32819.com/";
//const string domainName = "https://jeffmathews.com/";
//const string domainName = "https://www.jumpstartfitorlando.com/";
//const string domainName = "https://seeworthyconsulting.com/";
//const string domainName = "https://homecontrolfreak.com/";
//const string domainName = "https://www.tesla.com/powerwall";
//const string domainName = "https://www.creativefabrica.com/";

var response = await Requester.GetFromWeb(domainName, cacheFolder, MyEnum.CacheMode.ForceRefresh);
Console.WriteLine(JsonConvert.SerializeObject(response.Info, Formatting.Indented));
Console.WriteLine();
Console.WriteLine($"HTML content length = {response.Content.Length}");
Console.WriteLine();
Console.WriteLine("press any key to exit");
Console.ReadKey();