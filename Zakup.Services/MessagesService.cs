using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Zakup.Common.DTO;
using Zakup.EntityFramework;

namespace Zakup.Services;

public class MessagesService
{
    private readonly UserService _userService;
    public MessagesService(IDbContextFactory<ApplicationDbContext> dbContextFactory, UserService userService)
    {
        _userService = userService;
    }

    public async Task SendMenuMessage(ITelegramBotClient botClient, long userId)
    {
       var userState = await _userService.GetUserState(userId);
    }
}