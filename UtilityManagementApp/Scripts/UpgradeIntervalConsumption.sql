-- Apply after the meter/network migrations, before deploying consumption calculations.
-- Baselines and interval usage are derived from non-deleted counter captures. No captures are deleted.
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
 r.Notes,r.IsDeleted,r.UpdatedAt,r.UpdatedBy,m.IsActive,m.MeterName,m.MeterType,m.Unit,
 a.AreaName,s.StationName,d.DepartmentName,e.Email AS EmployeeEmail,u.Email AS UpdatedByEmployeeEmail
FROM Captures r JOIN dbo.Meters m ON m.Id=r.MeterId
LEFT JOIN dbo.Area a ON a.Id=m.AreaId
LEFT JOIN dbo.Stations s ON s.Id=m.StationId
LEFT JOIN dbo.Departments d ON d.Id=m.DepartmentId
LEFT JOIN dbo.Employees e ON e.Id=r.EmployeeId
LEFT JOIN dbo.Employees u ON u.Id=r.UpdatedBy;
GO
CREATE OR ALTER PROCEDURE dbo.spMeterReading_GetLatestByMeterId @MeterId INT AS
BEGIN SET NOCOUNT ON; SELECT TOP(1) * FROM dbo.vwMeterReadingIntervals WHERE MeterId=@MeterId ORDER BY ReadingDate DESC,Id DESC; END;
GO
CREATE OR ALTER PROCEDURE dbo.spMeterReadings_All AS
BEGIN SET NOCOUNT ON; SELECT * FROM dbo.vwMeterReadingIntervals WHERE IsActive=1 ORDER BY ReadingDate DESC,Id DESC; END;
GO
CREATE OR ALTER PROCEDURE dbo.spMeterReadings_Delete @Id INT AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 DECLARE @MeterId INT; SELECT @MeterId=MeterId FROM dbo.MeterReadings WHERE Id=@Id AND IsDeleted=0;
 IF @MeterId IS NULL RETURN;
 BEGIN TRY BEGIN TRANSACTION;
 DECLARE @LockId INT; SELECT @LockId=Id FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE Id=@MeterId;
 UPDATE dbo.MeterReadings SET IsDeleted=1 WHERE Id=@Id;
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
CREATE OR ALTER PROCEDURE dbo.spMeterReadings_GetByDateRange @StartDate DATE,@EndDate DATE AS
BEGIN SET NOCOUNT ON; SELECT * FROM dbo.vwMeterReadingIntervals WHERE IsActive=1 AND ReadingDate>=@StartDate AND ReadingDate<DATEADD(DAY,1,@EndDate) ORDER BY ReadingDate DESC,Id DESC; END;
GO
CREATE OR ALTER PROCEDURE dbo.spMeterReadings_GetById @Id INT AS
BEGIN SET NOCOUNT ON; SELECT * FROM dbo.vwMeterReadingIntervals WHERE Id=@Id; END;
GO
CREATE OR ALTER PROCEDURE dbo.spMeterReadings_GetByMeterId @MeterId INT AS
BEGIN SET NOCOUNT ON; SELECT * FROM dbo.vwMeterReadingIntervals WHERE MeterId=@MeterId ORDER BY ReadingDate DESC,Id DESC; END;
GO
CREATE OR ALTER PROCEDURE dbo.spMeterReading_GetLatestAll AS
BEGIN SET NOCOUNT ON;
WITH Latest AS (SELECT *, ROW_NUMBER() OVER(PARTITION BY MeterId ORDER BY ReadingDate DESC,Id DESC) AS RN FROM dbo.vwMeterReadingIntervals WHERE IsActive=1)
SELECT * FROM Latest WHERE RN=1 ORDER BY MeterId; END;
GO
CREATE OR ALTER PROCEDURE dbo.spMeterReadings_Insert
 @MeterId INT,@EmployeeId INT,@ReadingDate DATETIME2(0),@CurrentReading INT,@Notes NVARCHAR(255),@Id INT OUTPUT AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
 BEGIN TRANSACTION;
DECLARE @Active BIT;
SELECT @Active=IsActive FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE Id=@MeterId;
IF @Active IS NULL OR @Active=0 THROW 51010,'Select an active meter.',1;
DECLARE @ExcludeId INT=NULL;
DECLARE @Before INT,@After INT;
SELECT TOP(1) @Before=CurrentReading FROM dbo.MeterReadings WHERE MeterId=@MeterId AND IsDeleted=0 AND (@ExcludeId IS NULL OR Id<>@ExcludeId) AND ReadingDate<@ReadingDate ORDER BY ReadingDate DESC,Id DESC;
SELECT TOP(1) @After=CurrentReading FROM dbo.MeterReadings WHERE MeterId=@MeterId AND IsDeleted=0 AND (@ExcludeId IS NULL OR Id<>@ExcludeId) AND ReadingDate>@ReadingDate ORDER BY ReadingDate,Id;
IF EXISTS(SELECT 1 FROM dbo.MeterReadings WHERE MeterId=@MeterId AND IsDeleted=0 AND (@ExcludeId IS NULL OR Id<>@ExcludeId) AND ReadingDate=@ReadingDate)
 THROW 51011,'A reading already exists for this meter at that time. Edit that capture instead.',1;
IF @CurrentReading<0 OR (@Before IS NOT NULL AND @CurrentReading<@Before) OR (@After IS NOT NULL AND @CurrentReading>@After)
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
 @Id INT,@ReadingDate DATETIME,@CurrentReading INT,@Notes NVARCHAR(255),@UpdatedAt DATETIME,@UpdatedBy INT AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 DECLARE @MeterId INT; SELECT @MeterId=MeterId FROM dbo.MeterReadings WHERE Id=@Id AND IsDeleted=0;
 IF @MeterId IS NULL THROW 51013,'Reading not found.',1;
 BEGIN TRY BEGIN TRANSACTION;
DECLARE @Active BIT;
SELECT @Active=IsActive FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE Id=@MeterId;
IF @Active IS NULL OR @Active=0 THROW 51010,'Select an active meter.',1;
DECLARE @ExcludeId INT=@Id;
DECLARE @Before INT,@After INT;
SELECT TOP(1) @Before=CurrentReading FROM dbo.MeterReadings WHERE MeterId=@MeterId AND IsDeleted=0 AND (@ExcludeId IS NULL OR Id<>@ExcludeId) AND ReadingDate<@ReadingDate ORDER BY ReadingDate DESC,Id DESC;
SELECT TOP(1) @After=CurrentReading FROM dbo.MeterReadings WHERE MeterId=@MeterId AND IsDeleted=0 AND (@ExcludeId IS NULL OR Id<>@ExcludeId) AND ReadingDate>@ReadingDate ORDER BY ReadingDate,Id;
IF EXISTS(SELECT 1 FROM dbo.MeterReadings WHERE MeterId=@MeterId AND IsDeleted=0 AND (@ExcludeId IS NULL OR Id<>@ExcludeId) AND ReadingDate=@ReadingDate)
 THROW 51011,'A reading already exists for this meter at that time. Edit that capture instead.',1;
IF @CurrentReading<0 OR (@Before IS NOT NULL AND @CurrentReading<@Before) OR (@After IS NOT NULL AND @CurrentReading>@After)
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
