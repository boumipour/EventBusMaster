CREATE PROCEDURE `OutBox_Select`()
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
        
END