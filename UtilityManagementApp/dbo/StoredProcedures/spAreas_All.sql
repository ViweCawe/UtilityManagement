CREATE PROCEDURE [dbo].[spAreas_All]

AS
BEGIN
	SET NOCOUNT ON;
	SELECT 
	[a].[Id],
	[a].[AreaName],
	[a].[CreatedAt] 

	FROM dbo.Area [a]
	ORDER BY AreaName;

END
