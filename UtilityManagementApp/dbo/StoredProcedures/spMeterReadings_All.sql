CREATE PROCEDURE dbo.spMeterReadings_All AS
BEGIN SET NOCOUNT ON; SELECT * FROM dbo.vwMeterReadingIntervals ORDER BY ReadingDate DESC,Id DESC; END;
