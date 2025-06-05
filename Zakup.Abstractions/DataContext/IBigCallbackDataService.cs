namespace Zakup.Abstractions.DataContext;

public interface IBigCallbackDataService
{
    public Task<string> GetBigCallbackData(long id);
    
    Task<long> AddData(string bigCallbackData);
}