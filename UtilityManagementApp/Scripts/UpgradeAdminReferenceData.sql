-- Apply before deploying admin area/location management. No existing records are removed.
CREATE OR ALTER PROCEDURE dbo.spAdminAreas_All AS
BEGIN SET NOCOUNT ON;
 SELECT a.Id,a.AreaName AS Name,a.StationId,s.StationName,a.IsActive FROM dbo.Area a LEFT JOIN dbo.Stations s ON s.Id=a.StationId ORDER BY a.AreaName;
END;
GO
CREATE OR ALTER PROCEDURE dbo.spAdminLocations_All AS
BEGIN SET NOCOUNT ON; SELECT Id,StationName AS Name,IsActive FROM dbo.Stations ORDER BY StationName; END;
GO
CREATE OR ALTER PROCEDURE dbo.spAdminArea_Save @Id INT=NULL,@AreaName NVARCHAR(100),@StationId INT,@IsActive BIT AS
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
GO
CREATE OR ALTER PROCEDURE dbo.spAdminLocation_Save @Id INT=NULL,@StationName NVARCHAR(100),@IsActive BIT AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 SET @StationName=LTRIM(RTRIM(@StationName));
 BEGIN TRY BEGIN TRANSACTION;
 IF @StationName IS NULL OR LEN(@StationName)=0 THROW 51105,'Enter a location name.',1;
 IF EXISTS(SELECT 1 FROM dbo.Stations WITH(UPDLOCK,HOLDLOCK) WHERE StationName=@StationName AND (@Id IS NULL OR Id<>@Id)) THROW 51106,'That location already exists.',1;
 IF @Id IS NULL INSERT dbo.Stations(StationName,IsActive) VALUES(@StationName,@IsActive);
 ELSE BEGIN
   IF NOT EXISTS(SELECT 1 FROM dbo.Stations WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id) THROW 51107,'Location not found. Reload the page.',1;
   IF @IsActive=0 AND (EXISTS(SELECT 1 FROM dbo.Area WITH(UPDLOCK,HOLDLOCK) WHERE StationId=@Id AND IsActive=1) OR EXISTS(SELECT 1 FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE StationId=@Id AND IsActive=1)) THROW 51108,'Move or deactivate active areas and meters first.',1;
   UPDATE dbo.Stations SET StationName=@StationName,IsActive=@IsActive WHERE Id=@Id;
 END;
 COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.spAdminArea_Delete @Id INT AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY BEGIN TRANSACTION;
 DECLARE @Lock INT; SELECT @Lock=Id FROM dbo.Area WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id;
 IF EXISTS(SELECT 1 FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE AreaId=@Id) THROW 51109,'This area is used by meters. Keep it for history, or deactivate it after moving active meters.',1;
 DELETE dbo.Area WHERE Id=@Id;
 COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.spAdminLocation_Delete @Id INT AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY BEGIN TRANSACTION;
 DECLARE @Lock INT; SELECT @Lock=Id FROM dbo.Stations WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id;
 IF EXISTS(SELECT 1 FROM dbo.Area WITH(UPDLOCK,HOLDLOCK) WHERE StationId=@Id) OR EXISTS(SELECT 1 FROM dbo.Meters WITH(UPDLOCK,HOLDLOCK) WHERE StationId=@Id) THROW 51110,'This location is used by areas or meters. Keep it for history, or deactivate it when no longer active.',1;
 DELETE dbo.Stations WHERE Id=@Id;
 COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.spAreas_All AS
BEGIN SET NOCOUNT ON; SELECT Id,AreaName,StationId,IsActive,CreatedAt FROM dbo.Area WHERE IsActive=1 ORDER BY AreaName; END;
GO
CREATE OR ALTER PROCEDURE dbo.spStation_All AS
BEGIN SET NOCOUNT ON; SELECT Id,StationName,IsActive,CreatedAt FROM dbo.Stations WHERE IsActive=1 ORDER BY StationName; END;
GO
