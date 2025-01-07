  CREATE PROCEDURE `OutBox_Delete`
        (
		   IN IN_PersistencePeriodInDays int,
           IN IN_State smallint
        )
        BEGIN
		        DELETE FROM OutboxMessages 
		        WHERE DATEDIFF(NOW(), CreateDateTime) > IN_PersistencePeriodInDays
		        AND State = IN_State;
        END