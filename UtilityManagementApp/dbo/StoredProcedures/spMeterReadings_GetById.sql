CREATE PROCEDURE dbo.spMeterReadings_GetById @Id INT AS
BEGIN SET NOCOUNT ON; SELECT * FROM dbo.vwMeterReadingIntervals WHERE Id=@Id; END;
