 CREATE TABLE `OutboxMessages`  
    (
        `MessageId` varchar(36) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
        `Type` varchar(500) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
        `Data` text CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
        `State` smallint(6) NOT NULL,        
        `Shard` varchar(36) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL,
        `ShardOffset` int(11) NOT NULL,
        `QueueName` varchar(200) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL,
        `CreateDateTime` datetime NOT NULL,
        `LastModifyDateTime` datetime NULL DEFAULT NULL,
        `LockUntil` datetime NULL DEFAULT NULL,
        `BatchId` varchar(36) CHARACTER SET latin1 COLLATE latin1_swedish_ci NULL DEFAULT NULL
    ) 
    ENGINE = InnoDB CHARACTER SET = latin1 COLLATE = latin1_swedish_ci ROW_FORMAT = Dynamic;