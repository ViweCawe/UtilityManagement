CREATE PROCEDURE dbo.spMeters_Insert
 @MeterName NVARCHAR(100),@MeterType INT,@Unit NVARCHAR(20),@AreaId INT,@DepartmentId INT,@StationId INT,@IsCumulative BIT,@Id INT OUTPUT,
 @AssetKind INT=1,@NetworkRole INT=4,@CapacityLitres DECIMAL(14,2)=NULL,
 @SerialNumber NVARCHAR(100)='',@FlowDirection INT=0,@ReplacesMeterId INT=NULL,@InstalledOn DATETIME2(0)=NULL
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF NOT ((@MeterType=1 AND @Unit IN(N'L',N'kL',N'm³')) OR (@MeterType=2 AND @Unit=N'kWh')) THROW 51100,'Invalid counter unit. m² is not water volume.',1;
 IF @InstalledOn>GETDATE() THROW 51101,'Installation cannot be in the future.',1;
 BEGIN TRY BEGIN TRANSACTION;
 IF @ReplacesMeterId IS NOT NULL
 BEGIN
  DECLARE @Revision INT;
  SELECT @Revision=Revision FROM dbo.WaterNetworkDocument WITH(UPDLOCK,HOLDLOCK) WHERE DocumentId=1;
  IF @Revision IS NULL INSERT dbo.WaterNetworkDocument(DocumentId,NetworkJson,Revision) VALUES(1,NULL,0);
  IF @InstalledOn IS NULL THROW 51102,'A replacement needs a cutover time.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE Id=@ReplacesMeterId AND IsDeleted=0 AND MeterType=@MeterType AND AssetKind=@AssetKind AND NetworkRole=@NetworkRole AND AreaId=@AreaId AND StationId=@StationId AND DepartmentId=@DepartmentId AND FlowDirection=@FlowDirection AND (InstalledOn IS NULL OR InstalledOn<@InstalledOn)) THROW 51103,'Replacement must retain the same measurement boundary, utility and placement.',1;
  IF EXISTS(SELECT 1 FROM dbo.Meters WHERE ReplacesMeterId=@ReplacesMeterId) THROW 51104,'This meter already has a successor.',1;
  IF EXISTS(SELECT 1 FROM dbo.MeterReadings WHERE MeterId=@ReplacesMeterId AND IsDeleted=0 AND ReadingDate>@InstalledOn) THROW 51105,'Old meter has readings after the cutover.',1;
  UPDATE dbo.Meters SET IsActive=0,RetiredOn=@InstalledOn WHERE Id=@ReplacesMeterId;
 END;
 INSERT dbo.Meters(MeterName,MeterType,Unit,AreaId,DepartmentId,StationId,IsCumulative,AssetKind,NetworkRole,CapacityLitres,SerialNumber,FlowDirection,ReplacesMeterId,InstalledOn)
 VALUES(@MeterName,@MeterType,@Unit,@AreaId,@DepartmentId,@StationId,@IsCumulative,@AssetKind,@NetworkRole,@CapacityLitres,@SerialNumber,@FlowDirection,@ReplacesMeterId,@InstalledOn);
 SET @Id=CONVERT(INT,SCOPE_IDENTITY());
 IF @ReplacesMeterId IS NOT NULL BEGIN
  UPDATE dbo.WaterNetworkLinks SET MeterId=@Id WHERE MeterId=@ReplacesMeterId;
  UPDATE dbo.WaterNetworkDocument SET Revision=Revision+1,UpdatedAt=SYSUTCDATETIME() WHERE DocumentId=1;
 END;
 COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
