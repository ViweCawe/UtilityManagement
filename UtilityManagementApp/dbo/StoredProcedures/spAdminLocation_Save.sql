CREATE PROCEDURE dbo.spAdminLocation_Save @Id INT=NULL,@StationName NVARCHAR(100),@IsActive BIT AS
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
