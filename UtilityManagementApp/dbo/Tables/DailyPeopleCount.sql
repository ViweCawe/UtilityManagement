CREATE TABLE [dbo].[DailyPeopleCount]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[Date] DATETIME NOT NULL UNIQUE,
	[Visitors] INT NULL,
	[Employees] INT NULL,
	[Total]  AS (Visitors + Employees ) PERSISTED
)
