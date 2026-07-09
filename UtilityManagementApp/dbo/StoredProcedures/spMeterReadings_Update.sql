CREATE PROCEDURE [dbo].[spMeterReadings_Update]
(
	@Id INT,
	@ReadingDate DATETIME2,
	@CurrentReading FLOAT,
	@Notes NVARCHAR(255),
	@UpdatedAt DATETIME2,
	@UpdatedBy NVARCHAR(50)

)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @PreviousReading decimal(18,1);

	DECLARE @MeterId INT;

	SELECT @MeterId = MeterId
	FROM dbo.MeterReadings
	WHERE Id = @Id;

	SELECT TOP 1 
		@PreviousReading = CurrentReading
	FROM dbo.MeterReadings
	WHERE MeterId = @MeterId
	AND Id <> @Id
	AND ReadingDate < @ReadingDate
	ORDER BY ReadingDate DESC;

	SET @PreviousReading = ISNULL(@PreviousReading, 0);

	DECLARE @Usage DECIMAL;

	SET @Usage = @CurrentReading - @PreviousReading;

	UPDATE dbo.MeterReadings
		SET 
			ReadingDate = @ReadingDate,
			CurrentReading = @CurrentReading,
			Notes = @Notes,
			UpdatedAt = @UpdatedAt,
			UpdatedBy = @UpdatedBy
	WHERE Id = @Id



END