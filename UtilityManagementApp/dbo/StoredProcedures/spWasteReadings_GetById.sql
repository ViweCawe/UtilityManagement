CREATE PROCEDURE dbo.spWasteReadings_GetById @Id INT AS
BEGIN SET NOCOUNT ON;
 SELECT wr.Id,wr.WasteTypeId,wr.WasteAmount,wr.ReadingDate,wr.CapturedBy AS CreatedBy,wr.Notes,wr.IsDeleted,
 wr.UpdatedAt,TRY_CONVERT(INT,wr.UpdatedBy) AS UpdatedBy,COALESCE(e.Email,'Unknown employee') AS EmployeeEmail,
 COALESCE(wt.Name,'Unknown waste type') AS WasteTypeName,COALESCE(wc.Name,'Uncategorised') AS CategoryName,COALESCE(wm.Name,'Unknown material') AS MaterialName
 FROM dbo.WasteReading wr LEFT JOIN dbo.WasteType wt ON wt.Id=wr.WasteTypeId
 LEFT JOIN dbo.WasteCategory wc ON wc.Id=wt.CategoryId LEFT JOIN dbo.WasteMaterial wm ON wm.Id=wt.WasteMaterialId
 LEFT JOIN dbo.Employees e ON e.Id=wr.CapturedBy WHERE wr.Id=@Id AND wr.IsDeleted=0;
END;
