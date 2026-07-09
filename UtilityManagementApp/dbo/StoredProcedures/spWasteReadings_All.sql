CREATE PROCEDURE [dbo].[spWasteReadings_All]

AS
BEGIN
    SET NOCOUNT ON;
SELECT[wr].[Id], 
      [wr].[WasteTypeId], 
      [wr].[WasteReading], 
      [wr].[ReadingDate],
      [wr].[CapturedBy],
      [wr].[Notes],
      [wr].[IsDeleted],
      [wc].[Name] AS WasteCategory, 
      [wm].[Name] AS WasteMaterial
   
    


   

FROM dbo.WasteReading wr
INNER JOIN dbo.WasteType wt
    ON wr.WasteTypeId = wt.Id

INNER JOIN dbo.WasteCategory wc
    ON wt.CategoryId = wc.Id

INNER JOIN dbo.WasteMaterial wm
    ON wt.WasteMaterialId = wm.Id

    WHERE [wt].IsActive = 1
	ORDER BY ReadingDate DESC;

END