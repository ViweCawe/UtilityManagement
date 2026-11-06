CREATE PROCEDURE dbo.spMeters_Delete @Id INT AS
BEGIN
 SET NOCOUNT ON;
 -- Retire the physical meter; reading and replacement history must survive.
 UPDATE dbo.Meters SET IsActive=0,RetiredOn=COALESCE(RetiredOn,GETDATE()) WHERE Id=@Id;
END;
