CREATE UNIQUE INDEX UX_Meters_Replacement ON dbo.Meters(ReplacesMeterId) WHERE ReplacesMeterId IS NOT NULL;
