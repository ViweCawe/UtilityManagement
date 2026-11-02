CREATE PROCEDURE dbo.spWaterNetworkLinks_Save @NodeId NVARCHAR(80), @MeterId INT = NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @MeterId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Meters WHERE Id=@MeterId AND MeterType=1 AND IsActive=1)
  THROW 51002, 'Select an active water meter.', 1;
 BEGIN TRANSACTION;
 IF EXISTS (SELECT 1 FROM dbo.WaterNetworkLinks WITH (UPDLOCK,HOLDLOCK) WHERE NodeId=@NodeId)
  UPDATE dbo.WaterNetworkLinks SET MeterId=@MeterId WHERE NodeId=@NodeId;
 ELSE INSERT dbo.WaterNetworkLinks(NodeId,MeterId) VALUES(@NodeId,@MeterId);
 COMMIT TRANSACTION;
END
