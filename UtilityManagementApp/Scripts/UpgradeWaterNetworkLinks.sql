-- Run after UpgradeMeterAssets.sql, before deploying the updated network page.
-- No meter/readings records are deleted. Null assignment explicitly unlinks a point.
IF OBJECT_ID('dbo.WaterNetworkLinks','U') IS NULL
BEGIN
 CREATE TABLE dbo.WaterNetworkLinks (
  NodeId NVARCHAR(80) NOT NULL CONSTRAINT PK_WaterNetworkLinks PRIMARY KEY,
  MeterId INT NULL CONSTRAINT FK_WaterNetworkLinks_Meters REFERENCES dbo.Meters(Id));
 CREATE UNIQUE INDEX UX_WaterNetworkLinks_Meter ON dbo.WaterNetworkLinks(MeterId) WHERE MeterId IS NOT NULL;
END
GO
CREATE OR ALTER PROCEDURE dbo.spWaterNetworkLinks_All AS
BEGIN SET NOCOUNT ON; SELECT NodeId, MeterId FROM dbo.WaterNetworkLinks; END
GO
CREATE OR ALTER PROCEDURE dbo.spWaterNetworkLinks_Save @NodeId NVARCHAR(80), @MeterId INT = NULL AS
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
GO
CREATE OR ALTER PROCEDURE dbo.spMeterReading_GetLatestAll
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Latest AS
    (
        SELECT *,
               ROW_NUMBER() OVER
               (
                   PARTITION BY MeterId
                   ORDER BY ReadingDate DESC, Id DESC
               ) AS [RN]
        FROM dbo.MeterReadings
        WHERE IsDeleted = 0
    )
    SELECT *
    FROM Latest
    WHERE RN = 1
    ORDER BY MeterId;
END
GO
