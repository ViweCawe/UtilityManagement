CREATE PROCEDURE dbo.spWaterNetworkDocument_Get AS
BEGIN
 SET NOCOUNT ON;
 SELECT NetworkJson,Revision FROM dbo.WaterNetworkDocument WHERE DocumentId=1;
END
