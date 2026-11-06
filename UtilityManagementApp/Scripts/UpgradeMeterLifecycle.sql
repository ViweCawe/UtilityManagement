-- Run after earlier meter-assets/network/interval upgrades and before deploying the new web build.
-- Review/backup the database through your normal deployment process. No readings are deleted.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.Meters','SerialNumber') IS NULL ALTER TABLE dbo.Meters ADD SerialNumber NVARCHAR(100) NOT NULL CONSTRAINT DF_Meters_Serial DEFAULT '';
IF COL_LENGTH('dbo.Meters','FlowDirection') IS NULL ALTER TABLE dbo.Meters ADD FlowDirection INT NOT NULL CONSTRAINT DF_Meters_FlowDirection DEFAULT 0;
IF COL_LENGTH('dbo.Meters','ReplacesMeterId') IS NULL ALTER TABLE dbo.Meters ADD ReplacesMeterId INT NULL;
IF COL_LENGTH('dbo.Meters','InstalledOn') IS NULL ALTER TABLE dbo.Meters ADD InstalledOn DATETIME2(0) NULL;
IF COL_LENGTH('dbo.Meters','RetiredOn') IS NULL ALTER TABLE dbo.Meters ADD RetiredOn DATETIME2(0) NULL;
IF OBJECT_ID('dbo.MeterReadingImportBatches','U') IS NULL
 CREATE TABLE dbo.MeterReadingImportBatches(Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,FileName NVARCHAR(200) NOT NULL,FileHash CHAR(64) NOT NULL,EmployeeId INT NOT NULL REFERENCES dbo.Employees(Id),ImportedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),InsertedCount INT NOT NULL);
IF COL_LENGTH('dbo.MeterReadings','ImportBatchId') IS NULL ALTER TABLE dbo.MeterReadings ADD ImportBatchId INT NULL;
-- Use a new batch below so added columns are visible when compiling dependent objects.
GO
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_Meters_Replaces') ALTER TABLE dbo.Meters ADD CONSTRAINT FK_Meters_Replaces FOREIGN KEY(ReplacesMeterId) REFERENCES dbo.Meters(Id);
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_Meters_Direction') ALTER TABLE dbo.Meters ADD CONSTRAINT CK_Meters_Direction CHECK(FlowDirection IN(0,1,2));
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_Meters_Service') ALTER TABLE dbo.Meters ADD CONSTRAINT CK_Meters_Service CHECK((InstalledOn IS NULL OR RetiredOn IS NULL OR InstalledOn<=RetiredOn) AND (ReplacesMeterId IS NULL OR ReplacesMeterId<>Id));
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_Meters_Replacement' AND object_id=OBJECT_ID('dbo.Meters')) CREATE UNIQUE INDEX UX_Meters_Replacement ON dbo.Meters(ReplacesMeterId) WHERE ReplacesMeterId IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_MeterReadings_Import') ALTER TABLE dbo.MeterReadings ADD CONSTRAINT FK_MeterReadings_Import FOREIGN KEY(ImportBatchId) REFERENCES dbo.MeterReadingImportBatches(Id);
IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.MeterReadings') AND name='CurrentReading' AND (TYPE_NAME(user_type_id)<>'decimal' OR scale<>6))
BEGIN
 IF COL_LENGTH('dbo.MeterReadings','Usage') IS NOT NULL ALTER TABLE dbo.MeterReadings DROP COLUMN Usage;
 ALTER TABLE dbo.MeterReadings ALTER COLUMN CurrentReading DECIMAL(19,6) NOT NULL;
 ALTER TABLE dbo.MeterReadings ALTER COLUMN PreviousReading DECIMAL(19,6) NOT NULL;
 ALTER TABLE dbo.MeterReadings ADD Usage AS(CurrentReading-PreviousReading) PERSISTED;
END;
GO
-- Normalize equivalent unit spellings only; original counter numbers are unchanged.
UPDATE dbo.Meters SET Unit=CASE LOWER(LTRIM(RTRIM(Unit)))
 WHEN N'l' THEN N'L' WHEN N'litre' THEN N'L' WHEN N'litres' THEN N'L' WHEN N'liter' THEN N'L' WHEN N'liters' THEN N'L'
 WHEN N'm3' THEN N'm³' WHEN N'm³' THEN N'm³' WHEN N'kl' THEN N'kL' WHEN N'kwh' THEN N'kWh' ELSE Unit END;
GO
CREATE OR ALTER VIEW dbo.vwMeterReadingIntervals AS
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

GO
CREATE OR ALTER PROCEDURE [dbo].[spMeters_All]
AS
BEGIN 
		SET NOCOUNT ON;
		SELECT[m].[Id], [m].[SerialNumber], [m].[FlowDirection], [m].[ReplacesMeterId], [m].[InstalledOn], [m].[RetiredOn], [m].[AssetKind], [m].[NetworkRole], [m].[CapacityLitres],
        [m].[AreaId], [m].[DepartmentId], [m].[StationId], [m].[IsCumulative],
		[m].[MeterName],
		[m].[MeterType],
		[m].[Unit],
		[m].[IsActive],


		[a].[AreaName],
		[s].[StationName],
		[d].[DepartmentName]

		FROM dbo.Meters [m]
		LEFT JOIN dbo.Area [a]
		ON [m].AreaId = [a].Id
		LEFT JOIN dbo.Stations [s]
		ON [m].StationId = [s].Id
		LEFT JOIN dbo.Departments [d]
		ON [m].DepartmentId = [d].[Id]
			WHERE [m].IsDeleted = 0
			ORDER BY [m].MeterName;
			
END
GO
CREATE OR ALTER PROCEDURE [dbo].[spMeters_GetById]
    @Id INT
AS
BEGIN 
	SET NOCOUNT ON;
	SELECT 
		[m].[Id], [m].[SerialNumber], [m].[FlowDirection], [m].[ReplacesMeterId], [m].[InstalledOn], [m].[RetiredOn], [m].[AssetKind], [m].[NetworkRole], [m].[CapacityLitres],
        [m].[AreaId], [m].[DepartmentId], [m].[StationId], [m].[IsCumulative],
		[m].[MeterName],
		[m].[MeterType],
		[m].[Unit],
		[m].[IsActive],


		[a].[AreaName],
		[s].[StationName],
		[d].[DepartmentName]

		FROM dbo.Meters [m]
		LEFT JOIN dbo.Area [a]
		ON [m].AreaId = [a].Id
		LEFT JOIN dbo.Stations [s]
		ON [m].StationId = [s].Id
		LEFT JOIN dbo.Departments [d]
		ON [m].DepartmentId = [d].[Id]
		WHERE [m].Id = @Id
		AND [m].IsDeleted = 0;
END
GO
CREATE OR ALTER PROCEDURE dbo.spMeters_Insert
 @MeterName NVARCHAR(100),@MeterType INT,@Unit NVARCHAR(20),@AreaId INT,@DepartmentId INT,@StationId INT,@IsCumulative BIT,@Id INT OUTPUT,
 @AssetKind INT=1,@NetworkRole INT=4,@CapacityLitres DECIMAL(14,2)=NULL,
 @SerialNumber NVARCHAR(100)='',@FlowDirection INT=0,@ReplacesMeterId INT=NULL,@InstalledOn DATETIME2(0)=NULL
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF NOT ((@MeterType=1 AND @Unit IN(N'L',N'kL',N'm³')) OR (@MeterType=2 AND @Unit=N'kWh')) THROW 51100,'Invalid counter unit. m² is not water volume.',1;
 IF @InstalledOn>GETDATE() THROW 51101,'Installation cannot be in the future.',1;
 BEGIN TRY BEGIN TRANSACTION;
 IF @ReplacesMeterId IS NOT NULL
 BEGIN
  DECLARE @Revision INT;
  SELECT @Revision=Revision FROM dbo.WaterNetworkDocument WITH(UPDLOCK,HOLDLOCK) WHERE DocumentId=1;
  IF @Revision IS NULL INSERT dbo.WaterNetworkDocument(DocumentId,NetworkJson,Revision) VALUES(1,NULL,0);
  IF @InstalledOn IS NULL THROW 51102,'A replacement needs a cutover time.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE Id=@ReplacesMeterId AND IsDeleted=0 AND MeterType=@MeterType AND AssetKind=@AssetKind AND NetworkRole=@NetworkRole AND AreaId=@AreaId AND StationId=@StationId AND DepartmentId=@DepartmentId AND FlowDirection=@FlowDirection AND (InstalledOn IS NULL OR InstalledOn<@InstalledOn)) THROW 51103,'Replacement must retain the same measurement boundary, utility and placement.',1;
  IF EXISTS(SELECT 1 FROM dbo.Meters WHERE ReplacesMeterId=@ReplacesMeterId) THROW 51104,'This meter already has a successor.',1;
  IF EXISTS(SELECT 1 FROM dbo.MeterReadings WHERE MeterId=@ReplacesMeterId AND IsDeleted=0 AND ReadingDate>@InstalledOn) THROW 51105,'Old meter has readings after the cutover.',1;
  UPDATE dbo.Meters SET IsActive=0,RetiredOn=@InstalledOn WHERE Id=@ReplacesMeterId;
 END;
 INSERT dbo.Meters(MeterName,MeterType,Unit,AreaId,DepartmentId,StationId,IsCumulative,AssetKind,NetworkRole,CapacityLitres,SerialNumber,FlowDirection,ReplacesMeterId,InstalledOn)
 VALUES(@MeterName,@MeterType,@Unit,@AreaId,@DepartmentId,@StationId,@IsCumulative,@AssetKind,@NetworkRole,@CapacityLitres,@SerialNumber,@FlowDirection,@ReplacesMeterId,@InstalledOn);
 SET @Id=CONVERT(INT,SCOPE_IDENTITY());
 IF @ReplacesMeterId IS NOT NULL BEGIN
  UPDATE dbo.WaterNetworkLinks SET MeterId=@Id WHERE MeterId=@ReplacesMeterId;
  UPDATE dbo.WaterNetworkDocument SET Revision=Revision+1,UpdatedAt=SYSUTCDATETIME() WHERE DocumentId=1;
 END;
 COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;

GO
CREATE OR ALTER PROCEDURE dbo.spMeters_UpdateDetails
 @Id INT,@MeterType INT,@MeterName NVARCHAR(100),@Unit NVARCHAR(20),@AreaId INT,@DepartmentId INT,@StationId INT,@IsCumulative BIT,@IsActive BIT,
 @AssetKind INT=1,@NetworkRole INT=4,@CapacityLitres DECIMAL(14,2)=NULL,@SerialNumber NVARCHAR(100)='',@FlowDirection INT=0,@InstalledOn DATETIME2(0)=NULL
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY BEGIN TRANSACTION;
 DECLARE @lock INT; SELECT @lock=Id FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id AND IsDeleted=0;
 IF @lock IS NULL THROW 51106,'Meter not found.',1;
 IF NOT ((@MeterType=1 AND @Unit IN(N'L',N'kL',N'm³')) OR (@MeterType=2 AND @Unit=N'kWh')) THROW 51100,'Invalid counter unit.',1;
 IF @InstalledOn>GETDATE() THROW 51101,'Installation cannot be in the future.',1;
 IF EXISTS(SELECT 1 FROM dbo.Meters m WHERE m.Id=@Id AND (EXISTS(SELECT 1 FROM dbo.MeterReadings WHERE MeterId=@Id) OR m.ReplacesMeterId IS NOT NULL OR EXISTS(SELECT 1 FROM dbo.Meters n WHERE n.ReplacesMeterId=@Id)) AND
  (m.Unit<>@Unit OR m.MeterType<>@MeterType OR m.AssetKind<>@AssetKind OR m.NetworkRole<>@NetworkRole OR (m.FlowDirection<>@FlowDirection AND (m.FlowDirection<>0 OR m.ReplacesMeterId IS NOT NULL OR EXISTS(SELECT 1 FROM dbo.Meters linked WHERE linked.ReplacesMeterId=m.Id))) OR m.AreaId<>@AreaId OR m.StationId<>@StationId OR m.DepartmentId<>@DepartmentId OR ISNULL(m.InstalledOn,'17530101')<>ISNULL(@InstalledOn,'17530101') OR (m.SerialNumber<>@SerialNumber AND m.SerialNumber<>N'')))
 THROW 51107,'Recorded or linked meters retain their unit, serial, boundary, placement and service dates. Create a replacement.',1;
 IF @IsActive=1 AND EXISTS(SELECT 1 FROM dbo.Meters WHERE ReplacesMeterId=@Id) THROW 51108,'A replaced meter cannot be reactivated.',1;
 UPDATE dbo.Meters SET MeterName=@MeterName,MeterType=@MeterType,Unit=@Unit,AreaId=@AreaId,DepartmentId=@DepartmentId,StationId=@StationId,IsCumulative=@IsCumulative,IsActive=@IsActive,RetiredOn=CASE WHEN @IsActive=0 THEN COALESCE(RetiredOn,GETDATE()) ELSE RetiredOn END,AssetKind=@AssetKind,NetworkRole=@NetworkRole,CapacityLitres=@CapacityLitres,SerialNumber=@SerialNumber,FlowDirection=@FlowDirection,InstalledOn=@InstalledOn WHERE Id=@Id;
 COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;

GO
CREATE OR ALTER PROCEDURE dbo.spMeters_Delete @Id INT AS
BEGIN
 SET NOCOUNT ON;
 -- Retire the physical meter; reading and replacement history must survive.
 UPDATE dbo.Meters SET IsActive=0,RetiredOn=COALESCE(RetiredOn,GETDATE()) WHERE Id=@Id;
END;

GO
CREATE OR ALTER PROCEDURE dbo.spMeterReadings_All AS
BEGIN SET NOCOUNT ON; SELECT * FROM dbo.vwMeterReadingIntervals ORDER BY ReadingDate DESC,Id DESC; END;

GO
CREATE OR ALTER PROCEDURE dbo.spMeterReadings_GetByDateRange @StartDate DATE,@EndDate DATE AS
BEGIN SET NOCOUNT ON; SELECT * FROM dbo.vwMeterReadingIntervals WHERE ReadingDate>=@StartDate AND ReadingDate<DATEADD(DAY,1,@EndDate) ORDER BY ReadingDate DESC,Id DESC; END;

GO
CREATE OR ALTER PROCEDURE dbo.spMeterReadings_Insert
 @MeterId INT,@EmployeeId INT,@ReadingDate DATETIME2(0),@CurrentReading DECIMAL(19,6),@Notes NVARCHAR(255),@Id INT OUTPUT AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
 BEGIN TRANSACTION;
DECLARE @Active BIT;
SELECT @Active=IsActive FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE Id=@MeterId;
IF @Active IS NULL OR @Active=0 THROW 51010,'Select an active meter.',1;
IF EXISTS(SELECT 1 FROM dbo.Meters WHERE Id=@MeterId AND ((InstalledOn IS NOT NULL AND @ReadingDate<InstalledOn) OR (RetiredOn IS NOT NULL AND @ReadingDate>RetiredOn))) THROW 51109,'Reading date is outside physical meter service.',1;
DECLARE @ExcludeId INT=NULL;
DECLARE @Before DECIMAL(19,6),@After DECIMAL(19,6);
SELECT TOP(1) @Before=CurrentReading FROM dbo.MeterReadings WHERE MeterId=@MeterId AND IsDeleted=0 AND (@ExcludeId IS NULL OR Id<>@ExcludeId) AND ReadingDate<@ReadingDate ORDER BY ReadingDate DESC,Id DESC;
SELECT TOP(1) @After=CurrentReading FROM dbo.MeterReadings WHERE MeterId=@MeterId AND IsDeleted=0 AND (@ExcludeId IS NULL OR Id<>@ExcludeId) AND ReadingDate>@ReadingDate ORDER BY ReadingDate,Id;
IF EXISTS(SELECT 1 FROM dbo.MeterReadings WHERE MeterId=@MeterId AND IsDeleted=0 AND (@ExcludeId IS NULL OR Id<>@ExcludeId) AND ReadingDate=@ReadingDate)
 THROW 51011,'A reading already exists for this meter at that time. Edit that capture instead.',1;
IF @ReadingDate>GETDATE() OR @CurrentReading<0 OR (@Before IS NOT NULL AND @CurrentReading<@Before) OR (@After IS NOT NULL AND @CurrentReading>@After)
 THROW 51012,'The counter must be between the readings immediately before and after this capture. Record replacements/resets as a new meter.',1;
INSERT dbo.MeterReadings(MeterId,EmployeeId,ReadingDate,CurrentReading,PreviousReading,Notes)
VALUES(@MeterId,@EmployeeId,@ReadingDate,@CurrentReading,COALESCE(@Before,@CurrentReading),@Notes);
SET @Id=CONVERT(INT,SCOPE_IDENTITY());
;WITH Chain AS (
 SELECT Id,CurrentReading,LAG(CurrentReading) OVER(ORDER BY ReadingDate,Id) AS PreviousCounter
 FROM dbo.MeterReadings WHERE MeterId=@MeterId AND IsDeleted=0
)
UPDATE mr SET PreviousReading=COALESCE(c.PreviousCounter,c.CurrentReading)
FROM dbo.MeterReadings mr JOIN Chain c ON c.Id=mr.Id;
COMMIT TRANSACTION;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK TRANSACTION; THROW; END CATCH
END;

GO
CREATE OR ALTER PROCEDURE dbo.spMeterReadings_Update
 @Id INT,@ReadingDate DATETIME,@CurrentReading DECIMAL(19,6),@Notes NVARCHAR(255),@UpdatedAt DATETIME,@UpdatedBy INT AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 DECLARE @MeterId INT; SELECT @MeterId=MeterId FROM dbo.MeterReadings WHERE Id=@Id AND IsDeleted=0;
 IF @MeterId IS NULL THROW 51013,'Reading not found.',1;
 BEGIN TRY BEGIN TRANSACTION;
DECLARE @Active BIT;
SELECT @Active=IsActive FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE Id=@MeterId;
IF @Active IS NULL OR @Active=0 THROW 51010,'Select an active meter.',1;
IF EXISTS(SELECT 1 FROM dbo.Meters WHERE Id=@MeterId AND ((InstalledOn IS NOT NULL AND @ReadingDate<InstalledOn) OR (RetiredOn IS NOT NULL AND @ReadingDate>RetiredOn))) THROW 51109,'Reading date is outside physical meter service.',1;
DECLARE @ExcludeId INT=@Id;
DECLARE @Before DECIMAL(19,6),@After DECIMAL(19,6);
SELECT TOP(1) @Before=CurrentReading FROM dbo.MeterReadings WHERE MeterId=@MeterId AND IsDeleted=0 AND (@ExcludeId IS NULL OR Id<>@ExcludeId) AND ReadingDate<@ReadingDate ORDER BY ReadingDate DESC,Id DESC;
SELECT TOP(1) @After=CurrentReading FROM dbo.MeterReadings WHERE MeterId=@MeterId AND IsDeleted=0 AND (@ExcludeId IS NULL OR Id<>@ExcludeId) AND ReadingDate>@ReadingDate ORDER BY ReadingDate,Id;
IF EXISTS(SELECT 1 FROM dbo.MeterReadings WHERE MeterId=@MeterId AND IsDeleted=0 AND (@ExcludeId IS NULL OR Id<>@ExcludeId) AND ReadingDate=@ReadingDate)
 THROW 51011,'A reading already exists for this meter at that time. Edit that capture instead.',1;
IF @ReadingDate>GETDATE() OR @CurrentReading<0 OR (@Before IS NOT NULL AND @CurrentReading<@Before) OR (@After IS NOT NULL AND @CurrentReading>@After)
 THROW 51012,'The counter must be between the readings immediately before and after this capture. Record replacements/resets as a new meter.',1;
UPDATE dbo.MeterReadings SET ReadingDate=@ReadingDate,CurrentReading=@CurrentReading,Notes=@Notes,UpdatedAt=@UpdatedAt,UpdatedBy=@UpdatedBy WHERE Id=@Id AND IsDeleted=0;
IF @@ROWCOUNT=0 THROW 51013,'Reading not found.',1;
;WITH Chain AS (
 SELECT Id,CurrentReading,LAG(CurrentReading) OVER(ORDER BY ReadingDate,Id) AS PreviousCounter
 FROM dbo.MeterReadings WHERE MeterId=@MeterId AND IsDeleted=0
)
UPDATE mr SET PreviousReading=COALESCE(c.PreviousCounter,c.CurrentReading)
FROM dbo.MeterReadings mr JOIN Chain c ON c.Id=mr.Id;
COMMIT TRANSACTION;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK TRANSACTION; THROW; END CATCH
END;

GO
CREATE OR ALTER PROCEDURE dbo.spMeterReadings_Import
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

GO
COMMIT TRANSACTION;
