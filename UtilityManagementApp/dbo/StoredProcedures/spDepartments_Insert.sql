CREATE PROCEDURE [dbo].[spDepartments_Insert]
	(
	@DepartmentName NVARCHAR(100),
	@Id INT OUTPUT
	)
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.Departments
	(
	DepartmentName
	)
	VALUES
	(
	@DepartmentName
	);
	SET @Id = SCOPE_IDENTITY();


END
