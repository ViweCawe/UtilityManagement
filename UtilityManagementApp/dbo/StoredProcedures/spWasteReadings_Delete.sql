CREATE PROCEDURE dbo.spWasteReadings_Delete @Id INT,@UpdatedBy INT=NULL,@UpdatedAt DATETIME2=NULL AS
BEGIN SET NOCOUNT ON;
 IF @UpdatedBy IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Employees WHERE Id=@UpdatedBy AND IsActive=1) THROW 51203,'An active employee link is required.',1;
 UPDATE dbo.WasteReading SET IsDeleted=1,UpdatedAt=COALESCE(@UpdatedAt,SYSUTCDATETIME()),UpdatedBy=COALESCE(CONVERT(NVARCHAR(100),@UpdatedBy),UpdatedBy) WHERE Id=@Id AND IsDeleted=0;
 IF @@ROWCOUNT=0 THROW 51204,'Waste collection not found. Reload the page.',1;
END;
