namespace MrEventBus.Abstraction.Producer.Outbox.Config;

public class OutboxConfiguration
{
    public bool EnableOutboxProcessor { get; set; } = true;
    public ushort OutboxProcessConcurrency { get; set; } = 1;
    public TimeSpan OutboxReaderInterval { get; set; } = new TimeSpan(0, 0, 5);
    public TimeSpan OutboxPersistenceDuration { get; set; } = new TimeSpan(0, 0, 0);

    public IReadOnlyCollection<Type> EnabledEvents { get; set; } = new List<Type>();

    public string ConnectionString { get; set; }=string.Empty;
}
