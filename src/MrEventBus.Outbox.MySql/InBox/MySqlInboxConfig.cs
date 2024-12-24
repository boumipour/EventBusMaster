using MrEventBus.Abstraction.Subscriber.Inbox.Config;

namespace MrEventBus.Box.MySql.InBox
{
    public class MySqlInboxConfig : InboxConfig
    {
        public bool DBInitializer { get; set; } = false;
        public string MySqlConnectionString { get; set; } = string.Empty;
    }
}
