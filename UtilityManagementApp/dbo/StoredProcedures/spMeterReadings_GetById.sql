CREATE PROCEDURE [dbo].[spMeterReadingS_GetById]
	(
	@Id INT
	)
AS
BEGIN
	SET NOCOUNT ON;
	SELECT 
	[mr].[Id],
	[mr].[ReadingDate],
	[mr].[CurrentReading], 
	[mr].[PreviousReading],
	[mr].[Usage],
	[mr].[Notes],

	[m].[Id] AS [MeterId],
	[m].[MeterType],
	[m].[MeterName],

	[a].[AreaName],
	[s].[StationName],
	[d].[DepartmentName],
	

	[e].[Id] AS [Employees]


	FROM dbo.MeterReadings [mr]
	INNER JOIN dbo.Meters [m] 
	ON [mr].MeterId = [m].Id

	LEFT JOIN dbo.Area [a]
	ON [m].AreaId = [a].Id


	LEFT JOIN dbo.Stations [s]
	ON [m].StationId = [s].Id


	LEFT JOIN dbo.Departments [d]
	ON [m].DepartmentId = [d].Id

	LEFT JOIN dbo.Employees [e]
	ON [mr].EmployeeId = [e].Id



	WHERE [mr].Id = @Id
	AND [m].IsActive = 1;
	

END