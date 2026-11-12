/* The least-privilege user the MCP server connects as, and the row-level security that limits which clients
   each caller sees. 03_procedures.sql grants the one thing mcp_reader may do. */

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

/* Row-level security: each caller sees only the clients trading.CallerClient lists for it, with their accounts
   and trades. The MCP server sets the caller's key on every connection, read-only (SESSION_CONTEXT 'caller_key').
   The filter applies to every query on these tables, so the stored procedures don't need to remember it.
   A caller with no key, or no rows in CallerClient, sees nothing. db_owner, the admin who loads the data,
   sees everything. */
CREATE FUNCTION trading.fn_CallerCanSeeClient(@ClientId INT)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS CanSee
    WHERE  IS_MEMBER(N'db_owner') = 1
       OR  EXISTS (SELECT 1
                   FROM   trading.CallerClient cc
                   WHERE  cc.ClientId  = @ClientId
                     AND  cc.CallerKey = CAST(SESSION_CONTEXT(N'caller_key') AS NVARCHAR(128)));
GO

CREATE FUNCTION trading.fn_CallerCanSeeAccount(@AccountId INT)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS CanSee
    WHERE  IS_MEMBER(N'db_owner') = 1
       OR  EXISTS (SELECT 1
                   FROM   trading.Account a
                   JOIN   trading.CallerClient cc ON cc.ClientId = a.ClientId
                   WHERE  a.AccountId  = @AccountId
                     AND  cc.CallerKey = CAST(SESSION_CONTEXT(N'caller_key') AS NVARCHAR(128)));
GO

CREATE SECURITY POLICY trading.CallerClientFilter
    ADD FILTER PREDICATE trading.fn_CallerCanSeeClient(ClientId)   ON trading.Client,
    ADD FILTER PREDICATE trading.fn_CallerCanSeeClient(ClientId)   ON trading.Account,
    ADD FILTER PREDICATE trading.fn_CallerCanSeeAccount(AccountId) ON trading.Trade
    WITH (STATE = ON);
GO

SET NOEXEC OFF;   -- re-enable execution if the safety check above skipped the script
GO
