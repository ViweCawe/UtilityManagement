CREATE PROCEDURE [dbo].[spEmployees_Update]
AS
BEGIN
	SET NOCOUNT ON;
	UPDATE dbo.Employees
	SET IsActive = 0
	WHERE Id IN (SELECT Id FROM dbo.Employees WHERE IsActive = 1);
END