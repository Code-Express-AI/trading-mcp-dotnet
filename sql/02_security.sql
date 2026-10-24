/* The least-privilege user the MCP server connects as. 03_procedures.sql grants the one thing it may do. */

/* Safety check: never create objects in a system database. */
IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
BEGIN
    RAISERROR(N'You are connected to a system database. Switch to the TradingDemo database first, then run this script again.', 16, 1);
    SET NOEXEC ON;   -- skip everything below
END
GO

-- A contained database user: no server login needed, so this works the same on Azure SQL.
CREATE USER mcp_reader WITH PASSWORD = '<<replace-with-a-long-random-password>>';

-- No direct access to any table. Whatever the server code (or a prompt-injected model) tries,
-- reads and writes fail at the database. The stored procedures in 03 are the only way in.
DENY SELECT, INSERT, UPDATE, DELETE, ALTER ON SCHEMA::trading TO mcp_reader;

-- Client.Email and Account.AccountNumber are masked in 01_schema.sql. mcp_reader has no UNMASK
-- permission, so it only ever sees masked values, e.g. ACCXXXXX678.
GO

SET NOEXEC OFF;   -- re-enable execution if the safety check above skipped the script
GO
