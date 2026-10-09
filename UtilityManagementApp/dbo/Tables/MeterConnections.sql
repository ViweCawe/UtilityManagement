CREATE TABLE [dbo].[MeterConnections]
(
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UpstreamMeterId] INT NOT NULL,
    [DownstreamMeterId] INT NOT NULL,
    [ValidFrom] DATETIME2 NOT NULL CONSTRAINT [DF_MeterConnections_ValidFrom] DEFAULT SYSUTCDATETIME(),
    [ValidTo] DATETIME2 NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_MeterConnections_IsActive] DEFAULT (1),
    CONSTRAINT [FK_MeterConnections_Upstream] FOREIGN KEY ([UpstreamMeterId]) REFERENCES [dbo].[Meters]([Id]),
    CONSTRAINT [FK_MeterConnections_Downstream] FOREIGN KEY ([DownstreamMeterId]) REFERENCES [dbo].[Meters]([Id]),
    CONSTRAINT [CK_MeterConnections_NotSelf] CHECK ([UpstreamMeterId] <> [DownstreamMeterId]),
    CONSTRAINT [CK_MeterConnections_Dates] CHECK ([ValidTo] IS NULL OR [ValidTo] > [ValidFrom])
);
GO
CREATE INDEX [IX_MeterConnections_Upstream] ON [dbo].[MeterConnections] ([UpstreamMeterId], [IsActive], [ValidFrom]);
GO
CREATE INDEX [IX_MeterConnections_Downstream] ON [dbo].[MeterConnections] ([DownstreamMeterId], [IsActive], [ValidFrom]);
GO
-- Prevent cycles, overlapping active parent relationships and cross-utility links
-- in the mapping service before enabling any connection. Do not populate
-- relationships until the physical water/electricity mapping is verified.
