CREATE PROCEDURE [dbo].[spMeters_Insert]
	(
	@MeterName NVARCHAR(100),
	@MeterType INT,
	@Unit NVARCHAR(20),
	@AreaId INT,
	@DepartmentId INT,
	@StationId	INT,
	@IsCumulative BIT,
	@Id INT OUTPUT
	)
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.Meters
	(
		MeterName,
		MeterType,
		Unit,
		AreaId,
		DepartmentId,
		StationId,
		IsCumulative

	)
	VALUES
	(
		@MeterName,
		@MeterType,
		@Unit,
		@AreaId,
		@DepartmentId,
		@StationId,
		@IsCumulative
	);

		SET @Id = SCOPE_IDENTITY();
	

	
END