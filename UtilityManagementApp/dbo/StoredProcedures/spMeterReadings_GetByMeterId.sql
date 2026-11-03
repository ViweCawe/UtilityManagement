CREATE PROCEDURE dbo.spMeterReadings_GetByMeterId @MeterId INT AS
BEGIN SET NOCOUNT ON; SELECT * FROM dbo.vwMeterReadingIntervals WHERE MeterId=@MeterId ORDER BY ReadingDate DESC,Id DESC; END;
