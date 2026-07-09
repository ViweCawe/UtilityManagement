CREATE TABLE [dbo].[WasteReading]
(
	Id INT IDENTITY(1,1) PRIMARY KEY,

    WasteTypeId INT NOT NULL,
    WasteReading DECIMAL(18,2) NOT NULL,
    ReadingDate DATETIME2 NOT NULL,
    CapturedBy INT NOT NULL,
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    Notes NVARCHAR(500) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_WasteReading_WasteType
        FOREIGN KEY (WasteTypeId)
        REFERENCES WasteType(Id),
    CONSTRAINT FK_WasteReading_Employees
        FOREIGN KEY (CapturedBy)
        REFERENCES Employees(Id),
   

                


  )
