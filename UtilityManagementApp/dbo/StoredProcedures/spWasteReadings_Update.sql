CREATE   PROCEDURE dbo.spWasteReadings_Update
(
    @Id INT,
    @WasteAmount DECIMAL(18, 2),
    @Notes NVARCHAR(500),
    @UpdatedAt DATETIME2,
    @UpdatedBy INT
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.WasteReading
    SET
        WasteAmount = @WasteAmount,
        Notes = @Notes,
        UpdatedAt = @UpdatedAt,
        UpdatedBy = @UpdatedBy
    WHERE Id = @Id;

    RETURN @@ROWCOUNT;
END;
GO