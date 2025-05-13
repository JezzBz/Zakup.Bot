using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Zakup.Entities;

namespace Zakup.EntityFramework;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)  : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
    
    
    public DbSet<ChannelAdministrator> ChannelAdministrators { get; set; }
    
    public DbSet<ChannelFeedback> ChannelFeedback { get; set; }
    
    public DbSet<ChannelJoinRequest> ChannelJoinRequests { get; set; }
    
    public DbSet<ChannelMember> ChannelMembers { get; set; }
    
    public DbSet<ChannelSheet> ChannelSheets { get; set; }
    
    public DbSet<TelegramChannel> Channels { get; set; }
    
    public DbSet<TelegramAdPost> TelegramAdPosts { get; set; }
    
    public DbSet<TelegramDocument> TelegramDocuments { get; set; }
    
    public DbSet<TelegramPostButton> TelegramPostButtons { get; set; }
    
    public DbSet<TelegramUser> Users { get; set; }
    
    public DbSet<TelegramUserState> UserStates { get; set; }
    
    public DbSet<TelegramZakup> TelegramZakups { get; set; }
    
    public DbSet<UserSpreadSheet> SpreadSheets { get; set; }
    
    public DbSet<ZakupClient> ZakupClients { get; set; }
    
    public DbSet<MessageForward> MessageForwards { get; set; }
    
    public DbSet<ChannelRating> ChannelRatings { get; set; }
}