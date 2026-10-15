CREATE PROCEDURE [dbo].[spMeterConnections_Disconnect]
    @DownstreamMeterId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @LockResult INT;
    EXEC @LockResult = sp_getapplock @Resource = 'MeterConnections_Write',
        @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
    IF @LockResult < 0 THROW 51004, 'Could not acquire meter mapping lock.', 1;
    UPDATE dbo.MeterConnections
       SET IsActive = 0, ValidTo = SYSUTCDATETIME()
     WHERE DownstreamMeterId = @DownstreamMeterId AND IsActive = 1 AND ValidTo IS NULL;
    COMMIT TRANSACTION;
END
