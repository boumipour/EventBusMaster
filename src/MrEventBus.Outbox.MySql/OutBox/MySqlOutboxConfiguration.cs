using MrEventBus.Abstraction.Producer.Outbox.Config;

namespace MrEventBus.Boxing.MySql.OutBox;

public class MySqlOutboxConfiguration : OutboxConfig
{
    public string MySqlConnectionString { get; set; } = string.Empty;
}
