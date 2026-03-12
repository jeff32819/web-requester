using WebRequesterData.Models;

namespace WebRequesterData;

public interface IDatabaseService
{
    Task<int> PingAsync();
    Task<pageTbl> PageGetAsync(string url);
    Task<pageLinkTbl> PageLinkAddAsync();
    Task PageUpdateAsync(pageTbl page);

}