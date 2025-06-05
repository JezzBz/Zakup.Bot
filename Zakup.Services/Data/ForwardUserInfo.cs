using Zakup.Entities;

namespace Zakup.Services.Data;

public class ForwardUserInfo
{
    public ChannelMember Member { get; set; }
    public TelegramZakup Zakup { get; set; }
}