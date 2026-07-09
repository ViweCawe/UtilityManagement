CREATE PROCEDURE [dbo].[spMeters_All]
AS
BEGIN 
		SET NOCOUNT ON;
		SELECT[m].[Id],
		[m].[MeterName],
		[m].[MeterType],
		[m].[Unit],
		[m].[IsActive],


		[a].[AreaName],
		[s].[StationName],
		[d].[DepartmentName]

		FROM dbo.Meters [m]
		LEFT JOIN dbo.Area [a]
		ON [m].AreaId = [a].Id
		LEFT JOIN dbo.Stations [s]
		ON [m].StationId = [s].Id
		LEFT JOIN dbo.Departments [d]
		ON [m].DepartmentId = [d].[Id]
			WHERE [m].IsActive = 1
			ORDER BY [m].MeterName;
			
END