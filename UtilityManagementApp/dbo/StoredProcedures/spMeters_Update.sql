CREATE PROCEDURE [dbo].[spMeters_Update]
	@Id INT,
	@MeterName NVARCHAR(255),
	@MeterType NVARCHAR(100),
	@AreaId INT,
	@IsActive BIT,
	@IsCumulative BIT
AS
BEGIN

SET NOCOUNT ON;

	UPDATE dbo.Meters
	SET 
		MeterName = @MeterName,
		MeterType = @MeterType,
		AreaId = @AreaId,
		IsActive = @IsActive,
		IsCumulative = @IsCumulative
	WHERE Id = @Id


END