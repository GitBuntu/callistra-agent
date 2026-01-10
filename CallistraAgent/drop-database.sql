-- Drop and recreate CallistraAgent database
-- Run this to start fresh after fixing the setup script

USE master;
GO

-- Close all connections to the database
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'CallistraAgent')
BEGIN
    ALTER DATABASE CallistraAgent SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE CallistraAgent;
    PRINT 'Database CallistraAgent dropped successfully.';
END
GO

PRINT 'You can now run: sqlcmd -S localhost -E -C -i setup-database.sql';
