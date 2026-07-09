CREATE PROCEDURE [dbo].[spEmployee_Update]
	@Id INT,
	@UserId NVARCHAR(255),
	@IsActive BIT
AS
BEGIN
	SET NOCOUNT ON;
	UPDATE dbo.Employees
	SET 
		UserId = @UserId,
		IsActive = @IsActive
	WHERE Id = @Id;
END