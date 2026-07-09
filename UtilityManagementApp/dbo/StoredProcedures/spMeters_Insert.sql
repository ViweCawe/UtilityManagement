CREATE PROCEDURE [dbo].[spMeter_Insert]
	(
	@MeterName NVARCHAR(100),
	@MeterType NVARCHAR(50),
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