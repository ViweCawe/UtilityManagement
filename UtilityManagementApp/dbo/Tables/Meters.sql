CREATE TABLE [dbo].[Meters]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1), 
	[MeterName] NVARCHAR(100) NOT NULL, 
	[MeterType] INT  NOT NULL,
	--1 : Electricity, 2: Water, 3: Refuse
	[Unit] NVARCHAR(50) NOT NULL, -- kWh for electricity, Liters for water, Kg For Refuse.
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
