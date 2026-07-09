CREATE PROCEDURE [dbo].[spArea_Update]
(
	@Id INT,
	@AreaName NVARCHAR(100),
	@UpdatedAt DATETIME2,
	@UpdatedBy NVARCHAR(50),
	@IsActive BIT
	
)

AS
BEGIN	
	SET NOCOUNT ON;

	UPDATE dbo.Area
		SET 
			AreaName = @AreaName,
			IsActive = @IsActive
	WHERE Id = @Id

END