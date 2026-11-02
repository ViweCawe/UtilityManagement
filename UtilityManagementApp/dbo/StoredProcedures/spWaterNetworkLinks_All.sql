CREATE PROCEDURE dbo.spWaterNetworkLinks_All AS
BEGIN SET NOCOUNT ON; SELECT NodeId, MeterId FROM dbo.WaterNetworkLinks; END
