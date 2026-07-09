CREATE PROCEDURE [dbo].[spWasteReadings_Delete]
(
	@Id INT
)

AS 
BEGIN
	SET NOCOUNT ON;

	DELETE FROM WasteReading
	WHERE Id = @Id

	UPDATE WasteReading
	SET IsDeleted =	1
	WHERE Id =@Id
END