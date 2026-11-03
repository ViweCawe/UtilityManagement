-- Apply UpgradeWaterNetworkLinks.sql first. Run this migration BEFORE deploying the editor.
-- This preserves the current map on reruns and never deletes meters or readings.
IF OBJECT_ID('dbo.WaterNetworkDocument','U') IS NULL
BEGIN
CREATE TABLE dbo.WaterNetworkDocument (
 DocumentId TINYINT NOT NULL CONSTRAINT PK_WaterNetworkDocument PRIMARY KEY CONSTRAINT CK_WaterNetworkDocument_Id CHECK (DocumentId=1),
 NetworkJson NVARCHAR(MAX) NULL CONSTRAINT CK_WaterNetworkDocument_Json CHECK (NetworkJson IS NULL OR ISJSON(NetworkJson)=1),
 Revision INT NOT NULL CONSTRAINT CK_WaterNetworkDocument_Revision CHECK (Revision>=0),
 UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_WaterNetworkDocument_UpdatedAt DEFAULT SYSUTCDATETIME(),
 UpdatedBy NVARCHAR(256) NULL
);
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.WaterNetworkDocument WHERE DocumentId=1)
 INSERT dbo.WaterNetworkDocument(DocumentId,NetworkJson,Revision) VALUES(1,NULL,0);
GO
CREATE OR ALTER PROCEDURE dbo.spWaterNetworkDocument_Get AS
BEGIN
 SET NOCOUNT ON;
 SELECT NetworkJson,Revision FROM dbo.WaterNetworkDocument WHERE DocumentId=1;
END
GO
CREATE OR ALTER PROCEDURE dbo.spWaterNetworkDocument_Save
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
GO
CREATE OR ALTER PROCEDURE dbo.spWaterNetworkLinks_Save @NodeId NVARCHAR(80), @MeterId INT = NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRANSACTION;
 -- Take the same document lock as full-map saves; meter-link edits invalidate stale editors.
 DECLARE @Revision INT;
 SELECT @Revision=Revision FROM dbo.WaterNetworkDocument WITH(UPDLOCK,HOLDLOCK) WHERE DocumentId=1;
 IF @Revision IS NULL
  INSERT dbo.WaterNetworkDocument(DocumentId,NetworkJson,Revision) VALUES(1,NULL,0);
 IF EXISTS (SELECT 1 FROM dbo.WaterNetworkDocument WHERE DocumentId=1 AND NetworkJson IS NOT NULL)
    AND NOT EXISTS (SELECT 1 FROM dbo.WaterNetworkDocument d CROSS APPLY OPENJSON(d.NetworkJson,'$.nodes') WITH(NodeId NVARCHAR(80) '$.id') n WHERE d.DocumentId=1 AND n.NodeId=@NodeId)
 BEGIN ROLLBACK TRANSACTION; THROW 51006, 'This diagram point was removed. Reload the network.', 1; END;
 IF @MeterId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Meters WHERE Id=@MeterId AND MeterType=1 AND IsActive=1)
 BEGIN ROLLBACK TRANSACTION; THROW 51002, 'Select an active water meter.', 1; END;
 IF EXISTS (SELECT 1 FROM dbo.WaterNetworkLinks WITH(UPDLOCK,HOLDLOCK) WHERE NodeId=@NodeId)
  UPDATE dbo.WaterNetworkLinks SET MeterId=@MeterId WHERE NodeId=@NodeId;
 ELSE INSERT dbo.WaterNetworkLinks(NodeId,MeterId) VALUES(@NodeId,@MeterId);
 UPDATE dbo.WaterNetworkDocument SET Revision=Revision+1,UpdatedAt=SYSUTCDATETIME() WHERE DocumentId=1;
 COMMIT TRANSACTION;
END
GO
