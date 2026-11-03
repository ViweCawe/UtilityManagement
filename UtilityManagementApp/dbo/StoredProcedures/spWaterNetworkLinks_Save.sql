CREATE PROCEDURE dbo.spWaterNetworkLinks_Save @NodeId NVARCHAR(80), @MeterId INT = NULL AS
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
