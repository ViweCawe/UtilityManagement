CREATE PROCEDURE dbo.spAdminLocations_All AS
BEGIN SET NOCOUNT ON; SELECT Id,StationName AS Name,IsActive FROM dbo.Stations ORDER BY StationName; END;
