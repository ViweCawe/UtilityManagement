CREATE PROCEDURE [dbo].[spMeterReading_GetLatestByMeterId]
@Id INT 
AS
BEGIN
	SET NOCOUNT ON;

SELECT mr.*
FROM MeterReadings mr
INNER JOIN
(
    SELECT
        MeterId,
        MAX(ReadingDate) AS LatestDate
    FROM MeterReadings
    GROUP BY MeterId
) latest

ON mr.MeterId = latest.MeterId
AND mr.ReadingDate = latest.LatestDate

ORDER BY mr.ReadingDate;

END