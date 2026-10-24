/* The stored procedures: the only thing the MCP server can run. Safe to run again after changes.

   Why this works: the mcp and trading schemas are both owned by dbo, so SQL Server's "ownership chaining"
   lets a procedure read tables its caller can't touch. Each procedure is a hard cap on what the model can
   ask for: fixed columns, row limits, no free-form SQL. Dynamic SQL (EXEC(@sql)) breaks the chain, so
   don't use it in these procedures.

   Errors numbered 50000+ (THROW) are argument problems only the database can spot, such as an unknown
   ticker. The MCP server passes their messages straight to the model, so write them for it. */

/* Safety check: never create objects in a system database. */
IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
BEGIN
    RAISERROR(N'You are connected to a system database. Switch to the TradingDemo database first, then run this script again.', 16, 1);
    SET NOEXEC ON;   -- skip everything below
END
GO

-- CREATE SCHEMA has to be the only statement in its batch, hence EXEC.
IF SCHEMA_ID(N'mcp') IS NULL
    EXEC(N'CREATE SCHEMA mcp');
GO

CREATE OR ALTER PROCEDURE mcp.GetDatasetInfo
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  (SELECT MIN(PriceDate) FROM trading.InstrumentPrice) AS FirstPriceDate,
            (SELECT MAX(PriceDate) FROM trading.InstrumentPrice) AS LastPriceDate,
            (SELECT COUNT(*) FROM trading.Instrument)            AS Instruments,
            (SELECT COUNT(*) FROM trading.Account)               AS Accounts,
            (SELECT COUNT(*) FROM trading.Trade)                 AS Trades;
END
GO

CREATE OR ALTER PROCEDURE mcp.SearchInstruments
    @Text NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET @Text = LTRIM(RTRIM(@Text));
    IF LEN(@Text) < 1 RETURN;

    /* Escape LIKE wildcards so '%' or '_' match literally instead of matching everything. */
    DECLARE @Pattern NVARCHAR(300) = REPLACE(REPLACE(REPLACE(@Text, '[', '[[]'), '%', '[%]'), '_', '[_]');

    SELECT TOP (25) Ticker, Name, Sector, Currency
    FROM   trading.Instrument
    WHERE  Ticker LIKE @Pattern + '%' OR Name LIKE '%' + @Pattern + '%'
    ORDER BY Ticker;
END
GO

/* Most recent 250 bars inside the requested range, returned oldest-first. */
CREATE OR ALTER PROCEDURE mcp.GetPriceHistory
    @Ticker NVARCHAR(10),
    @From   DATE = NULL,
    @To     DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @From = ISNULL(@From, '1900-01-01');
    SET @To   = ISNULL(@To,   '9999-12-31');
    IF NOT EXISTS (SELECT 1 FROM trading.Instrument WHERE Ticker = @Ticker)
        THROW 50001, N'Unknown ticker. Use search_instruments to find the right one.', 1;

    WITH recent AS (
        SELECT TOP (250) p.PriceDate, p.OpenPrice, p.HighPrice, p.LowPrice, p.ClosePrice, p.Volume
        FROM   trading.InstrumentPrice p
        JOIN   trading.Instrument i ON i.InstrumentId = p.InstrumentId
        WHERE  i.Ticker = @Ticker
          AND  p.PriceDate BETWEEN @From AND @To
        ORDER BY p.PriceDate DESC
    )
    SELECT PriceDate,
           OpenPrice  AS [Open],
           HighPrice  AS [High],
           LowPrice   AS [Low],
           ClosePrice AS [Close],
           Volume
    FROM   recent
    ORDER BY PriceDate;
END
GO

/* Daily % change vs the previous trading day. @Date snaps back to the last trading day on/before it. */
CREATE OR ALTER PROCEDURE mcp.GetTopMovers
    @Date  DATE = NULL,
    @Count INT  = 5
AS
BEGIN
    SET NOCOUNT ON;
    IF @Count < 1  SET @Count = 1;
    IF @Count > 20 SET @Count = 20;

    SET @Date = (SELECT MAX(PriceDate) FROM trading.InstrumentPrice WHERE PriceDate <= ISNULL(@Date, '9999-12-31'));
    IF @Date IS NULL
        THROW 50003, N'No prices on or before this date. Call get_dataset_info for the available range.', 1;

    WITH w AS (
        SELECT p.InstrumentId, p.PriceDate, p.ClosePrice,
               LAG(p.ClosePrice) OVER (PARTITION BY p.InstrumentId ORDER BY p.PriceDate) AS PrevClose
        FROM   trading.InstrumentPrice p
        WHERE  p.PriceDate BETWEEN DATEADD(DAY, -7, @Date) AND @Date
    )
    SELECT TOP (@Count)
           i.Ticker, i.Name, w.PriceDate, w.ClosePrice, w.PrevClose,
           ROUND((w.ClosePrice / w.PrevClose - 1) * 100, 2) AS ChangePct
    FROM   w
    JOIN   trading.Instrument i ON i.InstrumentId = w.InstrumentId
    WHERE  w.PriceDate = @Date AND w.PrevClose IS NOT NULL
    ORDER BY ABS(w.ClosePrice / w.PrevClose - 1) DESC;
END
GO

/* AccountNumber comes back MASKED (Dynamic Data Masking), so the agent works with AccountId instead. */
CREATE OR ALTER PROCEDURE mcp.FindAccounts
    @Name NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET @Name = LTRIM(RTRIM(@Name));
    IF LEN(@Name) < 2 RETURN;

    /* Escape LIKE wildcards, otherwise '%%' passes the 2-character minimum and matches every client. */
    DECLARE @Pattern NVARCHAR(300) = REPLACE(REPLACE(REPLACE(@Name, '[', '[[]'), '%', '[%]'), '_', '[_]');

    SELECT TOP (25)
           a.AccountId, a.AccountNumber, c.FullName AS ClientName, c.Country, c.RiskProfile,
           a.AccountType, a.BaseCurrency, a.OpenedOn
    FROM   trading.Account a
    JOIN   trading.Client  c ON c.ClientId = a.ClientId
    WHERE  c.FullName LIKE '%' + @Pattern + '%'
    ORDER BY c.FullName, a.AccountId;
END
GO

CREATE OR ALTER PROCEDURE mcp.ListTrades
    @AccountId INT,
    @From      DATE = NULL,
    @To        DATE = NULL,
    @Ticker    NVARCHAR(10) = NULL,
    @Page      INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @PageSize INT = 50;
    IF @Page < 1    SET @Page = 1;
    IF @Page > 1000 SET @Page = 1000;
    IF NOT EXISTS (SELECT 1 FROM trading.Account WHERE AccountId = @AccountId)
        THROW 50002, N'Unknown accountId. Use find_accounts to look it up.', 1;
    IF @Ticker IS NOT NULL AND NOT EXISTS (SELECT 1 FROM trading.Instrument WHERE Ticker = @Ticker)
        THROW 50001, N'Unknown ticker. Use search_instruments to find the right one.', 1;

    SELECT t.TradeId, t.TradeTimestamp, i.Ticker, RTRIM(t.Side) AS Side,
           t.Quantity, t.Price, t.Fees, t.Status
    FROM   trading.Trade t
    JOIN   trading.Instrument i ON i.InstrumentId = t.InstrumentId
    WHERE  t.AccountId = @AccountId
      AND (@From   IS NULL OR t.TradeTimestamp >= @From)
      AND (@To     IS NULL OR t.TradeTimestamp <  DATEADD(DAY, 1, @To))
      AND (@Ticker IS NULL OR i.Ticker = @Ticker)
    ORDER BY t.TradeTimestamp DESC, t.TradeId DESC
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE mcp.GetPositions
    @AccountId INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM trading.Account WHERE AccountId = @AccountId)
        THROW 50002, N'Unknown accountId. Use find_accounts to look it up.', 1;
    SELECT TOP (200)
           Ticker, InstrumentName, Sector, Currency,
           NetQuantity, AvgBuyPrice, PriceAsOf, LastPrice, MarketValue, UnrealisedPnL,
           CAST(CASE WHEN AvgBuyPrice > 0 THEN (LastPrice / AvgBuyPrice - 1) * 100 END AS DECIMAL(9,2)) AS UnrealisedPnLPct
    FROM   trading.v_PositionValuation
    WHERE  AccountId = @AccountId
    ORDER BY MarketValue DESC;
END
GO

/* One row per sector plus a TOTAL row. */
CREATE OR ALTER PROCEDURE mcp.GetPortfolioSummary
    @AccountId INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM trading.Account WHERE AccountId = @AccountId)
        THROW 50002, N'Unknown accountId. Use find_accounts to look it up.', 1;
    SELECT CASE WHEN GROUPING(Sector) = 1 THEN 'TOTAL' ELSE Sector END AS Sector,
           MAX(Currency)        AS Currency,
           COUNT(*)             AS Positions,
           SUM(MarketValue)     AS MarketValue,
           SUM(UnrealisedPnL)   AS UnrealisedPnL
    FROM   trading.v_PositionValuation
    WHERE  AccountId = @AccountId
    GROUP BY ROLLUP(Sector)
    HAVING COUNT(*) > 0
    ORDER BY GROUPING(Sector), SUM(MarketValue) DESC;
END
GO

/* The only write mcp_reader can make. It can add audit rows but never read or change them. */
CREATE OR ALTER PROCEDURE mcp.WriteAudit
    @Caller       NVARCHAR(200),
    @ToolName     NVARCHAR(100),
    @Parameters   NVARCHAR(MAX),
    @RowsReturned INT = NULL,
    @DurationMs   INT = NULL,
    @Success      BIT,
    @Error        NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT trading.AuditLog (Caller, ToolName, Parameters, RowsReturned, DurationMs, Success, Error)
    VALUES (@Caller, @ToolName, @Parameters, @RowsReturned, @DurationMs, @Success, @Error);
END
GO

-- The one thing mcp_reader may do: run these procedures.
GRANT EXECUTE ON SCHEMA::mcp TO mcp_reader;
GO

SET NOEXEC OFF;   -- re-enable execution if the safety check above skipped the script
GO
