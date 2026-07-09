CREATE PROCEDURE [dbo].[spEmployees_All]
AS
BEGIN
	SET NOCOUNT ON;

	SELECT [e].[Id],
		   [e].[Email],
		   [e].[IsActive]
		   FROM dbo.Employees [e]
END