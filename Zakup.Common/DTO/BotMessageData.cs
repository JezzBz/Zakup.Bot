using Telegram.Bot.Types.ReplyMarkups;

namespace Zakup.Common.DTO;

public class BotMessageData
{
    private string? MessageText = null;
    public List<List<InlineKeyboardButton>> Keyboard { get; set; }

    public string? Text
    {
        get
        {
            if (!Files.Any())
            {
                return MessageText;
            }
            
            return null;
        }
        set
        {
            MessageText = value;
        }
    }
    
    public string? Caption  {
        get
        {
            if (Files.Any())
            {
                return MessageText;
            }

            return null;
        }
        set => MessageText = value;
    }

    //TODO: переделать на документ после того как их добавлю 
    public List<object> Files { get; set; }
    
    
}