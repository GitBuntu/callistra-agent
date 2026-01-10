-- Members table
-- Represents a healthcare program enrollee who can receive outreach calls
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
GO

-- Index for filtering active members
CREATE NONCLUSTERED INDEX [IX_Members_Status]
    ON [dbo].[Members]([Status] ASC);
GO

-- Update trigger to automatically set UpdatedAt
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
GO
