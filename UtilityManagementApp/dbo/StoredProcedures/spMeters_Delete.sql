CREATE PROCEDURE [dbo].[spMeters_Delete]
@Id INT
AS
BEGIN


SET NOCOUNT ON;
	DELETE FROM dbo.Meters
	WHERE Id = @Id;
	UPDATE dbo.Meters
		SET IsDeleted = 1
		WHERE Id = @Id;
END
