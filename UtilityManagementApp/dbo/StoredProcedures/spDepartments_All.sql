CREATE PROCEDURE [dbo].[spDepartments_All]
as
BEGIN
	SELECT 
	[Id],
	[DepartmentName],
	[CreatedAt]
	FROM dbo.Departments
END