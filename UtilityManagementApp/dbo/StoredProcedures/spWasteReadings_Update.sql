CREATE PROCEDURE dbo.spWasteReadings_Update @Id INT,@WasteAmount DECIMAL(18,2),@Notes NVARCHAR(500),@UpdatedAt DATETIME2,@UpdatedBy INT,@ReadingDate DATETIME2=NULL AS
BEGIN SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @WasteAmount IS NULL OR @WasteAmount<=0 THROW 51200,'Waste amount must be positive.',1;
 IF @ReadingDate IS NOT NULL AND CONVERT(DATE,@ReadingDate)>CONVERT(DATE,GETDATE()) THROW 51201,'Choose a collection date no later than today.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.Employees WHERE Id=@UpdatedBy AND IsActive=1) THROW 51203,'An active employee link is required.',1;
 UPDATE dbo.WasteReading SET WasteAmount=@WasteAmount,Notes=@Notes,ReadingDate=COALESCE(@ReadingDate,ReadingDate),UpdatedAt=@UpdatedAt,UpdatedBy=@UpdatedBy WHERE Id=@Id AND IsDeleted=0;
 IF @@ROWCOUNT=0 THROW 51204,'Waste collection not found. Reload the page.',1;
END;
