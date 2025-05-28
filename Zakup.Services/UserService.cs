using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.EntityFramework;
using Zakup.Services.Extensions;
using Zakup.Services.Options;

namespace Zakup.Services;

public class UserService 
{
    private readonly ApplicationDbContext _context;
    private readonly BotConfigOptions _options;
    public UserService(ApplicationDbContext context, IOptions<BotConfigOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public bool IsAdmin(long userId)
    {
       return _options.AdministratorIds.Contains(userId);
    }
    
    public async Task<TelegramUserState?> GetUserState(long userId, CancellationToken cancellationToken = default)
    {
        var state = await _context.UserStates.FirstOrDefaultAsync(q => q.UserId == userId, cancellationToken) 
                    ?? 
                    await SetUserState(userId, new TelegramUserState(), cancellationToken);
        return state;
    }

    public async ValueTask<TelegramUserState> SetUserState(long userId, TelegramUserState state, CancellationToken cancellationToken = default)
    {
        var currentState = await _context.UserStates.FirstOrDefaultAsync(q => q.UserId == userId, cancellationToken);

        if (currentState == null)
        {
            state.UserId = userId;
            await _context.AddAsync(state, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return state;
        }

        currentState.State = state.State;
        currentState.CachedValue = state.CachedValue;
        currentState.MenuMessageId = state.MenuMessageId;
        currentState.PreviousMessageId = state.PreviousMessageId;
        var updatedState = _context.Update(currentState);
        await _context.SaveChangesAsync(cancellationToken);
        return updatedState.Entity;
    }
    
    public async ValueTask<TelegramUserState> SetUserState(TelegramUserState state, CancellationToken cancellationToken = default)
    {
        var currentState = await _context.UserStates.FirstOrDefaultAsync(q => q.UserId == state.UserId, cancellationToken);

        if (currentState == null)
        {
            state.UserId = state.UserId;
            await _context.AddAsync(state, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return state;
        }

        currentState.State = state.State;
        currentState.CachedValue = state.CachedValue;
        currentState.MenuMessageId = state.MenuMessageId;
        currentState.PreviousMessageId = state.PreviousMessageId;
        var updatedState = _context.Update(currentState);
        await _context.SaveChangesAsync(cancellationToken);
        return updatedState.Entity;
    }
    
    public async Task<TelegramUser?> GetUser(long userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(q => q.UserState)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken: cancellationToken);
    }

    public async Task<TelegramUser> CreateUser(long userId, string name, string? refer = null, CancellationToken cancellationToken = default)
    {
        var user = new TelegramUser()
        {
            Channels = new List<TelegramChannel>(),
            Id = userId,
            UserName = name,
            Refer = refer,
        };
        var state = new TelegramUserState
        {
            Id = Guid.NewGuid(),
            User = user,
            State = UserStateType.None,
        };
        try
        {
            var userEntity = await _context.AddAsync(user, cancellationToken);
            await _context.SaveChangesWithRetriesAsync(cancellationToken: cancellationToken);
            state.UserId = userEntity.Entity.Id;
            await _context.AddAsync(state, cancellationToken);
            await _context.SaveChangesWithRetriesAsync(cancellationToken: cancellationToken);
            user.UserState = state;
            return user;
        }
        catch (DbUpdateConcurrencyException) //Не уверен насколько это теперь нужно, перенёс из старого кода
        {
            // Проверяем, не был ли пользователь уже создан параллельным процессом
            var existingUser = await _context.Users
                .Include(u => u.UserState)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (existingUser != null)
            {
                Console.WriteLine($"[CreateUser] User {userId} already exists, using existing user");

                if (existingUser.UserState == null)
                {
                    // Создаем состояние, если его еще нет
                    var session = new TelegramUserState()
                    {
                        UserId = userId,
                        State = UserStateType.None,
                        Id = Guid.NewGuid(),
                        User = existingUser
                    };

                    await _context.AddAsync(session, cancellationToken);
                    await _context.SaveChangesWithRetriesAsync(cancellationToken: cancellationToken);
                }
            }
            else
            {
                // Если пользователя и правда нет, перебрасываем исключение
                throw;
            }
        }

        return user;
    }

    public async Task<bool> UserExists(long userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(q => q.Id == userId, cancellationToken);
    }
    
    
    public async Task<List<TelegramUser>> GetOrCreateAdminUsers(IEnumerable<ChatMember> admins, CancellationToken cancellationToken)
    {
        var adminList = new List<TelegramUser>();
        
        foreach (var admin in admins.Where(x => !x.User.IsBot))
        {
            var user = await _context.Users.FindAsync(admin.User.Id, cancellationToken);
            
            if (user is not null)
            {
                adminList.Add(user);
            }
            else
            {
                var newUser = new TelegramUser
                {
                    Id = admin.User.Id,
                    UserName = admin.User.Username,
                    Channels = new List<TelegramChannel>()
                };
                
                var createdUser = await _context.Users.AddAsync(newUser, cancellationToken);
                adminList.Add(createdUser.Entity);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        Console.WriteLine("Admin list updated.");
        
        return adminList;
    }
}