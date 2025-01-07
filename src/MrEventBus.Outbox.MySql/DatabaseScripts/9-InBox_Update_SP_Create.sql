 CREATE PROCEDURE `InBox_Update`
    (
        IN IN_MessageId VARCHAR(36),
        IN IN_State smallint
    )
    BEGIN
        SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

		UPDATE InboxMessages 
		SET 
		    State = IN_State,
			LastModifyDateTime = NOW()
		WHERE MessageId = IN_MessageId;
    END