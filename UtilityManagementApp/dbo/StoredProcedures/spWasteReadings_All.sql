CREATE PROCEDURE [dbo].[spWasteReadings_All]

AS
BEGIN
    SET NOCOUNT ON;
SELECT[wr].[Id], 
      [wr].[WasteTypeId], 
      [wr].WasteAmount , 
      [wr].[ReadingDate],


      [e].[Email] AS EmployeeEmail,

      [e].Id as CapturedBy,
      [wr].[Notes],
      [wr].[IsDeleted],
      [wc].[Name] AS WasteCategory, 
      [wm].[Name] AS WasteMaterial,
      [wt].[Name] AS WasteTypeName



      
   
    


   

FROM dbo.WasteReading wr
INNER JOIN dbo.WasteType wt
    ON wr.WasteTypeId = wt.Id

INNER JOIN dbo.WasteCategory wc
    ON wt.CategoryId = wc.Id

INNER JOIN dbo.WasteMaterial wm
    ON wt.WasteMaterialId = wm.Id

    INNER JOIN dbo.Employees e
    ON wr.CapturedBy = e.Id

    WHERE [wt].IsActive = 1
	ORDER BY ReadingDate DESC;

END