namespace Zakup.Abstractions.DataContext;

public interface IDeletable
{
    bool HasDeleted { get; set; }

    DateTime? DeletedUtc { get; set; }
}