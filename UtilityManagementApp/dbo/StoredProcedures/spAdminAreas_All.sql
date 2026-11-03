CREATE PROCEDURE dbo.spAdminAreas_All AS
BEGIN SET NOCOUNT ON;
 SELECT a.Id,a.AreaName AS Name,a.StationId,s.StationName,a.IsActive FROM dbo.Area a LEFT JOIN dbo.Stations s ON s.Id=a.StationId ORDER BY a.AreaName;
END;
