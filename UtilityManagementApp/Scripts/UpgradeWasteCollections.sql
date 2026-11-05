-- Apply before deploying the waste fixes. Existing collections are retained.
CREATE OR ALTER PROCEDURE dbo.spWasteReadings_All AS
BEGIN SET NOCOUNT ON;
 SELECT wr.Id,wr.WasteTypeId,wr.WasteAmount,wr.ReadingDate,wr.CapturedBy,wr.CapturedBy AS CreatedBy,
 wr.Notes,wr.IsDeleted,wr.UpdatedAt,TRY_CONVERT(INT,wr.UpdatedBy) AS UpdatedBy,
 COALESCE(e.Email,'Unknown employee') AS EmployeeEmail,
 COALESCE(wc.Name,'Uncategorised') AS WasteCategory,COALESCE(wc.Name,'Uncategorised') AS CategoryName,
 COALESCE(wm.Name,'Unknown material') AS WasteMaterial,COALESCE(wm.Name,'Unknown material') AS MaterialName,
 COALESCE(wt.Name,'Unknown waste type') AS WasteTypeName
 FROM dbo.WasteReading wr
 LEFT JOIN dbo.WasteType wt ON wt.Id=wr.WasteTypeId
 LEFT JOIN dbo.WasteCategory wc ON wc.Id=wt.CategoryId
 LEFT JOIN dbo.WasteMaterial wm ON wm.Id=wt.WasteMaterialId
 LEFT JOIN dbo.Employees e ON e.Id=wr.CapturedBy
 ORDER BY wr.ReadingDate DESC,wr.Id DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.spWasteReadings_GetById @Id INT AS
BEGIN SET NOCOUNT ON;
 SELECT wr.Id,wr.WasteTypeId,wr.WasteAmount,wr.ReadingDate,wr.CapturedBy AS CreatedBy,wr.Notes,wr.IsDeleted,
 wr.UpdatedAt,TRY_CONVERT(INT,wr.UpdatedBy) AS UpdatedBy,COALESCE(e.Email,'Unknown employee') AS EmployeeEmail,
 COALESCE(wt.Name,'Unknown waste type') AS WasteTypeName,COALESCE(wc.Name,'Uncategorised') AS CategoryName,COALESCE(wm.Name,'Unknown material') AS MaterialName
 FROM dbo.WasteReading wr LEFT JOIN dbo.WasteType wt ON wt.Id=wr.WasteTypeId
 LEFT JOIN dbo.WasteCategory wc ON wc.Id=wt.CategoryId LEFT JOIN dbo.WasteMaterial wm ON wm.Id=wt.WasteMaterialId
 LEFT JOIN dbo.Employees e ON e.Id=wr.CapturedBy WHERE wr.Id=@Id AND wr.IsDeleted=0;
END;
GO
CREATE OR ALTER PROCEDURE dbo.spWasteReadings_Insert @WasteTypeId INT,@CreatedBy INT,@WasteAmount DECIMAL(18,2),@RecordedAt DATETIME2,@Notes NVARCHAR(500),@Id INT OUTPUT AS
BEGIN SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @WasteAmount IS NULL OR @WasteAmount<=0 THROW 51200,'Waste amount must be positive.',1;
 IF @RecordedAt IS NULL OR CONVERT(DATE,@RecordedAt)>CONVERT(DATE,GETDATE()) THROW 51201,'Choose a valid collection date no later than today.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.WasteType WHERE Id=@WasteTypeId AND IsActive=1) THROW 51202,'Choose an active waste type.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.Employees WHERE Id=@CreatedBy AND IsActive=1) THROW 51203,'An active employee link is required.',1;
 INSERT dbo.WasteReading(WasteTypeId,CapturedBy,WasteAmount,ReadingDate,Notes) VALUES(@WasteTypeId,@CreatedBy,@WasteAmount,@RecordedAt,@Notes);
 SET @Id=CONVERT(INT,SCOPE_IDENTITY());
END;
GO
CREATE OR ALTER PROCEDURE dbo.spWasteReadings_Update @Id INT,@WasteAmount DECIMAL(18,2),@Notes NVARCHAR(500),@UpdatedAt DATETIME2,@UpdatedBy INT,@ReadingDate DATETIME2=NULL AS
BEGIN SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @WasteAmount IS NULL OR @WasteAmount<=0 THROW 51200,'Waste amount must be positive.',1;
 IF @ReadingDate IS NOT NULL AND CONVERT(DATE,@ReadingDate)>CONVERT(DATE,GETDATE()) THROW 51201,'Choose a collection date no later than today.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.Employees WHERE Id=@UpdatedBy AND IsActive=1) THROW 51203,'An active employee link is required.',1;
 UPDATE dbo.WasteReading SET WasteAmount=@WasteAmount,Notes=@Notes,ReadingDate=COALESCE(@ReadingDate,ReadingDate),UpdatedAt=@UpdatedAt,UpdatedBy=@UpdatedBy WHERE Id=@Id AND IsDeleted=0;
 IF @@ROWCOUNT=0 THROW 51204,'Waste collection not found. Reload the page.',1;
END;
GO
CREATE OR ALTER PROCEDURE dbo.spWasteReadings_Delete @Id INT,@UpdatedBy INT=NULL,@UpdatedAt DATETIME2=NULL AS
BEGIN SET NOCOUNT ON;
 IF @UpdatedBy IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Employees WHERE Id=@UpdatedBy AND IsActive=1) THROW 51203,'An active employee link is required.',1;
 UPDATE dbo.WasteReading SET IsDeleted=1,UpdatedAt=COALESCE(@UpdatedAt,SYSUTCDATETIME()),UpdatedBy=COALESCE(CONVERT(NVARCHAR(100),@UpdatedBy),UpdatedBy) WHERE Id=@Id AND IsDeleted=0;
 IF @@ROWCOUNT=0 THROW 51204,'Waste collection not found. Reload the page.',1;
END;
GO
