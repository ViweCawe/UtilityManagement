CREATE PROCEDURE [dbo].[spWasteReadings_Insert]
(

   @WasteTypeId INT,
   @CreatedBy INT,
   @WasteAmount DECIMAL(18,2),
   @RecordedAt DATETIME ,
   @Notes NVARCHAR (150),
   @Id INT OUTPUT

)
AS 
BEGIN
	SET NOCOUNT ON;

	  DECLARE @CategoryId INT;
      DECLARE @MaterialId INT;

    SELECT
        @CategoryId = CategoryId,
        @MaterialId = WasteMaterialId
    FROM dbo.WasteType
    WHERE Id = @WasteTypeId;

	INSERT INTO dbo.WasteReading
	(
	WasteTypeId,
	CapturedBy,
	WasteReading,
	ReadingDate,
	Notes
	)
	VALUES(
	@WasteTypeId ,
	@CreatedBy,
	@WasteAmount,
	@RecordedAt,
	@Notes

	)
	 SET @Id = CAST(SCOPE_IDENTITY() AS INT);
END
GO