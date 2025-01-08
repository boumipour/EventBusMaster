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
		    )
            ON DUPLICATE KEY UPDATE
            LastModifyDateTime = VALUES(LastModifyDateTime);
        END