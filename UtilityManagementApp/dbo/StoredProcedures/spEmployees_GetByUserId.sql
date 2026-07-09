CREATE PROCEDURE [dbo].[spEmployees_GetByUserId]
	@UserId NVARCHAR(255)
AS
BEGIN
	SET NOCOUNT ON;
	SELECT 
		[Id],
		[IsActive]
		FROM dbo.Employees
		WHERE [UserId] = @UserId;
END