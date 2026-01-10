-- =============================================================================
-- CallistraAgent Database Setup Script
-- =============================================================================
-- This script creates the CallistraAgent database and all required tables
-- Run this script on your SQL Server instance (SQL Server 2019 or later)
-- =============================================================================

USE master;
GO

-- Create the database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'CallistraAgent')
BEGIN
    CREATE DATABASE CallistraAgent;
    PRINT 'Database CallistraAgent created successfully.';
END
ELSE
BEGIN
    PRINT 'Database CallistraAgent already exists.';
END
GO

-- Switch to the CallistraAgent database
USE CallistraAgent;
GO

PRINT 'Switched to CallistraAgent database.';
GO

-- =============================================================================
-- Create Members Table
-- =============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Members' AND SCHEMA_NAME(schema_id) = 'dbo')
BEGIN
    CREATE TABLE [dbo].[Members]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [FirstName] NVARCHAR(100) NOT NULL,
        [LastName] NVARCHAR(100) NOT NULL,
        [PhoneNumber] NVARCHAR(20) NOT NULL,
        [Program] NVARCHAR(100) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_Members] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_Member_PhoneNumber] UNIQUE ([PhoneNumber]),
        CONSTRAINT [CHK_Member_Status] CHECK ([Status] IN ('Active', 'Pending', 'Inactive'))
    );

    -- Index for filtering active members
    CREATE NONCLUSTERED INDEX [IX_Members_Status]
        ON [dbo].[Members]([Status] ASC);

    -- Update trigger to automatically set UpdatedAt
    EXEC('
    CREATE TRIGGER [dbo].[TR_Members_UpdateTimestamp]
    ON [dbo].[Members]
    AFTER UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;
        UPDATE [dbo].[Members]
        SET [UpdatedAt] = GETUTCDATE()
        WHERE [Id] IN (SELECT DISTINCT [Id] FROM Inserted);
    END;
    ');

    PRINT 'Table [dbo].[Members] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [dbo].[Members] already exists.';
END
GO

-- =============================================================================
-- Create CallSessions Table
-- =============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CallSessions' AND SCHEMA_NAME(schema_id) = 'dbo')
BEGIN
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

    -- Index for looking up all calls for a member
    CREATE NONCLUSTERED INDEX [IX_CallSessions_MemberId]
        ON [dbo].[CallSessions]([MemberId] ASC);

    -- Index for filtering in-progress calls
    CREATE NONCLUSTERED INDEX [IX_CallSessions_Status]
        ON [dbo].[CallSessions]([Status] ASC);

    -- Index for webhook event routing (filtered index requires QUOTED_IDENTIFIER ON)
    SET QUOTED_IDENTIFIER ON;
    -- Index for recent calls first
    CREATE NONCLUSTERED INDEX [IX_CallSessions_StartTime]
        ON [dbo].[CallSessions]([StartTime] DESC);

    -- Update trigger to automatically set UpdatedAt
    EXEC('
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
    ');

    PRINT 'Table [dbo].[CallSessions] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [dbo].[CallSessions] already exists.';
END
GO

-- =============================================================================
-- Create CallResponses Table
-- =============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CallResponses' AND SCHEMA_NAME(schema_id) = 'dbo')
BEGIN
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

    -- Index for looking up all responses for a call
    CREATE NONCLUSTERED INDEX [IX_CallResponses_CallSessionId]
        ON [dbo].[CallResponses]([CallSessionId] ASC);

    -- Unique composite index to ensure one answer per question per call
    CREATE UNIQUE NONCLUSTERED INDEX [IX_CallResponses_CallSession_Question]
        ON [dbo].[CallResponses]([CallSessionId] ASC, [QuestionNumber] ASC);

    PRINT 'Table [dbo].[CallResponses] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [dbo].[CallResponses] already exists.';
END
GO

-- =============================================================================
-- Insert Test Data (Optional)
-- =============================================================================
-- Uncomment the following section to insert test data

/*
-- Insert test members
IF NOT EXISTS (SELECT 1 FROM [dbo].[Members] WHERE [PhoneNumber] = '+18005551001')
BEGIN
    INSERT INTO [dbo].[Members] ([FirstName], [LastName], [PhoneNumber], [Program], [Status])
    VALUES
        ('John', 'Doe', '+18005551001', 'Diabetes Care', 'Active'),
        ('Jane', 'Smith', '+18005551002', 'Wellness Program', 'Active'),
        ('Bob', 'Johnson', '+18005551003', 'Heart Health', 'Active');

    PRINT 'Test members inserted successfully.';
END
ELSE
BEGIN
    PRINT 'Test members already exist.';
END
GO
*/

-- =============================================================================
-- Verification
-- =============================================================================
PRINT '';
PRINT '=============================================================================';
PRINT 'Database Setup Complete!';
PRINT '=============================================================================';
PRINT '';
PRINT 'Tables created:';
SELECT
    t.name AS TableName,
    COUNT(c.column_id) AS ColumnCount,
    (SELECT COUNT(*) FROM sys.indexes WHERE object_id = t.object_id AND index_id > 0) AS IndexCount
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
    AND t.name IN ('Members', 'CallSessions', 'CallResponses')
GROUP BY t.name, t.object_id
ORDER BY t.name;
GO

PRINT '';
PRINT 'You can now run the application with this connection string:';
PRINT 'Server=localhost;Database=CallistraAgent;Integrated Security=true;TrustServerCertificate=true;';
PRINT '';
