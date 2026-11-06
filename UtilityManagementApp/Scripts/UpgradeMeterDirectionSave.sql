-- Apply after UpgradeMeterLifecycle.sql. Replaces only the meter update procedure.
-- Run with sqlcmd -b (stop on error) before deploying the save-feedback fix.
IF COL_LENGTH('dbo.Meters','FlowDirection') IS NULL OR COL_LENGTH('dbo.Meters','ReplacesMeterId') IS NULL
 THROW 51111,'Apply UpgradeMeterLifecycle.sql before this direction correction.',1;
GO
CREATE OR ALTER PROCEDURE dbo.spMeters_UpdateDetails
 @Id INT,@MeterType INT,@MeterName NVARCHAR(100),@Unit NVARCHAR(20),@AreaId INT,@DepartmentId INT,@StationId INT,@IsCumulative BIT,@IsActive BIT,
 @AssetKind INT=1,@NetworkRole INT=4,@CapacityLitres DECIMAL(14,2)=NULL,@SerialNumber NVARCHAR(100)='',@FlowDirection INT=0,@InstalledOn DATETIME2(0)=NULL
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @FlowDirection NOT IN(0,1,2) THROW 51110,'Choose a valid meter direction.',1;
 BEGIN TRY BEGIN TRANSACTION;
 -- Follow the same lock order as network saves and replacements.
 DECLARE @Revision INT;
 SELECT @Revision=Revision FROM dbo.WaterNetworkDocument WITH(UPDLOCK,HOLDLOCK) WHERE DocumentId=1;
 IF @Revision IS NULL INSERT dbo.WaterNetworkDocument(DocumentId,NetworkJson,Revision) VALUES(1,NULL,0);
 DECLARE @OldDirection INT;
 DECLARE @lock INT; SELECT @OldDirection=FlowDirection,@lock=Id FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id AND IsDeleted=0;
 IF @lock IS NULL THROW 51106,'Meter not found.',1;
 IF NOT ((@MeterType=1 AND @Unit IN(N'L',N'kL',N'm³')) OR (@MeterType=2 AND @Unit=N'kWh')) THROW 51100,'Invalid counter unit.',1;
 IF @InstalledOn>GETDATE() THROW 51101,'Installation cannot be in the future.',1;
 IF EXISTS(SELECT 1 FROM dbo.Meters m WHERE m.Id=@Id AND (EXISTS(SELECT 1 FROM dbo.MeterReadings WHERE MeterId=@Id) OR m.ReplacesMeterId IS NOT NULL OR EXISTS(SELECT 1 FROM dbo.Meters n WHERE n.ReplacesMeterId=@Id)) AND
  (m.Unit<>@Unit OR m.MeterType<>@MeterType OR m.AssetKind<>@AssetKind OR m.NetworkRole<>@NetworkRole OR m.AreaId<>@AreaId OR m.StationId<>@StationId OR m.DepartmentId<>@DepartmentId OR ISNULL(m.InstalledOn,'17530101')<>ISNULL(@InstalledOn,'17530101') OR (m.SerialNumber<>@SerialNumber AND m.SerialNumber<>N'')))
 THROW 51107,'Recorded or linked meters retain their unit, serial, physical boundary, placement and service dates. Direction corrections are allowed; use a replacement for physical changes.',1;
 IF @IsActive=1 AND EXISTS(SELECT 1 FROM dbo.Meters WHERE ReplacesMeterId=@Id) THROW 51108,'A replaced meter cannot be reactivated.',1;
 UPDATE dbo.Meters SET MeterName=@MeterName,MeterType=@MeterType,Unit=@Unit,AreaId=@AreaId,DepartmentId=@DepartmentId,StationId=@StationId,IsCumulative=@IsCumulative,IsActive=@IsActive,RetiredOn=CASE WHEN @IsActive=0 THEN COALESCE(RetiredOn,GETDATE()) ELSE RetiredOn END,AssetKind=@AssetKind,NetworkRole=@NetworkRole,CapacityLitres=@CapacityLitres,SerialNumber=@SerialNumber,FlowDirection=@FlowDirection,InstalledOn=@InstalledOn WHERE Id=@Id;
 IF @OldDirection<>@FlowDirection
 BEGIN
  -- Direction is a correction of the measurement-point classification. Keep its
  -- predecessor/successor chain consistent without changing counter values.
  DECLARE @Root INT;
  ;WITH Parents AS (
   SELECT Id,ReplacesMeterId FROM dbo.Meters WHERE Id=@Id
   UNION ALL SELECT m.Id,m.ReplacesMeterId FROM dbo.Meters m JOIN Parents p ON m.Id=p.ReplacesMeterId
  ) SELECT @Root=Id FROM Parents WHERE ReplacesMeterId IS NULL OPTION(MAXRECURSION 500);
  ;WITH Family AS (
   SELECT Id FROM dbo.Meters WHERE Id=@Root
   UNION ALL SELECT m.Id FROM dbo.Meters m JOIN Family f ON m.ReplacesMeterId=f.Id
  ) SELECT Id INTO #DirectionFamily FROM Family OPTION(MAXRECURSION 500);
  UPDATE m SET FlowDirection=@FlowDirection FROM dbo.Meters m JOIN #DirectionFamily f ON f.Id=m.Id;
  UPDATE dbo.WaterNetworkDocument SET Revision=Revision+1,UpdatedAt=SYSUTCDATETIME() WHERE DocumentId=1;
 END;
 COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;

GO
