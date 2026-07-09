CREATE TABLE [dbo].[DailyPeopleCount]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[Date] DATETIME NOT NULL UNIQUE,
	[Visitor] INT NOT NULL,
	[Employees] INT NOT NULL,
	[Total]  AS (Visitor - Employees ) PERSISTED
)
