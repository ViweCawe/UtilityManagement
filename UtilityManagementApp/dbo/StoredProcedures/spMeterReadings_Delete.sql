CREATE PROCEDURE [dbo].[spMeterReadings_Delete]
(
	@Id INT
)
AS
BEGIN
	SET NOCOUNT ON;


	DELETE FROM dbo.MeterReadings
	WHERE Id = @Id;

	UPDATE dbo.MeterReadings
		SET IsDeleted = 1
		WHERE Id = @Id;
END