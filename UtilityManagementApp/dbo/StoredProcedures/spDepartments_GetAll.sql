CREATE PROCEDURE [dbo].[spDepartments_GetAll]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [Id],
        [DepartmentName],
        [IsActive]
    FROM [dbo].[Departments]
    WHERE [IsActive] = 1
    ORDER BY [DepartmentName];
END