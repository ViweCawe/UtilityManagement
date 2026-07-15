CREATE PROCEDURE [dbo].[spDailyPeopleCount_GetAll]

AS
BEGIN
	SET NOCOUNT ON;
	SELECT
		[Id],
		[Visitors],
		[Employees],
		[Total],
		[Date]
	FROM dbo.DailyPeopleCount
	ORDER BY [Date];
END