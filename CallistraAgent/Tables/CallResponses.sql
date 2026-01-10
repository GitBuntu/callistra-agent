-- CallResponses table
-- Represents a member's answer to a specific question during a call
CREATE TABLE [dbo].[CallResponses]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [CallSessionId] INT NOT NULL,
    [QuestionNumber] INT NOT NULL,
    [QuestionText] NVARCHAR(500) NOT NULL,
    [ResponseValue] INT NOT NULL,
    [RespondedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_CallResponses] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_CallResponse_CallSession] FOREIGN KEY ([CallSessionId]) REFERENCES [dbo].[CallSessions]([Id]),
    CONSTRAINT [CHK_CallResponse_QuestionNumber] CHECK ([QuestionNumber] BETWEEN 1 AND 3),
    CONSTRAINT [CHK_CallResponse_ResponseValue] CHECK ([ResponseValue] IN (1, 2))
);
GO

-- Index for looking up all responses for a call
CREATE NONCLUSTERED INDEX [IX_CallResponses_CallSessionId]
    ON [dbo].[CallResponses]([CallSessionId] ASC);
GO

-- Unique composite index to ensure one answer per question per call
CREATE UNIQUE NONCLUSTERED INDEX [IX_CallResponses_CallSession_Question]
    ON [dbo].[CallResponses]([CallSessionId] ASC, [QuestionNumber] ASC);
GO
