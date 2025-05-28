namespace Zakup.Services.Options;

public class BotConfigOptions
{
    public const string Key = "Telegram";
    
    public List<long> AdministratorIds { get; set; }
}