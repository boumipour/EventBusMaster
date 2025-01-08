CREATE PROCEDURE `InBox_Insert`
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
	    )
        ON DUPLICATE KEY UPDATE
        LastModifyDateTime = VALUES(LastModifyDateTime);
    END