CREATE  PROCEDURE [dbo].[spMeterReading_GetLatestByMeterId]
(
    @MeterId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        mr.*
    FROM dbo.MeterReadings AS mr
    WHERE mr.MeterId = @MeterId
    ORDER BY
        mr.ReadingDate DESC,
        mr.Id DESC;
END;
GO