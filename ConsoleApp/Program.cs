using Newtonsoft.Json;
using WebRequesterDll;

const string cacheFolder = "T:\\aaaaaaaaaaaaaaaaaaa";
//const string domainName = "https://jeff32819.com/";
const string domainName = "https://jeffmathews.com/";
//const string domainName = "https://www.jumpstartfitorlando.com/";
//const string domainName = "https://seeworthyconsulting.com/";
//const string domainName = "";



var response = await Requester.GetFromWeb(domainName, cacheFolder);
Console.WriteLine(JsonConvert.SerializeObject(response.Properties, Formatting.Indented));
Console.WriteLine();
Console.WriteLine($"HTML content length = {response.Content.Length}");
Console.WriteLine();
Console.WriteLine("press any key to exit");
Console.ReadKey();