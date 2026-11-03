CREATE PROCEDURE dbo.spMeterReadings_All AS
BEGIN SET NOCOUNT ON; SELECT * FROM dbo.vwMeterReadingIntervals WHERE IsActive=1 ORDER BY ReadingDate DESC,Id DESC; END;
