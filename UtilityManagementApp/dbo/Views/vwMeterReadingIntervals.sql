CREATE VIEW dbo.vwMeterReadingIntervals AS
WITH Captures AS (
 SELECT mr.*, LAG(mr.CurrentReading) OVER(PARTITION BY mr.MeterId ORDER BY mr.ReadingDate,mr.Id) AS PriorCounter,
 LAG(mr.ReadingDate) OVER(PARTITION BY mr.MeterId ORDER BY mr.ReadingDate,mr.Id) AS PriorDate
 FROM dbo.MeterReadings mr WHERE mr.IsDeleted=0
)
SELECT r.Id,r.MeterId,r.EmployeeId,r.ReadingDate,r.CurrentReading,
 COALESCE(r.PriorCounter,r.CurrentReading) AS PreviousReading,
 r.CurrentReading-COALESCE(r.PriorCounter,r.CurrentReading) AS Usage,
 r.PriorDate AS PreviousReadingDate, DATEDIFF(DAY,r.PriorDate,r.ReadingDate) AS IntervalDays,
 CONVERT(BIT,CASE WHEN r.PriorDate IS NULL THEN 1 ELSE 0 END) AS IsBaseline,
 r.ImportBatchId,b.FileName AS ImportFileName,r.Notes,r.IsDeleted,r.UpdatedAt,r.UpdatedBy,m.SerialNumber,m.FlowDirection,m.ReplacesMeterId,m.InstalledOn,m.RetiredOn,m.IsActive,m.MeterName,m.MeterType,m.Unit,
 a.AreaName,s.StationName,d.DepartmentName,e.Email AS EmployeeEmail,u.Email AS UpdatedByEmployeeEmail
FROM Captures r JOIN dbo.Meters m ON m.Id=r.MeterId AND m.IsDeleted=0
LEFT JOIN dbo.MeterReadingImportBatches b ON b.Id=r.ImportBatchId
LEFT JOIN dbo.Area a ON a.Id=m.AreaId
LEFT JOIN dbo.Stations s ON s.Id=m.StationId
LEFT JOIN dbo.Departments d ON d.Id=m.DepartmentId
LEFT JOIN dbo.Employees e ON e.Id=r.EmployeeId
LEFT JOIN dbo.Employees u ON u.Id=r.UpdatedBy;
