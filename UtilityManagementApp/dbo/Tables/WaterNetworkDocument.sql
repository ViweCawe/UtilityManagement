CREATE TABLE dbo.WaterNetworkDocument (
 DocumentId TINYINT NOT NULL CONSTRAINT PK_WaterNetworkDocument PRIMARY KEY CONSTRAINT CK_WaterNetworkDocument_Id CHECK (DocumentId=1),
 NetworkJson NVARCHAR(MAX) NULL CONSTRAINT CK_WaterNetworkDocument_Json CHECK (NetworkJson IS NULL OR ISJSON(NetworkJson)=1),
 Revision INT NOT NULL CONSTRAINT CK_WaterNetworkDocument_Revision CHECK (Revision>=0),
 UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_WaterNetworkDocument_UpdatedAt DEFAULT SYSUTCDATETIME(),
 UpdatedBy NVARCHAR(256) NULL
);
