using Dapper;
using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Producer.Outbox.Repository;
using System.Data;

namespace MrEventBus.Boxing.MySql
{
    public class OutboxMySqlRepository : IOutboxRepository
    {
        private readonly IMySqlConnectionFactory _mySqlConnectionFactory;
        private readonly StoredProcedureCreator _storedProcedureCreator;

        public OutboxMySqlRepository(IMySqlConnectionFactory mySqlConnectionFactory, StoredProcedureCreator storedProcedureCreator)
        {
            _mySqlConnectionFactory = mySqlConnectionFactory;
            _storedProcedureCreator = storedProcedureCreator;
        }

        public async Task CreateAsync(OutboxMessage outboxMessage)
        {
            try
            {
                await _storedProcedureCreator.CreateAllStoredProceduresAsync();
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

                using var connection = _mySqlConnectionFactory.CreateConnection();
                await connection.ExecuteAsync("OutBox_Insert", parameters, commandType: CommandType.StoredProcedure);

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
                await _storedProcedureCreator.CreateAllStoredProceduresAsync();

                var state = (int)OutboxMessageState.Sended;

                var param = new Dictionary<string, object>()
                {
                    ["@IN_PersistencePeriodInDays"] = persistencePeriodInDays,
                    ["@IN_State"] = state
                };
                var parameters = new DynamicParameters();
                parameters.AddDynamicParams(param);

                using var connection = _mySqlConnectionFactory.CreateConnection();
                await connection.ExecuteAsync("OutBox_Delete", parameters, commandType: CommandType.StoredProcedure);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        public async Task<IEnumerable<OutboxMessage>> GetAsync()
        {
            try
            {
                await _storedProcedureCreator.CreateAllStoredProceduresAsync();

                using var connection = _mySqlConnectionFactory.CreateConnection();
                return await connection.QueryAsync<OutboxMessage>("OutBox_Select", commandType: CommandType.StoredProcedure);
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
                await _storedProcedureCreator.CreateAllStoredProceduresAsync();

                var param = new Dictionary<string, object>()
                {
                    ["@IN_MessageId"] = outboxMessage.MessageId,
                    ["@IN_State"] = outboxMessage.State
                };
                var parameters = new DynamicParameters();
                parameters.AddDynamicParams(param);

                using var connection = _mySqlConnectionFactory.CreateConnection();
                await connection.ExecuteAsync("OutBox_Update", parameters, commandType: CommandType.StoredProcedure);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }
    }
}
