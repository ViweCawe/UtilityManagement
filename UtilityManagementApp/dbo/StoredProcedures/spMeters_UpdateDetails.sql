CREATE PROCEDURE dbo.spMeters_UpdateDetails
    @Id INT, @MeterType INT, @MeterName NVARCHAR(100), @Unit NVARCHAR(20),
    @AreaId INT, @DepartmentId INT, @StationId INT, @IsCumulative BIT, @IsActive BIT,
    @AssetKind INT = 1, @NetworkRole INT = 4, @CapacityLitres DECIMAL(14,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.Meters WHERE Id = @Id AND AssetKind <> @AssetKind)
       AND EXISTS (SELECT 1 FROM dbo.MeterReadings WHERE MeterId = @Id)
       THROW 51001, 'An asset with meter readings cannot change between Meter and Tank.', 1;
    UPDATE dbo.Meters SET MeterName=@MeterName, MeterType=@MeterType, Unit=@Unit,
        AreaId=@AreaId, DepartmentId=@DepartmentId, StationId=@StationId,
        IsCumulative=@IsCumulative, IsActive=@IsActive, AssetKind=@AssetKind,
        NetworkRole=@NetworkRole, CapacityLitres=@CapacityLitres WHERE Id=@Id;
END
