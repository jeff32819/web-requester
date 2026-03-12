using WebRequesterData.Models;

namespace WebRequesterData;

public interface IDatabaseService
{
    Task<int> PingAsync();
    Task<pageTbl> PageGetAsync(string url);
    Task PageLinkAddAsync(int pageId, int linkId);
    Task PageUpdateAsync(pageTbl page);

}