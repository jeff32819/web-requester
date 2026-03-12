using Microsoft.EntityFrameworkCore;
using WebRequesterData.Models;

namespace WebRequesterData;

internal class DatabaseService(WebRequesterContext context) : IDatabaseService
{
    public async Task<int> Ping()
    {
        return await context.pageTbl.CountAsync();
    }
}