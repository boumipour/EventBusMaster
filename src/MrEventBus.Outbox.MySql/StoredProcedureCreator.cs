using MySqlConnector;

namespace MrEventBus.Boxing.MySql;

public class StoredProcedureCreator
{
    private readonly MySqlConnection _mySqlConnection;

    public StoredProcedureCreator(MySqlConnection mySqlConnection)
    {
        _mySqlConnection = mySqlConnection;
    }

    public async Task CreateStoredProcedureAsync()
    {
        const string OutBox_Insert = @"
        DROP PROCEDURE IF EXISTS OutBox_Insert

        CREATE DEFINER=`root`@`%` PROCEDURE `OutBox_Insert`(
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

        const string OutBox_Select = @"
        DROP PROCEDURE IF EXISTS OutBox_Select

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

        const string OutBox_Update = @"
        DROP PROCEDURE IF EXISTS OutBox_Update

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

        const string OutBox_Delete = @"
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

        try
        {
            await _mySqlConnection.OpenAsync();

            await using var command1 = new MySqlCommand(OutBox_Insert, _mySqlConnection);
            await command1.ExecuteNonQueryAsync();

            await using var command2 = new MySqlCommand(OutBox_Select, _mySqlConnection);
            await command2.ExecuteNonQueryAsync();

            await using var command3 = new MySqlCommand(OutBox_Update, _mySqlConnection);
            await command2.ExecuteNonQueryAsync();

            await using var command4 = new MySqlCommand(OutBox_Delete, _mySqlConnection);
            await command2.ExecuteNonQueryAsync();

            Console.WriteLine("Stored procedure created successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}
