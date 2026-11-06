CREATE PROCEDURE dbo.spMeters_UpdateDetails
 @Id INT,@MeterType INT,@MeterName NVARCHAR(100),@Unit NVARCHAR(20),@AreaId INT,@DepartmentId INT,@StationId INT,@IsCumulative BIT,@IsActive BIT,
 @AssetKind INT=1,@NetworkRole INT=4,@CapacityLitres DECIMAL(14,2)=NULL,@SerialNumber NVARCHAR(100)='',@FlowDirection INT=0,@InstalledOn DATETIME2(0)=NULL
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY BEGIN TRANSACTION;
 DECLARE @lock INT; SELECT @lock=Id FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id AND IsDeleted=0;
 IF @lock IS NULL THROW 51106,'Meter not found.',1;
 IF NOT ((@MeterType=1 AND @Unit IN(N'L',N'kL',N'm³')) OR (@MeterType=2 AND @Unit=N'kWh')) THROW 51100,'Invalid counter unit.',1;
 IF @InstalledOn>GETDATE() THROW 51101,'Installation cannot be in the future.',1;
 IF EXISTS(SELECT 1 FROM dbo.Meters m WHERE m.Id=@Id AND (EXISTS(SELECT 1 FROM dbo.MeterReadings WHERE MeterId=@Id) OR m.ReplacesMeterId IS NOT NULL OR EXISTS(SELECT 1 FROM dbo.Meters n WHERE n.ReplacesMeterId=@Id)) AND
  (m.Unit<>@Unit OR m.MeterType<>@MeterType OR m.AssetKind<>@AssetKind OR m.NetworkRole<>@NetworkRole OR (m.FlowDirection<>@FlowDirection AND (m.FlowDirection<>0 OR m.ReplacesMeterId IS NOT NULL OR EXISTS(SELECT 1 FROM dbo.Meters linked WHERE linked.ReplacesMeterId=m.Id))) OR m.AreaId<>@AreaId OR m.StationId<>@StationId OR m.DepartmentId<>@DepartmentId OR ISNULL(m.InstalledOn,'17530101')<>ISNULL(@InstalledOn,'17530101') OR (m.SerialNumber<>@SerialNumber AND m.SerialNumber<>N'')))
 THROW 51107,'Recorded or linked meters retain their unit, serial, boundary, placement and service dates. Create a replacement.',1;
 IF @IsActive=1 AND EXISTS(SELECT 1 FROM dbo.Meters WHERE ReplacesMeterId=@Id) THROW 51108,'A replaced meter cannot be reactivated.',1;
 UPDATE dbo.Meters SET MeterName=@MeterName,MeterType=@MeterType,Unit=@Unit,AreaId=@AreaId,DepartmentId=@DepartmentId,StationId=@StationId,IsCumulative=@IsCumulative,IsActive=@IsActive,RetiredOn=CASE WHEN @IsActive=0 THEN COALESCE(RetiredOn,GETDATE()) ELSE RetiredOn END,AssetKind=@AssetKind,NetworkRole=@NetworkRole,CapacityLitres=@CapacityLitres,SerialNumber=@SerialNumber,FlowDirection=@FlowDirection,InstalledOn=@InstalledOn WHERE Id=@Id;
 COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
