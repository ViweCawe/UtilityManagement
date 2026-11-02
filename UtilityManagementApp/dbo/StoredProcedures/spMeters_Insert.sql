CREATE PROCEDURE [dbo].[spMeters_Insert]
	(
	@MeterName NVARCHAR(100),
	@MeterType INT,
	@Unit NVARCHAR(20),
	@AreaId INT,
	@DepartmentId INT,
	@StationId	INT,
	@IsCumulative BIT,
	@Id INT OUTPUT,
    @AssetKind INT = 1, @NetworkRole INT = 4, @CapacityLitres DECIMAL(14,2) = NULL
	)
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.Meters
	(
		AssetKind, NetworkRole, CapacityLitres,
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
		@AssetKind, @NetworkRole, @CapacityLitres,
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