CREATE PROCEDURE [dbo].[spMeterReadings_All]
AS
BEGIN
	SET NOCOUNT ON;
	SELECT [mr].[Id],
		   [mr].[ReadingDate],
		   [mr].[CurrentReading],
		   [mr].[PreviousReading],
		   [mr].[Usage],
		   [mr].[Notes],


		   [m].Id AS MeterId,
		   [m].[MeterType],


		   [a].[AreaName],
		   [s].[StationName],
		   [d].[DepartmentName],

		   [e].Id AS EmployeeId


	FROM dbo.MeterReadings [mr]
	INNER JOIN dbo.Meters [m]
	ON [mr].MeterId = [m].Id

	LEFT JOIN dbo.Area [a]
	ON [m].AreaId = [a].Id

	LEFT JOIN dbo.Stations [s]
	ON [m].StationId = [s].Id

	LEFT JOIN dbo.Employees [e]
	ON [mr].EmployeeId = [e].Id

	LEFT JOIN dbo.Departments [d]
	ON [m].DepartmentId = [d].Id



	WHERE [m].IsActive = 1
	ORDER BY ReadingDate DESC;


END