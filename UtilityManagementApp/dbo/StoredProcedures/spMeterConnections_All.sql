CREATE PROCEDURE [dbo].[spMeterConnections_All]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [Id], [UpstreamMeterId], [DownstreamMeterId], [ValidFrom], [ValidTo], [IsActive]
    FROM [dbo].[MeterConnections]
    ORDER BY [UpstreamMeterId], [DownstreamMeterId], [ValidFrom];
END
