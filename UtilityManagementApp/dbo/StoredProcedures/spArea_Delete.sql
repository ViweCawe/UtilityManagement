CREATE PROCEDURE [dbo].[spArea_Delete]
	@Id INT,
	@IsActive BIT
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM dbo.Area
	WHERE Id = @Id;

END