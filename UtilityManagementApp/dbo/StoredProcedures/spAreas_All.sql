CREATE PROCEDURE dbo.spAreas_All AS
BEGIN SET NOCOUNT ON; SELECT Id,AreaName,StationId,IsActive,CreatedAt FROM dbo.Area WHERE IsActive=1 ORDER BY AreaName; END;
