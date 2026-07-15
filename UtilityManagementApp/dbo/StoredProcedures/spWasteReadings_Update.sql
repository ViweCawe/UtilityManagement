CREATE PROCEDURE [dbo].[spWasteReadings_Update]
(
	
   @CaptureBy INT,
   @WasteAmount DECIMAL(18,2),
   @RecordedAt DATETIME ,
   @UpdatedAt DATETIME2,
   @UpdatedBy NVARCHAR(50),
   @Notes NVARCHAR (150),
   @Id INT OUTPUT

)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @WasteTypeId INT;

	SELECT @WasteTypeId = WasteTypeId 
	FROM WasteReading
	WHERE Id = @Id;

	UPDATE dbo.WasteReading

		SET
			UpdatedAt = @UpdatedAt,
			UpdatedBy = @UpdatedBy,
			WasteAmount = @WasteAmount,
			Notes = @Notes

			WHERE 
				Id = @Id

END