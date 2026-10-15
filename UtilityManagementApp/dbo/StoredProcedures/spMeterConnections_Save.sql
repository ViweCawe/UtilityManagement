CREATE PROCEDURE [dbo].[spMeterConnections_Save]
    @UpstreamMeterId INT,
    @DownstreamMeterId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @LockResult INT;
    EXEC @LockResult = sp_getapplock @Resource = 'MeterConnections_Write',
        @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
    IF @LockResult < 0 THROW 51000, 'Could not acquire meter mapping lock.', 1;
    IF @UpstreamMeterId = @DownstreamMeterId THROW 51001, 'A meter cannot feed itself.', 1;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.Meters p JOIN dbo.Meters c ON c.Id = @DownstreamMeterId
        WHERE p.Id = @UpstreamMeterId AND p.MeterType = c.MeterType
          AND p.IsActive = 1 AND c.IsActive = 1
          AND p.IsDeleted = 0 AND c.IsDeleted = 0
    ) THROW 51002, 'Meters must exist, be active, and have the same type.', 1;

    ;WITH Ancestors AS (
        SELECT UpstreamMeterId, DownstreamMeterId
        FROM dbo.MeterConnections
        WHERE IsActive = 1 AND ValidTo IS NULL AND DownstreamMeterId = @UpstreamMeterId
        UNION ALL
        SELECT mc.UpstreamMeterId, mc.DownstreamMeterId
        FROM dbo.MeterConnections mc
        JOIN Ancestors a ON mc.DownstreamMeterId = a.UpstreamMeterId
        WHERE mc.IsActive = 1 AND mc.ValidTo IS NULL
    )
    SELECT UpstreamMeterId INTO #Ancestors FROM Ancestors OPTION (MAXRECURSION 100);
    IF EXISTS (SELECT 1 FROM #Ancestors WHERE UpstreamMeterId = @DownstreamMeterId)
        THROW 51003, 'This connection would create a circular hierarchy.', 1;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    IF EXISTS (SELECT 1 FROM dbo.MeterConnections
        WHERE DownstreamMeterId = @DownstreamMeterId AND UpstreamMeterId = @UpstreamMeterId
          AND IsActive = 1 AND ValidTo IS NULL)
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;
    UPDATE dbo.MeterConnections
       SET IsActive = 0, ValidTo = @Now
     WHERE DownstreamMeterId = @DownstreamMeterId AND IsActive = 1 AND ValidTo IS NULL;
    INSERT INTO dbo.MeterConnections (UpstreamMeterId, DownstreamMeterId, ValidFrom, IsActive)
    VALUES (@UpstreamMeterId, @DownstreamMeterId, @Now, 1);
    COMMIT TRANSACTION;
END
