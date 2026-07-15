CREATE PROCEDURE [dbo].[spDailyPeopleCount_ByDateRange]
  @StartDate DATE,
    @EndDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        dpc.Id,
        dpc.Visitors AS Visitors,
        dpc.Employees AS Employees,
        dpc.Total AS Total,
        dpc.[Date] AS [Date]
    FROM dbo.DailyPeopleCount AS dpc
    WHERE dpc.[Date] >= @StartDate
      AND dpc.[Date] < DATEADD(DAY, 1, @EndDate)
    ORDER BY dpc.[Date] DESC, dpc.Id DESC;
END