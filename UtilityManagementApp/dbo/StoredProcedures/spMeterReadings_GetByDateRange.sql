CREATE PROCEDURE dbo.spMeterReadings_GetByDateRange @StartDate DATE,@EndDate DATE AS
BEGIN SET NOCOUNT ON; SELECT * FROM dbo.vwMeterReadingIntervals WHERE IsActive=1 AND ReadingDate>=@StartDate AND ReadingDate<DATEADD(DAY,1,@EndDate) ORDER BY ReadingDate DESC,Id DESC; END;
