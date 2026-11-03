CREATE PROCEDURE dbo.spStation_All AS
BEGIN SET NOCOUNT ON; SELECT Id,StationName,IsActive,CreatedAt FROM dbo.Stations WHERE IsActive=1 ORDER BY StationName; END;
