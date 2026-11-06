CREATE PROCEDURE dbo.spMeterReadings_Import
 @RowsJson NVARCHAR(MAX),@EmployeeId INT,@FileName NVARCHAR(200),@FileHash CHAR(64),@Inserted INT OUTPUT
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON; SET @Inserted=0;
 IF ISJSON(@RowsJson)<>1 THROW 51120,'Invalid import JSON.',1;
 SELECT MeterId,ReadingDate,Counter,Unit INTO #Rows FROM OPENJSON(@RowsJson) WITH(MeterId INT '$.MeterId',ReadingDate DATETIME2(0) '$.ReadingDate',Counter DECIMAL(19,6) '$.Counter',Unit NVARCHAR(20) '$.Unit');
 IF NOT EXISTS(SELECT 1 FROM #Rows) OR (SELECT COUNT(*) FROM #Rows)>10000 THROW 51121,'Use 1 to 10000 captures.',1;
 IF EXISTS(SELECT 1 FROM #Rows GROUP BY MeterId,ReadingDate HAVING COUNT(*)>1) THROW 51122,'Duplicate capture in file.',1;
 BEGIN TRY BEGIN TRANSACTION;
 -- Every reading writer locks its physical meter first; this also serializes replacement/import races.
 SELECT m.Id INTO #Locked FROM dbo.Meters m WITH(UPDLOCK,HOLDLOCK) WHERE m.Id IN(SELECT MeterId FROM #Rows);
 IF EXISTS(SELECT 1 FROM #Rows r LEFT JOIN dbo.Meters m ON m.Id=r.MeterId WHERE m.Id IS NULL OR m.IsDeleted=1 OR r.Unit IS NULL OR r.Counter IS NULL OR r.Counter<0 OR r.ReadingDate IS NULL OR r.ReadingDate<'17530101' OR r.ReadingDate>GETDATE() OR (m.InstalledOn IS NOT NULL AND r.ReadingDate<m.InstalledOn) OR (m.RetiredOn IS NOT NULL AND r.ReadingDate>m.RetiredOn) OR (m.IsActive=0 AND m.RetiredOn IS NULL) OR CASE WHEN r.Unit=N'm3' THEN N'm³' ELSE r.Unit END<>m.Unit OR NOT((m.MeterType=1 AND m.Unit IN(N'L',N'kL',N'm³')) OR (m.MeterType=2 AND m.Unit=N'kWh')))
 THROW 51123,'Invalid meter, unit, counter or service date in import.',1;
 IF EXISTS(SELECT 1 FROM #Rows r JOIN dbo.MeterReadings m ON m.MeterId=r.MeterId AND m.ReadingDate=r.ReadingDate AND m.IsDeleted=0 WHERE m.CurrentReading<>r.Counter) THROW 51124,'Conflicting capture already exists.',1;
 SELECT r.* INTO #New FROM #Rows r WHERE NOT EXISTS(SELECT 1 FROM dbo.MeterReadings m WHERE m.MeterId=r.MeterId AND m.ReadingDate=r.ReadingDate AND m.IsDeleted=0);
 ;WITH Combined AS(
  SELECT mr.MeterId,mr.ReadingDate,mr.CurrentReading AS Counter,0 AS IsNew FROM dbo.MeterReadings mr WHERE mr.IsDeleted=0 AND mr.MeterId IN(SELECT MeterId FROM #Rows)
  UNION ALL SELECT MeterId,ReadingDate,Counter,1 FROM #New
 ), Chain AS(SELECT *,LAG(Counter) OVER(PARTITION BY MeterId ORDER BY ReadingDate) AS PriorCounter,LAG(IsNew) OVER(PARTITION BY MeterId ORDER BY ReadingDate) AS PriorNew FROM Combined)
 SELECT MeterId INTO #Bad FROM Chain WHERE Counter<PriorCounter AND (IsNew=1 OR PriorNew=1);
 IF EXISTS(SELECT 1 FROM #Bad) THROW 51125,'Counter decreases within a physical meter. Review replacement IDs.',1;
 SET @Inserted=(SELECT COUNT(*) FROM #New);
 IF @Inserted=0 BEGIN COMMIT; RETURN; END;
 INSERT dbo.MeterReadingImportBatches(FileName,FileHash,EmployeeId,InsertedCount) VALUES(@FileName,@FileHash,@EmployeeId,@Inserted);
 DECLARE @BatchId INT=CONVERT(INT,SCOPE_IDENTITY());
 INSERT dbo.MeterReadings(MeterId,EmployeeId,ReadingDate,CurrentReading,PreviousReading,Notes,ImportBatchId) SELECT MeterId,@EmployeeId,ReadingDate,Counter,Counter,N'Historical spreadsheet import',@BatchId FROM #New;
 ;WITH Chain AS(SELECT Id,CurrentReading,LAG(CurrentReading) OVER(PARTITION BY MeterId ORDER BY ReadingDate,Id) AS PriorCounter FROM dbo.MeterReadings WHERE IsDeleted=0 AND MeterId IN(SELECT MeterId FROM #Rows))
 UPDATE mr SET PreviousReading=COALESCE(c.PriorCounter,c.CurrentReading) FROM dbo.MeterReadings mr JOIN Chain c ON c.Id=mr.Id;
 COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
