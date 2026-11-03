CREATE PROCEDURE dbo.spMeterReadings_Insert
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
