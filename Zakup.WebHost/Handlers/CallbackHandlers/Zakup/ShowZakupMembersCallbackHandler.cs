using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.ShowMembers)]
public class ShowZakupMembersCallbackHandler : ICallbackHandler<ShowZakupMembersCallbackData>
{
    private ZakupService _zakupService;

    public ShowZakupMembersCallbackHandler(ZakupService zakupService)
    {
        _zakupService = zakupService;
    }

    public async Task Handle(ITelegramBotClient botClient, ShowZakupMembersCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var members = await _zakupService.GetMembers(data.ZakupId, cancellationToken);
        var currentMembers = members.Where(m => m.LeftUtc == null).ToList();
        var unsubscribedMembers = members.Where(m => m.LeftUtc != null).ToList();
        if (!members.Any())
        {
            await botClient.SendTextMessageAsync(callbackQuery.From.Id, MessageTemplate.NoSubscribeData, cancellationToken: cancellationToken);
            return;
        }
        await SendMembersList(botClient,MessageTemplate.CurrentSubscribers, currentMembers, callbackQuery.From.Id);
        await SendMembersList(botClient, MessageTemplate.UnSubscribedMembers, unsubscribedMembers, callbackQuery.From.Id);
    }

    public ShowZakupMembersCallbackData Parse(List<string> parameters)
    {
        return new ShowZakupMembersCallbackData()
        {
            ZakupId = Guid.Parse(parameters[0]),
        };
    }
    
     async Task SendMembersList(ITelegramBotClient botClient, string header, List<ChannelMember> memberList, long userId)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine(header);

            if (!memberList.Any())
            {
                messageBuilder.AppendLine("Нет данных.");
                await botClient.SendTextMessageAsync(userId, messageBuilder.ToString());
                return;
            }

            int maxMessageLength = 4000; // Максимальная длина сообщения

            foreach (var member in memberList)
            {
                var username = string.IsNullOrEmpty(member.UserName) ? "Неизвестно" : $"@{member.UserName}";
                var line = $"• Username: {username}, ID: {member.UserId}";

                if (member.LeftUtc != null)
                {
                    var leftDate = member.LeftUtc.Value.ToString("dd.MM.yy");
                    string daysInChannelText = "";
                    if (member.JoinedUtc != null)
                    {
                        var daysInChannel = (member.LeftUtc.Value - member.JoinedUtc.Value).TotalDays;
                        daysInChannelText = $" ({daysInChannel:N0} дней в канале)";
                    }
                    line += $", Отписался: {leftDate}{daysInChannelText}";
                }

                // Проверяем, не превышает ли сообщение максимальную длину
                if (messageBuilder.Length + line.Length + 1 > maxMessageLength)
                {
                    await botClient.SendTextMessageAsync(userId, messageBuilder.ToString());
                    messageBuilder.Clear();
                    messageBuilder.AppendLine(header); // Повторяем заголовок в новом сообщении
                }

                messageBuilder.AppendLine(line);
            }

            // Отправляем оставшийся текст
            if (messageBuilder.Length > 0)
            {
                await botClient.SendTextMessageAsync(userId, messageBuilder.ToString());
            }
        }
}