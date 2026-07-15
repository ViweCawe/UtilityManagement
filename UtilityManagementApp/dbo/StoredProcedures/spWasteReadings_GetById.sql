CREATE PROCEDURE [dbo].[spWasteReadings_GetById]
    (
        @Id INT
    )
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        wr.Id,
        wr.WasteTypeId,
        wt.CategoryId,
        wt.WasteMaterialId,
        wr.WasteAmount,
        wr.ReadingDate,
        wr.CapturedBy,
        wr.Notes,

        wt.Name AS WasteTypeName,
        wc.Name AS CategoryName,
        wm.Name AS MaterialName

    FROM dbo.WasteReading wr

    INNER JOIN dbo.WasteType wt
        ON wr.WasteTypeId = wt.Id

    INNER JOIN dbo.WasteCategory wc
        ON wt.CategoryId = wc.Id

    INNER JOIN dbo.WasteMaterial wm
        ON wt.WasteMaterialId = wm.Id
    WHERE [wr].Id = @Id
END