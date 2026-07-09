CREATE PROCEDURE [dbo].[spDepartment_Update]
(
	@Id INT,
	@DepartmentName NVARCHAR(100),
	@IsActive BIT


)

AS
BEGIN
	SET NOCOUNT ON;

	UPDATE dbo.Departments
		SET 
			DepartmentName = @DepartmentName,
			IsActive = @IsActive
	WHERE Id = @Id

END
