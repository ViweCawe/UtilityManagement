CREATE PROCEDURE [dbo].[spEmployees_Insert]
	@UserId NVARCHAR(255),
	@Email NVARCHAR(255),
	@IsActive BIT = 1
AS
BEGIN
		INSERT INTO dbo.Employees
		(
			UserId,
			Email,
			IsActive
		)
		VALUES
		(
			@UserId,
			@Email,
			@IsActive
		);
END