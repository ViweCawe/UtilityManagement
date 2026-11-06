CREATE TABLE [dbo].[Meters]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[MeterName] NVARCHAR(100) NOT NULL,
	[MeterType] INT  NOT NULL,
	--1: Water, 2: Electricity (matches MeterType enum)
	[Unit] NVARCHAR(50) NOT NULL, -- Original counter unit: L, m³, kL or kWh.
	[AssetKind] INT NOT NULL DEFAULT 1,
    [NetworkRole] INT NOT NULL DEFAULT 4,
    [CapacityLitres] DECIMAL(14,2) NULL,
    CONSTRAINT CK_Meters_Asset CHECK (
        (AssetKind = 1 AND NetworkRole IN (1,2,3,4) AND CapacityLitres IS NULL)
        OR (AssetKind = 2 AND MeterType = 1 AND NetworkRole IN (1,3,5) AND (CapacityLitres IS NULL OR CapacityLitres > 0))),
    [SerialNumber] NVARCHAR(100) NOT NULL DEFAULT '',
    [FlowDirection] INT NOT NULL DEFAULT 0,
    [ReplacesMeterId] INT NULL,
    [InstalledOn] DATETIME2(0) NULL,
    [RetiredOn] DATETIME2(0) NULL,
    CONSTRAINT FK_Meters_Replaces FOREIGN KEY (ReplacesMeterId) REFERENCES dbo.Meters(Id),
    CONSTRAINT CK_Meters_Direction CHECK(FlowDirection IN(0,1,2)),
    CONSTRAINT CK_Meters_Service CHECK((InstalledOn IS NULL OR RetiredOn IS NULL OR InstalledOn<=RetiredOn) AND (ReplacesMeterId IS NULL OR ReplacesMeterId<>Id)),
    [AreaId] INT NOT NULL,
	[DepartmentId] INT NOT NULL,
	[StationId] INT NOT NULL,
	[CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
	[IsActive] BIT NOT NULL DEFAULT 1,
	[IsDeleted] BIT NOT NULL DEFAULT 0,
	[IsCumulative] BIT NOT NULL DEFAULT 0,
	CONSTRAINT [FK_Meters_Area] FOREIGN KEY (AreaId) REFERENCES [dbo].Area(Id),
    CONSTRAINT [FK_Meters_Stations] FOREIGN KEY (StationId) REFERENCES [dbo].Stations(Id),
	CONSTRAINT [FK_Meters_Departments] FOREIGN KEY (DepartmentId) REFERENCES [dbo].Departments(Id)
)
