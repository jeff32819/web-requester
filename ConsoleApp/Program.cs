using Newtonsoft.Json;
using WebRequesterDll;

var response = await Requester.GetFromWeb("https://jeff32819.com");
Console.WriteLine(JsonConvert.SerializeObject(response.Properties, Formatting.Indented));
Console.WriteLine();
Console.WriteLine($"HTML content length = {response.Content.Length}");
Console.WriteLine();
Console.WriteLine("press any key to exit");
Console.ReadKey();