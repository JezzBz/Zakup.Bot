using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.EntityFramework;
using Zakup.Services.Data;
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

    public async Task<IQueryable<ForwardUserInfo>> GetUserInfo(ITelegramBotClient botClient, long userId, long adminId, bool isPremium, string userName, CancellationToken cancellationToken = default)
    {
        var memberInfoQuery = _context.ChannelMembers
             .Include(m => m.Channel)
             .Where(m => m.UserId == userId)
              .Where(m => m.Channel.Administrators.Any(a => a.Id == adminId));
         var forwardMessage = new MessageForward
         {
             UserId = userId,
             ForwardAtUtc = DateTime.UtcNow,
             Source = MessageForwardSource.User
         };
         await _context.AddAsync(forwardMessage, cancellationToken);
         await _context.SaveChangesAsync(cancellationToken);
         var resultQuery = from m in memberInfoQuery
             join z in _context.TelegramZakups on m.InviteLink equals z.InviteLink into outter
             from o in outter.DefaultIfEmpty()
             select new ForwardUserInfo { Zakup = o, Member = m };
  
         var untrackedChannelsMember = await _context.Channels
             .Where(tc => tc.Administrators.Any(c => c.Id == userId))
             .Where(tc => tc.Members.All(m => m.UserId != userId))
             .ToListAsync(cancellationToken: cancellationToken);
  
         var untrackedMemberList = new List<ChannelMember>();
  
         foreach (var c in untrackedChannelsMember)
		 {
		 	try
		 	{
                 // Console.WriteLine(с.Id);
		 		var memberInfo = await botClient.GetChatMemberAsync(c.Id, userId, cancellationToken: cancellationToken);
  
		 		if (memberInfo.Status is ChatMemberStatus.Kicked or ChatMemberStatus.Left)
		 		{
		 			continue;
		 		}
  
		 		var newMember = new ChannelMember()
		 		{
		 			UserId = userId,
		 			IsPremium = isPremium,
		 			UserName = userName,
		 			ChannelId = c.Id,
		 			Status = true,
		 			Refer = "origin",
		 			JoinCount = 1,
		 		};
  
		 		untrackedMemberList.Add(newMember);
		 	}
		 	catch (ApiRequestException ex) when (ex.Message.Contains("bot is not a member of the channel chat") || ex.Message.Contains("chat not found"))
		 	{
		 		// Optionally log the error or inform the user
		 		Console.WriteLine($"Ошика Bot is not an admin in the channel {c.Id}. Cannot retrieve member info.");
		 		continue;
		 	}
		 }
  
  
         await _context.AddRangeAsync(untrackedMemberList, cancellationToken);
         await _context.SaveChangesAsync(cancellationToken);
         return resultQuery;
    }

    public async Task MarkAsLead(long userId, long leadUserId, CancellationToken cancellationToken = default)
    {
        var memberInfoQuery = _context.ChannelMembers
            .Include(m => m.Channel)
            .Where(m => m.UserId == leadUserId && m.Channel.Administrators.Any(a => a.Id == userId));

        var clientsQuery = memberInfoQuery.Join(_context.TelegramZakups, m => m.InviteLink, z => z.InviteLink,
            (member, zakup) => new ZakupClient()
            {
                ZakupId = zakup.Id,
                MemberId = member.Id
            });

        clientsQuery = clientsQuery
            .Where(c => !_context.ZakupClients.Any(zc => zc.Id == c.MemberId && zc.ZakupId == c.ZakupId));

        await _context.AddRangeAsync(clientsQuery, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUser(TelegramUser user, CancellationToken cancellationToken)
    {
        _context.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<long> DeleteUserFeedbacks(long userId, CancellationToken cancellationToken = default)
    {
        var feedBacks = _context.ChannelFeedback
            .Where(f => f.FromUserId == userId);
        var feedBacksCount = await feedBacks.CountAsync(cancellationToken: cancellationToken);
        _context.RemoveRange(feedBacks);
        await _context.SaveChangesAsync(cancellationToken);
        return feedBacksCount;
    }

    public async Task MuteUser(long userId,DateTime mutedToUtc, CancellationToken cancellationToken = default)
    {
        var user = await GetUser(userId, cancellationToken);
        user.MutedToUtc = mutedToUtc;
        await UpdateUser(user, cancellationToken);
    }

    public async Task UnMute(long userId, CancellationToken cancellationToken)
    {
       var user = await GetUser(userId, cancellationToken);
       user.MutedToUtc = null;
       await UpdateUser(user, cancellationToken);
    }

    public async Task UpdateUntrackedMemberChanges(ITelegramBotClient botClient, long userId, long requesterId,
        CancellationToken cancellationToken = default)
    {
        var untrackedChannelsMember = await _context.Channels
            .Where(tc => tc.Administrators.Any(c => c.Id == requesterId))
            .Where(tc => tc.Members.All(m => m.UserId != userId))
            .ToListAsync(cancellationToken: cancellationToken);

        var untrackedMemberList = new List<ChannelMember>();

        foreach (var c in untrackedChannelsMember)
        {
            try
            {
                var memberInfo = await botClient.GetChatMemberAsync(c.Id, userId, cancellationToken: cancellationToken);

                if (memberInfo.Status is ChatMemberStatus.Kicked or ChatMemberStatus.Left)
                {
                    continue;
                }

                var newMember = new ChannelMember()
                {
                    UserId = userId,
                    IsPremium = memberInfo.User.IsPremium,
                    UserName = memberInfo.User.Username,
                    ChannelId = c.Id,
                    Status = true,
                    Refer = "origin",
                    JoinCount = 1,
                };

                untrackedMemberList.Add(newMember);
            }
            catch (ApiRequestException ex)
                // when (ex.Message.Contains("chat not found") 
                //    || ex.Message.Contains("bot is not a member")
                //    || ex.Message.Contains("PARTICIPANT_ID_INVALID")
                //    || ex.Message.Contains("member list is inaccessible"))
            {
                Console.WriteLine($"Не удалось получить информацию о пользователе в чате {c.Id}: {ex.Message}");
                continue;
            }
        }
    }

    public async Task<ChannelMember?> GetMemberByUsername(string username)
    {
        return await _context.ChannelMembers.FirstOrDefaultAsync(m => m.UserName == username);
    }
}