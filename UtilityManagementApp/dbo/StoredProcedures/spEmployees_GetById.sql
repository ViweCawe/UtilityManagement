CREATE PROCEDURE [dbo].[spEmployees_GetById]
(
	@Id INT
)
AS
BEGIN
	SET NOCOUNT ON;
	SELECT 
		[Id],
		[IsActive]
	FROM dbo.Employees
	WHERE [Id] = @Id;
END