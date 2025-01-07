 CREATE PROCEDURE `InBox_Select`()
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
        
    END