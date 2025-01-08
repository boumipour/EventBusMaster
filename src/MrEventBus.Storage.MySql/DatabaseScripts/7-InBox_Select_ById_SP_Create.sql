CREATE PROCEDURE `InBox_Select_ById`
    (
        IN IN_MessageId VARCHAR(36)
    )
    BEGIN
        SELECT * 
        FROM InboxMessages 
        WHERE MessageId = IN_MessageId;
    END