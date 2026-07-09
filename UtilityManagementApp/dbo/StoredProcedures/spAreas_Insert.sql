CREATE PROCEDURE [dbo].[spArea_Insert]
	(
	@AreaName NVARCHAR(100),
	@Id INT OUTPUT
	)
AS
BEGIN
	SET NOCOUNT ON;
		
		INSERT INTO dbo.Area
		(
			AreaName
		)
		VALUES
		(
			@AreaName
		);
		SET @Id = SCOPE_IDENTITY();


END
