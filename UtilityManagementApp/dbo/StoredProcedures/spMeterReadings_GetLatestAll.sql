CREATE PROCEDURE dbo.spMeterReading_GetLatestAll
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Latest AS
    (
        SELECT *,
               ROW_NUMBER() OVER
               (
                   PARTITION BY MeterId
                   ORDER BY ReadingDate DESC, Id DESC
               ) AS [RN]
        FROM dbo.MeterReadings
    )
    SELECT *
    FROM Latest
    WHERE RN = 1
    ORDER BY MeterId;
END