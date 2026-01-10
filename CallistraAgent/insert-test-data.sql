-- =============================================================================
-- Insert Test Members for CallistraAgent
-- =============================================================================
-- Run this script after setup-database.sql to populate test data
-- =============================================================================

USE CallistraAgent;
GO

-- Clear existing test data (optional - comment out if you want to keep data)
-- DELETE FROM [dbo].[CallResponses];
-- DELETE FROM [dbo].[CallSessions];
-- DELETE FROM [dbo].[Members];
-- GO

-- Insert test members
SET IDENTITY_INSERT [dbo].[Members] ON;
GO

MERGE INTO [dbo].[Members] AS Target
USING (VALUES
    (1, 'John', 'Doe', '+18005551001', 'Diabetes Care', 'Active'),
    (2, 'Jane', 'Smith', '+18005551002', 'Wellness Program', 'Active'),
    (3, 'Bob', 'Johnson', '+18005551003', 'Heart Health', 'Active'),
    (4, 'Alice', 'Williams', '+18005551004', 'Mental Health', 'Active'),
    (5, 'Charlie', 'Brown', '+18005551005', 'Preventive Care', 'Pending')
) AS Source ([Id], [FirstName], [LastName], [PhoneNumber], [Program], [Status])
ON Target.[Id] = Source.[Id]
WHEN MATCHED THEN
    UPDATE SET
        [FirstName] = Source.[FirstName],
        [LastName] = Source.[LastName],
        [PhoneNumber] = Source.[PhoneNumber],
        [Program] = Source.[Program],
        [Status] = Source.[Status],
        [UpdatedAt] = GETUTCDATE()
WHEN NOT MATCHED THEN
    INSERT ([Id], [FirstName], [LastName], [PhoneNumber], [Program], [Status])
    VALUES (Source.[Id], Source.[FirstName], Source.[LastName], Source.[PhoneNumber], Source.[Program], Source.[Status]);

SET IDENTITY_INSERT [dbo].[Members] OFF;
GO

PRINT 'Test members inserted/updated successfully.';
GO

-- Verify inserted data
SELECT
    [Id],
    [FirstName],
    [LastName],
    [PhoneNumber],
    [Program],
    [Status],
    [CreatedAt]
FROM [dbo].[Members]
ORDER BY [Id];
GO

PRINT '';
PRINT '=============================================================================';
PRINT 'Test Data Setup Complete!';
PRINT '=============================================================================';
PRINT 'Total Members: ' + CAST((SELECT COUNT(*) FROM [dbo].[Members]) AS VARCHAR(10));
PRINT 'Active Members: ' + CAST((SELECT COUNT(*) FROM [dbo].[Members] WHERE [Status] = ''Active'') AS VARCHAR(10));
PRINT '';
