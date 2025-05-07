namespace Bot.Core;

public abstract class Worker
{
    public abstract ValueTask OnStart(CancellationToken ct = default);
    public abstract ValueTask OnUpdate(CancellationToken ct = default);
    public abstract ValueTask OnDestroy(CancellationToken ct = default);
}
