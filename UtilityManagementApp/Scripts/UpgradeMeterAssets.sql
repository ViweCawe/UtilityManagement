-- Run against the intended UtilityManagement database in SSMS BEFORE deploying the app.
-- No records are deleted. Existing assets default to Meter / End use; review these roles.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.Meters','AssetKind') IS NULL
    ALTER TABLE dbo.Meters ADD AssetKind INT NOT NULL CONSTRAINT DF_Meters_AssetKind DEFAULT 1 WITH VALUES;
IF COL_LENGTH('dbo.Meters','NetworkRole') IS NULL
    ALTER TABLE dbo.Meters ADD NetworkRole INT NOT NULL CONSTRAINT DF_Meters_NetworkRole DEFAULT 4 WITH VALUES;
IF COL_LENGTH('dbo.Meters','CapacityLitres') IS NULL
    ALTER TABLE dbo.Meters ADD CapacityLitres DECIMAL(14,2) NULL;
COMMIT TRANSACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name='CK_Meters_Asset' AND parent_object_id=OBJECT_ID('dbo.Meters'))
    ALTER TABLE dbo.Meters WITH CHECK ADD CONSTRAINT CK_Meters_Asset CHECK (
        (AssetKind=1 AND NetworkRole IN (1,2,3,4) AND CapacityLitres IS NULL)
        OR (AssetKind=2 AND MeterType=1 AND NetworkRole IN (3,5) AND IsCumulative=0 AND (CapacityLitres IS NULL OR CapacityLitres>0)));
GO
CREATE OR ALTER PROCEDURE [dbo].[spMeters_Insert]
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
GO
CREATE OR ALTER PROCEDURE [dbo].[spMeters_All]
AS
BEGIN 
		SET NOCOUNT ON;
		SELECT[m].[Id], [m].[AssetKind], [m].[NetworkRole], [m].[CapacityLitres],
        [m].[AreaId], [m].[DepartmentId], [m].[StationId], [m].[IsCumulative],
		[m].[MeterName],
		[m].[MeterType],
		[m].[Unit],
		[m].[IsActive],


		[a].[AreaName],
		[s].[StationName],
		[d].[DepartmentName]

		FROM dbo.Meters [m]
		LEFT JOIN dbo.Area [a]
		ON [m].AreaId = [a].Id
		LEFT JOIN dbo.Stations [s]
		ON [m].StationId = [s].Id
		LEFT JOIN dbo.Departments [d]
		ON [m].DepartmentId = [d].[Id]
			WHERE [m].IsActive = 1
			ORDER BY [m].MeterName;
			
END
GO
CREATE OR ALTER PROCEDURE [dbo].[spMeters_GetById]
    @Id INT
AS
BEGIN 
	SET NOCOUNT ON;
	SELECT 
		[m].[Id], [m].[AssetKind], [m].[NetworkRole], [m].[CapacityLitres],
        [m].[AreaId], [m].[DepartmentId], [m].[StationId], [m].[IsCumulative],
		[m].[MeterName],
		[m].[MeterType],
		[m].[Unit],
		[m].[IsActive],


		[a].[AreaName],
		[s].[StationName],
		[d].[DepartmentName]

		FROM dbo.Meters [m]
		LEFT JOIN dbo.Area [a]
		ON [m].AreaId = [a].Id
		LEFT JOIN dbo.Stations [s]
		ON [m].StationId = [s].Id
		LEFT JOIN dbo.Departments [d]
		ON [m].DepartmentId = [d].[Id]
		WHERE [m].Id = @Id
		AND [m].IsActive = 1;
END
GO
CREATE OR ALTER PROCEDURE dbo.spMeters_UpdateDetails
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

GO
