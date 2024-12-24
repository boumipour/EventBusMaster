using MrEventBus.Abstraction.Producer.Outbox.Config;

namespace MrEventBus.Box.MySql.OutBox;

public class MySqlOutboxConfig : OutboxConfig
{
    public bool DBInitializer { get; set; } = false;
    public string MySqlConnectionString { get; set; } = string.Empty;
}
