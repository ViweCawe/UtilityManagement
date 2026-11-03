CREATE PROCEDURE dbo.spAdminArea_Save @Id INT=NULL,@AreaName NVARCHAR(100),@StationId INT,@IsActive BIT AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 SET @AreaName=LTRIM(RTRIM(@AreaName));
 BEGIN TRY BEGIN TRANSACTION;
 IF @AreaName IS NULL OR LEN(@AreaName)=0 THROW 51100,'Enter an area name.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.Stations WITH(UPDLOCK,HOLDLOCK) WHERE Id=@StationId AND IsActive=1) THROW 51101,'Select an active location.',1;
 IF EXISTS(SELECT 1 FROM dbo.Area WITH(UPDLOCK,HOLDLOCK) WHERE StationId=@StationId AND AreaName=@AreaName AND (@Id IS NULL OR Id<>@Id)) THROW 51102,'That area already exists at this location.',1;
 IF @Id IS NULL INSERT dbo.Area(AreaName,StationId,IsActive) VALUES(@AreaName,@StationId,@IsActive);
 ELSE BEGIN
   IF NOT EXISTS(SELECT 1 FROM dbo.Area WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id) THROW 51103,'Area not found. Reload the page.',1;
   IF EXISTS(SELECT 1 FROM dbo.Area WHERE Id=@Id AND StationId<>@StationId) AND EXISTS(SELECT 1 FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE AreaId=@Id) THROW 51111,'Move the assigned meters before changing this area location.',1;
   IF @IsActive=0 AND EXISTS(SELECT 1 FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE AreaId=@Id AND IsActive=1) THROW 51104,'Move or deactivate active meters before deactivating this area.',1;
   UPDATE dbo.Area SET AreaName=@AreaName,StationId=@StationId,IsActive=@IsActive WHERE Id=@Id;
 END;
 COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
