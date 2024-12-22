using MrEventBus.Abstraction.Subscriber.Inbox.Config;

namespace MrEventBus.Boxing.MySql.InBox
{
    public class MySqlInboxConfiguration:InboxConfig
    {
        public string MySqlConnectionString { get; set; } = string.Empty;
    }
}
