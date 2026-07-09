CREATE PROCEDURE [dbo].[spStation_All]
AS
BEGIN
	SET NOCOUNT ON;

	SELECT [Id], [StationName], [CreatedAt], [IsActive] FROM [dbo].[Stations]

END