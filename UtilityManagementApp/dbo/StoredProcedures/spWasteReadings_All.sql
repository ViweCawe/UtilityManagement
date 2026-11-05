CREATE PROCEDURE dbo.spWasteReadings_All AS
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
