CREATE PROCEDURE dbo.spMeterReading_GetLatestByMeterId @MeterId INT AS
BEGIN SET NOCOUNT ON; SELECT TOP(1) * FROM dbo.vwMeterReadingIntervals WHERE MeterId=@MeterId ORDER BY ReadingDate DESC,Id DESC; END;
