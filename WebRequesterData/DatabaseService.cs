using Jeff32819DLL.MiscCore20;
using Microsoft.EntityFrameworkCore;
using WebRequesterData.Models;

namespace WebRequesterData;

internal class DatabaseService(WebRequesterContext db) : IDatabaseService
{
    public async Task<int> PingAsync()
    {
        return await db.pageTbl.CountAsync();
    }
    /// <summary>
    /// Get page, will add if it does not exist.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public async Task<pageTbl> PageGetAsync(string url)
    {
        var pg = new pageTbl
        {
            url = url,
            md5 = url.ToMd5Hash()
        };
        var existing = await db.pageTbl.SingleOrDefaultAsync(p => p.md5 == pg.md5);
        if (existing != null)
        {
            return existing;
        }
        var entry = db.pageTbl.Add(pg);
        await db.SaveChangesAsync();
        return entry.Entity;
    }
    /// <summary>
    /// Update page, ususally after scraping.
    /// </summary>
    /// <param name="page"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task PageUpdateAsync(pageTbl page)
    {
        var rs = await db.pageTbl.SingleOrDefaultAsync(p => p.id == page.id);
        if (rs == null)
        {
            throw new Exception("Page not found");
        }
        rs.statusCode = page.statusCode;
        rs.html = page.html;
        rs.updatedTimeStamp = DateTime.UtcNow;
        rs.errorMessage = page.errorMessage;
        await db.SaveChangesAsync();
    }


    public async Task<pageLinkTbl> PageLinkAddAsync()
    {
        db.pageLinkTbl.Add(new pageLinkTbl
        {
            pageId = 1,
            linkId = 1
        });
        await db.SaveChangesAsync();
        return new pageLinkTbl();
    }
}