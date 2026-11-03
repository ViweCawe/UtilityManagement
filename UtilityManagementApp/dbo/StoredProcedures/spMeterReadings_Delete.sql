CREATE PROCEDURE dbo.spMeterReadings_Delete @Id INT AS
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
