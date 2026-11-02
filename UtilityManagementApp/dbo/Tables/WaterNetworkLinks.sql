CREATE TABLE dbo.WaterNetworkLinks (
 NodeId NVARCHAR(80) NOT NULL CONSTRAINT PK_WaterNetworkLinks PRIMARY KEY,
 MeterId INT NULL CONSTRAINT FK_WaterNetworkLinks_Meters REFERENCES dbo.Meters(Id)
);
GO
CREATE UNIQUE INDEX UX_WaterNetworkLinks_Meter ON dbo.WaterNetworkLinks(MeterId) WHERE MeterId IS NOT NULL;
