CREATE PROCEDURE [dbo].[spWasteType_All]
AS
BEGIN
	SET NOCOUNT ON;

	SELECT [wt].[Id],
	[wt].[CategoryId],
	[wt].[WasteMaterialId],
	[wt].[IsActive], 
	[wt].[CreatedAt], 

	[wt].[Name] AS WasteTypeName, 
	[m].[Name] AS MaterialName, 
	[c].[Name] AS CategoryName

	FROM WasteType [wt]

	INNER JOIN dbo.WasteMaterial [m]
	ON [wt].WasteMaterialId = [m].[Id]

	INNER JOIN dbo.WasteCategory [c]
	ON [wt].CategoryId =[c].[Id]


END