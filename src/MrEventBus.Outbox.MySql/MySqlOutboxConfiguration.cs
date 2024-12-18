using MrEventBus.Abstraction.Producer.Outbox.Config;

namespace MrEventBus.Boxing.MySql;

public class MySqlOutboxConfiguration : OutboxConfiguration
{
    public string MySqlConnectionString { get; set; } = string.Empty;
}
