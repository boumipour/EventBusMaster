using MySqlConnector;

namespace MrEventBus.Boxing.MySql
{
    public interface IMySqlConnectionFactory 
    {
        MySqlConnection CreateConnection();
    }

    public class MySqlConnectionFactory: IMySqlConnectionFactory
    {
        private readonly string _connectionString;

        public MySqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public MySqlConnection CreateConnection()
        {
            var connection = new MySqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
