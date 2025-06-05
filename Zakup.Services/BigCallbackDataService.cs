using Zakup.Abstractions.DataContext;
using Zakup.Entities;
using Zakup.EntityFramework;

namespace Zakup.Services;

public class BigCallbackDataService : IBigCallbackDataService
{
    private readonly ApplicationDbContext _context;

    public BigCallbackDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetBigCallbackData(long id)
    {
        return (await _context.BigCallbackData.FindAsync(id)!)?.CallbackData;
    }

    public async Task<long> AddData(string bigCallbackData)
    {
        var entity =await _context.AddAsync(new BigCallbackData()
        {
            CallbackData = bigCallbackData
        });
        await _context.SaveChangesAsync();
        return entity.Entity.Id;
    }
}