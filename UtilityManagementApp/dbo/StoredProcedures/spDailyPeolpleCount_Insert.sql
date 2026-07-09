CREATE PROCEDURE [dbo].[spDailyPeolpleCount_Insert]
(
	@Visitors INT,
	@Employees INT,
	@Date DATETIME,
	@Id INT OUTPUT 

)

AS
BEGIN
	SET NOCOUNT ON;
	
		INSERT INTO dbo.DailyPeopleCount
		(
		Visitor,
		Employees,
		[Date]

		)
	VALUES
	(
		@Visitors,
		@Employees,
		@Date
	)
	SET @Id = CAST(SCOPE_IDENTITY() AS INT);
	
END