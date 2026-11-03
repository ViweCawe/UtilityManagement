CREATE PROCEDURE dbo.spWaterNetworkDocument_Save
 @NetworkJson NVARCHAR(MAX), @ExpectedRevision INT, @UpdatedBy NVARCHAR(256)
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF ISJSON(@NetworkJson)<>1 THROW 51003, 'Invalid network document.', 1;
 BEGIN TRANSACTION;
 DECLARE @CurrentRevision INT;
 SELECT @CurrentRevision=Revision FROM dbo.WaterNetworkDocument WITH(UPDLOCK,HOLDLOCK) WHERE DocumentId=1;
 IF @CurrentRevision IS NULL
 BEGIN INSERT dbo.WaterNetworkDocument(DocumentId,NetworkJson,Revision) VALUES(1,NULL,0); SET @CurrentRevision=0; END;
 IF @CurrentRevision<>@ExpectedRevision
 BEGIN ROLLBACK TRANSACTION; THROW 51004, 'The network changed. Reload before saving.', 1; END;
 IF EXISTS (SELECT 1 FROM OPENJSON(@NetworkJson,'$.nodes') WITH(MeterId INT '$.meterId') n
            LEFT JOIN dbo.Meters m ON m.Id=n.MeterId WHERE n.MeterId IS NOT NULL AND (m.Id IS NULL OR m.MeterType<>1 OR m.IsActive<>1))
 BEGIN ROLLBACK TRANSACTION; THROW 51005, 'A water meter is no longer available.', 1; END;
 UPDATE dbo.WaterNetworkDocument SET NetworkJson=@NetworkJson,Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedBy=@UpdatedBy WHERE DocumentId=1;
 -- Only diagram assignments are replaced; meter and reading records remain intact.
 DELETE FROM dbo.WaterNetworkLinks;
 INSERT dbo.WaterNetworkLinks(NodeId,MeterId)
 SELECT NodeId,MeterId FROM OPENJSON(@NetworkJson,'$.nodes') WITH(NodeId NVARCHAR(80) '$.id',MeterId INT '$.meterId');
 COMMIT TRANSACTION;
END
