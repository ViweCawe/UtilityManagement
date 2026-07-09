CREATE PROCEDURE [dbo].[spEmployees_Delete]
	@Id INT
AS
BEGIN
	SET NOCOUNT ON;
	DELETE FROM dbo.Employees
	WHERE Id = @Id;
END