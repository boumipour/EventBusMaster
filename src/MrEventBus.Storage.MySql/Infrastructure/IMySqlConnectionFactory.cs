using MySqlConnector;

namespace MrEventBus.Storage.MySql.Infrastructure
{
    public interface IMySqlConnectionFactory
    {
        MySqlConnection GetConnection();
    }
}
