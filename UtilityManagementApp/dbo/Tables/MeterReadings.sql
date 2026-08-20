CREATE TABLE [dbo].[MeterReadings]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1), 
	[MeterId] INT NOT NULL, 
	[EmployeeId] INT NOT NULL, 
	[ReadingDate] DATETIME NOT NULL DEFAULT GETDATE(), 
	[CurrentReading] INT NOT NULL,
	[PreviousReading] INT NOT NULL,
	[Usage] AS (CurrentReading - PreviousReading) PERSISTED,
	[Notes] NVARCHAR(255) NULL,
	[IsDeleted] BIT NOT NULL DEFAULT 0,
	[UpdatedAt] DATETIME  NULL,
	[UpdatedBy] INT NULL,
	CONSTRAINT [FK_MeterReadings_Employees] FOREIGN KEY (EmployeeId) REFERENCES [dbo].Employees(Id),
	CONSTRAINT [FK_MeterReadings_Meters] FOREIGN KEY (MeterId) REFERENCES [dbo].Meters(Id),
	CONSTRAINT [FK_MeterReadings_UpdatedBy] FOREIGN KEY (UpdatedBy) REFERENCES [dbo].Employees(Id)
	)