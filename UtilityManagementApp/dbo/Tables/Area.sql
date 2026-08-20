CREATE TABLE [dbo].[Area]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1), 
    [AreaName] NVARCHAR(100) NOT NULL, 
    [StationId] INT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(), 
    CONSTRAINT [FK_Area_Station] FOREIGN KEY (StationId) REFERENCES [dbo].[Stations](Id)


   

)
