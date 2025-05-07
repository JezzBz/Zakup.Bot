using Microsoft.EntityFrameworkCore;
using Zakup.Entities;

namespace Zakup.EntityFramework;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)  : DbContext(options)
{
    public DbSet<MessageForward> MessageForwards { get; set; }
    
    public DbSet<ChannelRating> ChannelRatings { get; set; }
}