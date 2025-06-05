using System.Collections.Concurrent;

namespace Zakup.Services;

public class MetadataStorage
{
    public readonly ConcurrentDictionary<long /* userId */, string /* last query */> PostMetadataStorage = new();
}
