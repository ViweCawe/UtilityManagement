CREATE PROCEDURE [dbo].[spMeterReadings_Insert]
(
	@MeterId INT,
	@EmployeeId INT,
	@ReadingDate DATETIME,
	@CurrentReading DECIMAL (18,2),
	@Notes NVARCHAR(500),
	@Id INT OUTPUT
)

AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @PreviousReading DECIMAL (18 ,1);

	SELECT TOP 1 @PreviousReading = CurrentReading
	FROM dbo.MeterReadings
	WHERE MeterId = @MeterId

	 AND ReadingDate < @ReadingDate
	ORDER BY ReadingDate DESC;

	SET @PreviousReading = ISNULL(@PreviousReading, 0);
	SET @ReadingDate = DATEADD(MINUTE, DATEDIFF(MINUTE, 0, @ReadingDate), 0);


	INSERT INTO dbo.MeterReadings
	(
		MeterId,
		EmployeeId,
		ReadingDate,
		CurrentReading,
		PreviousReading,
		Notes
	)
	VALUES
	(
		@MeterId,
		@EmployeeId,
		@ReadingDate,
		@CurrentReading,
		@PreviousReading,
		@Notes
	);
	SET		@Id = CAST(SCOPE_IDENTITY() AS INT);
	
END
GO