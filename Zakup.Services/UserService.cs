using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.EntityFramework;
using Zakup.Services.Extensions;

namespace Zakup.Services;

public class UserService 
{
    private readonly ApplicationDbContext _context;
    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TelegramUserState?> GetUserState(long userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserStates.FindAsync(userId, cancellationToken);
    }

    public async ValueTask SetUserState(long userId, TelegramUserState state, CancellationToken cancellationToken = default)
    {
        var currentState = await _context.UserStates.FindAsync(userId, cancellationToken);

        if (currentState == null)
        {
            state.UserId = userId;
            await _context.AddAsync(state, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        currentState.State = state.State;
        currentState.CachedValue = state.CachedValue;
        currentState.MenuMessageId = state.MenuMessageId;
        currentState.PreviousMessageId = state.PreviousMessageId;
        _context.Update(currentState);
        await _context.SaveChangesAsync(cancellationToken);
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
            await _context.AddAsync(user, cancellationToken);
            await _context.AddAsync(state, cancellationToken);
            await _context.SaveChangesWithRetriesAsync(cancellationToken: cancellationToken);
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


   
}