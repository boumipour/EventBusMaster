using Dapper;
using System.Data;

namespace MrEventBus.Boxing.MySql;

public class StoredProcedureCreator
{
    private readonly IMySqlConnectionFactory _mySqlConnectionFactory;

    private static bool _isInitialized;
    private static readonly object _lock = new();

    public StoredProcedureCreator(IMySqlConnectionFactory mySqlConnectionFactory)
    {
        _mySqlConnectionFactory = mySqlConnectionFactory;
    }

    const string OutBox_Insert_SP_Drop = "DROP PROCEDURE IF EXISTS OutBox_Insert";
    const string OutBox_Insert_SP_Create = @"
        CREATE PROCEDURE OutBox_Insert (
		    IN IN_MessageId VARCHAR(36),
            IN IN_Type VARCHAR(500),
            IN IN_Data TEXT,
            IN IN_Shard VARCHAR(36),
		    IN IN_QueueName VARCHAR(200),
            IN IN_State smallint,
            IN IN_CreateDateTime DATETIME,
            IN IN_LastModifyDateTime DATETIME
        )
        BEGIN
	 
		    DECLARE ShardOffset INT;

            SELECT COUNT(*) INTO ShardOffset
            FROM OutboxMessages
            WHERE Shard = IN_Shard;
		
		
		    INSERT INTO db.OutboxMessages (MessageId,Type,DATA,Shard,ShardOffset,State,QueueName,CreateDateTime,LastModifyDateTime,LockUntil)
		    VALUES
		    (
		        IN_MessageId,
		        IN_Type,
		        IN_Data,
		        IN_Shard,
		        ShardOffset+1,
		        IN_State,
		        IN_QueueName,
		        IN_CreateDateTime,
		        IN_LastModifyDateTime,
		        ADDTIME(NOW(), SEC_TO_TIME(-5*60))
		    );
        END";

    const string OutBox_Select_SP_Drop = "DROP PROCEDURE IF EXISTS OutBox_Select";
    const string OutBox_Select_SP_Create = @"
        CREATE DEFINER=`root`@`%` PROCEDURE `OutBox_Select`()
        BEGIN
		    
            SET @batchId = UUID();

            UPDATE OutboxMessages om
            JOIN (
                SELECT MIN(ShardOffset) AS minShardOffset, Shard
                FROM OutboxMessages
                WHERE State = 1
                GROUP BY Shard
            ) AS subquery ON om.Shard = subquery.Shard
            AND om.ShardOffset = subquery.minShardOffset
            SET 
                om.LockUntil = DATE_ADD(NOW(), INTERVAL 5 MINUTE),
                om.State = 2,
                om.BatchId = @batchId
            WHERE 
                om.LockUntil < NOW()
                AND om.State = 1;

            
            SELECT * 
            FROM OutboxMessages 
            WHERE BatchId = @batchId;
        
        END";

    const string OutBox_Update_SP_Drop = "DROP PROCEDURE IF EXISTS OutBox_Update";
    const string OutBox_Update_SP_Create = @"
        CREATE DEFINER=`root`@`%` PROCEDURE `OutBox_Update`
        (
		    IN IN_MessageId VARCHAR(36),
            IN IN_State smallint
        )
        BEGIN
		     UPDATE OutboxMessages 
		     SET 
				State = IN_State,
				LastModifyDateTime = NOW()
		     WHERE MessageId = IN_MessageId;
        END";

    const string OutBox_Delete_SP_Drop = "DROP PROCEDURE IF EXISTS OutBox_Delete";
    const string OutBox_Delete_SP_Create = @"
        CREATE DEFINER=`root`@`%` PROCEDURE `OutBox_Delete`
        (
		   IN IN_PersistencePeriodInDays int,
           IN IN_State smallint
        )
        BEGIN
		        DELETE FROM OutboxMessages 
		        WHERE DATEDIFF(NOW(), CreateDateTime) > IN_PersistencePeriodInDays
		        AND State = IN_State;
        END";


    public async ValueTask CreateAllStoredProceduresAsync()
    {

        if (_isInitialized) return;

        lock (_lock)
        {
            if (_isInitialized) return;
            _isInitialized = true;
        }

        var procedures = new[]
        {
            ("OutBox_Insert", OutBox_Insert_SP_Drop, OutBox_Insert_SP_Create),
            ("OutBox_Select", OutBox_Select_SP_Drop, OutBox_Select_SP_Create),
            ("OutBox_Update", OutBox_Update_SP_Drop, OutBox_Update_SP_Create),
            ("OutBox_Delete", OutBox_Delete_SP_Drop, OutBox_Delete_SP_Create)
        };

        foreach (var (name, dropCommand, createCommand) in procedures)
        {
            await CreateStoredProcedureAsync(name, dropCommand, createCommand);
        }
    }


    public async Task CreateStoredProcedureAsync(string procedureName, string dropCommand, string createCommand)
    {
        const string checkProcedureExists = @"
        SELECT COUNT(*) 
        FROM INFORMATION_SCHEMA.ROUTINES 
        WHERE ROUTINE_SCHEMA = DATABASE() AND ROUTINE_NAME = @ProcedureName";

        using var connection = _mySqlConnectionFactory.CreateConnection();
        var exists = await connection.ExecuteScalarAsync<int>(
            checkProcedureExists,
            new { ProcedureName = procedureName });

        if (exists > 0) return;

        try
        {
            await connection.ExecuteAsync(dropCommand, commandType: CommandType.Text);
            await connection.ExecuteAsync(createCommand, commandType: CommandType.Text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while creating procedure {procedureName}: {ex.Message}");
        }
    }

}
