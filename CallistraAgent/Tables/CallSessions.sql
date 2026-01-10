-- CallSessions table
-- Represents a single outbound call attempt to a member
CREATE TABLE [dbo].[CallSessions]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [MemberId] INT NOT NULL,
    [CallConnectionId] NVARCHAR(100) NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Initiated',
    [StartTime] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [EndTime] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_CallSessions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_CallSession_Member] FOREIGN KEY ([MemberId]) REFERENCES [dbo].[Members]([Id]),
    CONSTRAINT [UQ_CallSession_CallConnectionId] UNIQUE ([CallConnectionId]),
    CONSTRAINT [CHK_CallSession_Status] CHECK ([Status] IN ('Initiated', 'Ringing', 'Connected', 'Completed', 'Disconnected', 'Failed', 'NoAnswer', 'VoicemailMessage')),
    CONSTRAINT [CHK_CallSession_EndTime] CHECK ([EndTime] IS NULL OR [EndTime] >= [StartTime])
);
GO

-- Index for looking up all calls for a member
CREATE NONCLUSTERED INDEX [IX_CallSessions_MemberId]
    ON [dbo].[CallSessions]([MemberId] ASC);
GO

-- Index for filtering in-progress calls
CREATE NONCLUSTERED INDEX [IX_CallSessions_Status]
    ON [dbo].[CallSessions]([Status] ASC);
GO

-- Index for webhook event routing
CREATE NONCLUSTERED INDEX [IX_CallSessions_CallConnectionId]
    ON [dbo].[CallSessions]([CallConnectionId] ASC)
    WHERE [CallConnectionId] IS NOT NULL;
GO

-- Index for recent calls first
CREATE NONCLUSTERED INDEX [IX_CallSessions_StartTime]
    ON [dbo].[CallSessions]([StartTime] DESC);
GO

-- Update trigger to automatically set UpdatedAt
CREATE TRIGGER [dbo].[TR_CallSessions_UpdateTimestamp]
ON [dbo].[CallSessions]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[CallSessions]
    SET [UpdatedAt] = GETUTCDATE()
    WHERE [Id] IN (SELECT DISTINCT [Id] FROM Inserted);
END;
GO
