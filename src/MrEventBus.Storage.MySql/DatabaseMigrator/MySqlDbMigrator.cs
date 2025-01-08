using Dapper;
using MrEventBus.Storage.MySql.Infrastructure;
using System.Data;
using System.Text.RegularExpressions;


namespace MrEventBus.Storage.MySql.DatabaseMigrator
{
    public class MySqlDbMigrator
    {
        private static bool _isInitialized;
        private static readonly object _lock = new();

        private string MigrationTableName = "EventBus_Migrations";

        private readonly IMySqlConnectionFactory _mySqlConnectionFactory;

        private string GetMigrationFolderName() => $"{Environment.ProcessPath?.Substring(0, Environment.ProcessPath.LastIndexOf('\\'))}\\DatabaseScripts";
        private string GetMigrationFileRegex() => "^(?<place>\\d+)-(?<name>.+)\\.sql$";

        public MySqlDbMigrator(IMySqlConnectionFactory mySqlConnectionFactory)
        {
            _mySqlConnectionFactory = mySqlConnectionFactory;
        }


        private async Task CreateMigrationTableIfNotExistsAsync()
        {
            var Connection = _mySqlConnectionFactory.GetConnection();

            string query =
                "SELECT count(*) FROM " +
                "information_schema.tables " +
                "WHERE table_schema = '" + Connection.Database + "' " +
                "AND table_name = '" + MigrationTableName + "';";

            var count = await Connection.ExecuteScalarAsync<int>(query);
            if (count == 0)
            {
                query = $"CREATE TABLE {MigrationTableName} (" +
                        "MigrationName varchar(100) NOT NULL," +
                        "AppliedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP," +
                        "Id INT auto_increment NOT NULL," +
                        "CONSTRAINT NewTable_PK PRIMARY KEY (Id))";

                await Connection.ExecuteAsync(query);
            }
        }

        private Dictionary<int, string> EvaluateDbScripts()
        {
            if (!Directory.Exists(GetMigrationFolderName()))
            {
                Console.WriteLine("there is no DatabaseScripts folder to read the migration scripts");
                return new Dictionary<int, string>();
            }

            var files = Directory.GetFiles(GetMigrationFolderName()).Select(x => x.Split("\\").Last());
            var regex = new Regex(GetMigrationFileRegex());
            if (files.All(x => regex.IsMatch(x)))
            {
                var result = files.Select(x => regex.Match(x)).ToDictionary(x => Convert.ToInt32(x.Groups["place"].Value),
                    x => x.Groups["name"].Value);
                return result;
            }

            throw new Exception($"Migration file names should match with this regex: \"{GetMigrationFolderName()}\"");
        }

        private async Task<bool> ShouldBeAppliedAsync(string migrationName)
        {
            var Connection = _mySqlConnectionFactory.GetConnection();

            var query = $"select count(*) from {MigrationTableName} where MigrationName='{migrationName}'";
            var count = await Connection.ExecuteScalarAsync<int>(query);
            return count == 0;
        }
        private async Task NoteAppliedMigrationsAsync(string migrationName)
        {
            var Connection = _mySqlConnectionFactory.GetConnection();

            var query = $" insert into {MigrationTableName} (MigrationName) values ('{migrationName}')";
            await Connection.ExecuteAsync(query);
        }

        private async Task ApplyChangesAsync(Dictionary<int, string> migrations)
        {
            var Connection = _mySqlConnectionFactory.GetConnection();

            foreach (var item in migrations.OrderBy(x => x.Key))
            {
                if (await ShouldBeAppliedAsync(item.Value))
                {
                    var fileName = $"{item.Key}-{item.Value}.sql";
                    var migration = await File.ReadAllTextAsync($"{GetMigrationFolderName()}/{fileName}");
                    await Connection.ExecuteAsync(migration, commandType: CommandType.Text);
                    await NoteAppliedMigrationsAsync(item.Value);

                }
            }
        }

        public async ValueTask MigrateAsync()
        {
            if (_isInitialized) return;

            lock (_lock)
            {
                if (_isInitialized)
                    return;

                _isInitialized = true;
            }

            try
            {
                await CreateMigrationTableIfNotExistsAsync();
                var scripts = EvaluateDbScripts();
                await ApplyChangesAsync(scripts);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

    }
}
