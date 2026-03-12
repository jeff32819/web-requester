namespace WebRequesterData;

public interface IDatabaseService
{
    Task<int> Ping();
}