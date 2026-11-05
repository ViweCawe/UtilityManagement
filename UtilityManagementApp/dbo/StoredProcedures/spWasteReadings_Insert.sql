CREATE PROCEDURE dbo.spWasteReadings_Insert @WasteTypeId INT,@CreatedBy INT,@WasteAmount DECIMAL(18,2),@RecordedAt DATETIME2,@Notes NVARCHAR(500),@Id INT OUTPUT AS
BEGIN SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @WasteAmount IS NULL OR @WasteAmount<=0 THROW 51200,'Waste amount must be positive.',1;
 IF @RecordedAt IS NULL OR CONVERT(DATE,@RecordedAt)>CONVERT(DATE,GETDATE()) THROW 51201,'Choose a valid collection date no later than today.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.WasteType WHERE Id=@WasteTypeId AND IsActive=1) THROW 51202,'Choose an active waste type.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.Employees WHERE Id=@CreatedBy AND IsActive=1) THROW 51203,'An active employee link is required.',1;
 INSERT dbo.WasteReading(WasteTypeId,CapturedBy,WasteAmount,ReadingDate,Notes) VALUES(@WasteTypeId,@CreatedBy,@WasteAmount,@RecordedAt,@Notes);
 SET @Id=CONVERT(INT,SCOPE_IDENTITY());
END;
