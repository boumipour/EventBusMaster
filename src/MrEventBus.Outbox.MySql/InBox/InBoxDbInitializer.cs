using Dapper;
using MrEventBus.Boxing.MySql.Infrastructure;
using System.Data;

namespace MrEventBus.Boxing.MySql.InBox;

public class InBoxDbInitializer
{
    private readonly IMySqlConnectionFactory _mySqlConnectionFactory;

    private static bool _isInitialized;
    private static readonly object _lock = new();

    public InBoxDbInitializer(IMySqlConnectionFactory mySqlConnectionFactory)
    {
        _mySqlConnectionFactory = mySqlConnectionFactory;
    }

    const string InBox_tbl_Create = @"
    CREATE TABLE `InboxMessages`  
    (
      `MessageId` varchar(36) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
      `Type` varchar(500) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
      `Data` text CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
      `State` smallint(6) NOT NULL,
      `Shard` varchar(36) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
      `ShardOffset` int(11) NOT NULL,
      `PublishDateTime` datetime NULL DEFAULT NULL,  
      `CreateDateTime` datetime NOT NULL,
      `LastModifyDateTime` datetime NULL DEFAULT NULL,
      `LockUntil` datetime NULL DEFAULT NULL,
      `BatchId` varchar(36) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL
    ) 
    ENGINE = InnoDB CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;";

    const string InBox_Select_SP_Create = @" 
    CREATE DEFINER=`root`@`%` PROCEDURE `InBox_Select`()
    BEGIN
		    
            SET @batchId = UUID();

            UPDATE InboxMessages om
            JOIN (
                SELECT MIN(ShardOffset) AS minShardOffset, Shard
                FROM InboxMessages
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
            FROM InboxMessages 
            WHERE BatchId = @batchId;
        
    END";

    const string InBox_Select_ById_SP_Create = @"
    CREATE DEFINER=`root`@`%` PROCEDURE `InBox_Select_ById`
    (
        IN IN_MessageId VARCHAR(36)
    )
    BEGIN
        SELECT * 
        FROM InboxMessages 
        WHERE MessageId = IN_MessageId;
    END";

    const string InBox_Insert_SP_Create = @"
    CREATE DEFINER=`root`@`%` PROCEDURE `InBox_Insert`
    (
	      IN IN_MessageId VARCHAR(36),
        IN IN_Type VARCHAR(500),
        IN IN_Data TEXT,
        IN IN_Shard VARCHAR(36),
        IN IN_State smallint,
        IN IN_PublishDateTime DATETIME,
        IN IN_CreateDateTime DATETIME,
        IN IN_LastModifyDateTime DATETIME
    )
    BEGIN
 
	    DECLARE ShardOffset INT;

        SELECT COUNT(*) INTO ShardOffset
        FROM InboxMessages
        WHERE Shard = IN_Shard;
	
	
	    INSERT INTO db.InboxMessages (MessageId,Type,DATA,Shard,ShardOffset,State,PublishDateTime,CreateDateTime,LastModifyDateTime,LockUntil)
	    VALUES
	    (
	        IN_MessageId,
	        IN_Type,
	        IN_Data,
	        IN_Shard,
	        ShardOffset+1,
	        IN_State,
            IN_PublishDateTime,
	        IN_CreateDateTime,
	        IN_LastModifyDateTime,
	        ADDTIME(NOW(), SEC_TO_TIME(-5*60))
	    );
    END";

    const string InBox_Update_SP_Create = @"
    CREATE DEFINER=`root`@`%` PROCEDURE `InBox_Update`
    (
        IN IN_MessageId VARCHAR(36),
        IN IN_State smallint
    )
    BEGIN
		UPDATE InboxMessages 
		SET 
		    State = IN_State,
			LastModifyDateTime = NOW()
		WHERE MessageId = IN_MessageId;
    END";

    const string InBox_Delete_SP_Create = @"
    CREATE DEFINER=`root`@`%` PROCEDURE `InBox_Delete`
    (
	   IN IN_PersistencePeriodInDays int,
       IN IN_State smallint
    )
    BEGIN
	   DELETE FROM InboxMessages 
	   WHERE DATEDIFF(NOW(), CreateDateTime) > IN_PersistencePeriodInDays
	   AND State = IN_State;
    END";


    public async ValueTask InitializeAsync()
    {
        if (_isInitialized) return;

        lock (_lock)
        {
            if (_isInitialized) return;
            _isInitialized = true;
        }

        var tables = new[]
        {
            ("InboxMessages",  InBox_tbl_Create),
        };


        var procedures = new[]
        {
            ("InBox_Select",  InBox_Select_SP_Create),
            ("InBox_Select_byId",  InBox_Select_ById_SP_Create),
            ("InBox_Insert",  InBox_Insert_SP_Create),
            ("InBox_Update",  InBox_Update_SP_Create),
            ("InBox_Delete",  InBox_Delete_SP_Create)
        };

        foreach (var (name, createCommand) in tables)
        {
            await CreateTableAsync(name, createCommand);
        }

        foreach (var (name, createCommand) in procedures)
        {
            await CreateStoredProcedureAsync(name,  createCommand);
        }
    }

    private async Task CreateStoredProcedureAsync(string procedureName, string createCommand)
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
            await connection.ExecuteAsync(createCommand, commandType: CommandType.Text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while creating procedure {procedureName}: {ex.Message}");
        }
    }

    private async Task CreateTableAsync(string tableName, string createCommand)
    {
        const string checkTableExists = @"
        SELECT *
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_NAME = @TableName";

        using var connection = _mySqlConnectionFactory.CreateConnection();
        var exists = await connection.ExecuteScalarAsync<int>(
            checkTableExists,
            new { TableName = tableName });

        if (exists > 0) return;

        try
        {
            await connection.ExecuteAsync(createCommand, commandType: CommandType.Text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while creating table {tableName}: {ex.Message}");
        }
    }
}
