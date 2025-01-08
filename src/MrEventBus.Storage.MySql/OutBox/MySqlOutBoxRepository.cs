using Dapper;
using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Producer.Outbox.Repository;
using MrEventBus.Storage.MySql.DatabaseMigrator;
using MrEventBus.Storage.MySql.Infrastructure;
using System.Data;

namespace MrEventBus.Storage.MySql.OutBox
{
    public class MySqlOutBoxRepository : IOutboxRepository
    {
        private readonly IMySqlConnectionFactory _mySqlConnectionFactory;
        private readonly MySqlDbMigrator? _dbInitializer;

        public MySqlOutBoxRepository(IMySqlConnectionFactory mySqlConnectionFactory, MySqlDbMigrator? dbInitializer = null)
        {
            _mySqlConnectionFactory = mySqlConnectionFactory;
            _dbInitializer = dbInitializer;
        }

        public async Task<IEnumerable<OutboxMessage>> GetAsync()
        {
            try
            {
                if (_dbInitializer != null)
                    await _dbInitializer.MigrateAsync();

                using var connection = _mySqlConnectionFactory.GetConnection();
                return await connection.QueryAsync<OutboxMessage>("OutBox_Select", commandType: CommandType.StoredProcedure);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        public async Task CreateAsync(OutboxMessage outboxMessage)
        {
            try
            {
                if (_dbInitializer != null)
                    await _dbInitializer.MigrateAsync();

                var param = new Dictionary<string, object>()
                {
                    ["@IN_MessageId"] = outboxMessage.MessageId,
                    ["@IN_Type"] = outboxMessage.Type,
                    ["@IN_Data"] = outboxMessage.Data,
                    ["@IN_Shard"] = outboxMessage.Shard,
                    ["@IN_State"] = outboxMessage.State,
                    ["@IN_QueueName"] = outboxMessage.QueueName,
                    ["@IN_CreateDateTime"] = outboxMessage.CreateDateTime,
                    ["@IN_LastModifyDateTime"] = outboxMessage.LastModifyDateTime
                };
                var parameters = new DynamicParameters();
                parameters.AddDynamicParams(param);

                using var connection = _mySqlConnectionFactory.GetConnection();
                await connection.ExecuteAsync("OutBox_Insert", parameters, commandType: CommandType.StoredProcedure);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        public async Task UpdateAsync(OutboxMessage outboxMessage)
        {
            try
            {
                if (_dbInitializer != null)
                    await _dbInitializer.MigrateAsync();

                var param = new Dictionary<string, object>()
                {
                    ["@IN_MessageId"] = outboxMessage.MessageId,
                    ["@IN_State"] = outboxMessage.State
                };
                var parameters = new DynamicParameters();
                parameters.AddDynamicParams(param);

                using var connection = _mySqlConnectionFactory.GetConnection();
                await connection.ExecuteAsync("OutBox_Update", parameters, commandType: CommandType.StoredProcedure);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        public async Task DeleteAsync(double persistencePeriodInDays)
        {
            try
            {
                if (_dbInitializer != null)
                    await _dbInitializer.MigrateAsync();

                var state = (int)OutboxMessageState.Sended;

                var param = new Dictionary<string, object>()
                {
                    ["@IN_PersistencePeriodInDays"] = persistencePeriodInDays,
                    ["@IN_State"] = state
                };
                var parameters = new DynamicParameters();
                parameters.AddDynamicParams(param);

                using var connection = _mySqlConnectionFactory.GetConnection();
                await connection.ExecuteAsync("OutBox_Delete", parameters, commandType: CommandType.StoredProcedure);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

    }
}
