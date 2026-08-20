CREATE  PROCEDURE [dbo].[spMeterReadings_Insert]
(
    @MeterId INT,
    @EmployeeId INT,
    @ReadingDate DATETIME2(0),
    @CurrentReading INT,
    @Notes NVARCHAR(500),
    @Id INT OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Remove seconds.
    SET @ReadingDate =
        DATEADD(MINUTE, DATEDIFF(MINUTE, 0, @ReadingDate), 0);

    DECLARE @PreviousReading INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Only obtain the latest reading for this meter.
        SELECT TOP (1)
            @PreviousReading = CurrentReading
        FROM dbo.MeterReadings WITH (UPDLOCK, HOLDLOCK)
        WHERE MeterId = @MeterId
        ORDER BY ReadingDate DESC, Id DESC;

        SET @PreviousReading = ISNULL(@PreviousReading, 0);

        -- Validate against this meter only.
        IF @CurrentReading < @PreviousReading
        BEGIN
            DECLARE @ErrorMessage NVARCHAR(2048);

            SET @ErrorMessage = CONCAT(
                'Current reading cannot be less than this meter''s ',
                'previous reading of ',
                @PreviousReading,
                '.'
            );

            THROW 50001, @ErrorMessage, 1;
        END;

        INSERT INTO dbo.MeterReadings
        (
            MeterId,
            EmployeeId,
            ReadingDate,
            CurrentReading,
            PreviousReading,
            Notes
        )
        VALUES
        (
            @MeterId,
            @EmployeeId,
            @ReadingDate,
            @CurrentReading,
            @PreviousReading,
            ISNULL(@Notes, '')
        );

        SET @Id = CONVERT(INT, SCOPE_IDENTITY());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO