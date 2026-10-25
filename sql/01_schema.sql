/* Tables and views for the trading demo. Needs SQL Server 2022+ or Azure SQL Database (for the ledger table). */

/* Safety check: never create objects in a system database. */
IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
BEGIN
    RAISERROR(N'You are connected to a system database. Switch to the TradingDemo database first, then run this script again.', 16, 1);
    SET NOEXEC ON;   -- skip everything below
END
GO

CREATE SCHEMA trading;
GO

/* Every table with an IDENTITY key also has a random GUID key (<Name>Key). The integers are fast for joins
   and indexes, but they never leave the database: sequential numbers give away how many rows exist and let
   anyone holding a token walk through them one by one. A procedure returns a key only when another tool
   needs it as input. NEWID(), not NEWSEQUENTIALID(), so a key reveals nothing about its neighbours. */

CREATE TABLE trading.Instrument (
    InstrumentId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Instrument PRIMARY KEY,
    InstrumentKey UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Instrument_Key DEFAULT (NEWID()) CONSTRAINT UQ_Instrument_Key UNIQUE,
    Ticker        NVARCHAR(10)  NOT NULL CONSTRAINT UQ_Instrument_Ticker UNIQUE,
    Name          NVARCHAR(100) NOT NULL,
    Sector        NVARCHAR(50)  NOT NULL,
    Currency      CHAR(3)       NOT NULL
);

/* Daily prices. The primary key doubles as the index for "latest price" lookups.
   The columns are OpenPrice and ClosePrice because OPEN and CLOSE are reserved words. */
CREATE TABLE trading.InstrumentPrice (
    InstrumentId  INT           NOT NULL CONSTRAINT FK_InstrumentPrice_Instrument REFERENCES trading.Instrument(InstrumentId),
    PriceDate     DATE          NOT NULL,
    OpenPrice     DECIMAL(18,4) NOT NULL,
    HighPrice     DECIMAL(18,4) NOT NULL,
    LowPrice      DECIMAL(18,4) NOT NULL,
    ClosePrice    DECIMAL(18,4) NOT NULL,
    Volume        BIGINT        NOT NULL,
    CONSTRAINT PK_InstrumentPrice PRIMARY KEY (InstrumentId, PriceDate),
    CONSTRAINT CK_InstrumentPrice_Range  CHECK (HighPrice >= LowPrice
                                   AND HighPrice >= OpenPrice AND HighPrice >= ClosePrice
                                   AND LowPrice  <= OpenPrice AND LowPrice  <= ClosePrice),
    CONSTRAINT CK_InstrumentPrice_Positive CHECK (LowPrice > 0 AND Volume >= 0)
);

CREATE TABLE trading.Client (
    ClientId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Client PRIMARY KEY,
    ClientKey    UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Client_Key DEFAULT (NEWID()) CONSTRAINT UQ_Client_Key UNIQUE,
    FullName     NVARCHAR(100) NOT NULL,
    Email        NVARCHAR(200) MASKED WITH (FUNCTION = 'email()') NOT NULL,   -- masked for users without UNMASK
    Country      NVARCHAR(50)  NOT NULL,
    RiskProfile  NVARCHAR(20)  NOT NULL
        CONSTRAINT CK_Client_Risk CHECK (RiskProfile IN ('Conservative','Balanced','Growth','Aggressive')),
    CreatedOn    DATE          NOT NULL
);

CREATE TABLE trading.Account (
    AccountId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Account PRIMARY KEY,
    AccountKey     UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Account_Key DEFAULT (NEWID()) CONSTRAINT UQ_Account_Key UNIQUE,
    ClientId       INT           NOT NULL CONSTRAINT FK_Account_Client REFERENCES trading.Client(ClientId),
    AccountNumber  NVARCHAR(20)  MASKED WITH (FUNCTION = 'partial(3,"XXXXX",3)') NOT NULL
        CONSTRAINT UQ_Account_Number UNIQUE,
    AccountType    NVARCHAR(20)  NOT NULL
        CONSTRAINT CK_Account_Type CHECK (AccountType IN ('Brokerage','Retirement','Joint')),
    BaseCurrency   CHAR(3)       NOT NULL,
    OpenedOn       DATE          NOT NULL
);
CREATE INDEX IX_Account_ClientId ON trading.Account(ClientId);

CREATE TABLE trading.Trade (
    TradeId         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Trade PRIMARY KEY,
    TradeKey        UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Trade_Key DEFAULT (NEWID()) CONSTRAINT UQ_Trade_Key UNIQUE,
    AccountId       INT           NOT NULL CONSTRAINT FK_Trade_Account    REFERENCES trading.Account(AccountId),
    InstrumentId    INT           NOT NULL CONSTRAINT FK_Trade_Instrument REFERENCES trading.Instrument(InstrumentId),
    Side            CHAR(4)       NOT NULL CONSTRAINT CK_Trade_Side   CHECK (Side IN ('BUY','SELL')),
    Quantity        DECIMAL(18,4) NOT NULL CONSTRAINT CK_Trade_Qty    CHECK (Quantity > 0),
    Price           DECIMAL(18,4) NOT NULL CONSTRAINT CK_Trade_Price  CHECK (Price > 0),
    Fees            DECIMAL(18,2) NOT NULL CONSTRAINT DF_Trade_Fees DEFAULT (0),
    TradeTimestamp  DATETIME2(0)  NOT NULL,
    Status          NVARCHAR(10)  NOT NULL CONSTRAINT DF_Trade_Status DEFAULT ('SETTLED')
        CONSTRAINT CK_Trade_Status CHECK (Status IN ('PENDING','SETTLED','CANCELLED'))
);
-- Typical access paths: "trades for an account in a date range", "trades in an instrument"
CREATE INDEX IX_Trade_Account_Time ON trading.Trade(AccountId, TradeTimestamp DESC)
    INCLUDE (InstrumentId, Side, Quantity, Price, Fees, Status);
CREATE INDEX IX_Trade_Instrument_Time ON trading.Trade(InstrumentId, TradeTimestamp DESC);

/* Written by the MCP server, one row per tool call.
   An append-only ledger table: rows can be added but never updated or deleted, not even by sa.
   SQL Server also chains a hash through every row, so tampering with the data files is detected
   by sys.sp_verify_database_ledger. */

CREATE TABLE trading.AuditLog (
    AuditId        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLog PRIMARY KEY,
    AuditKey       UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_AuditLog_Key DEFAULT (NEWID()) CONSTRAINT UQ_AuditLog_Key UNIQUE,
    OccurredAtUtc  DATETIME2(3)  NOT NULL CONSTRAINT DF_AuditLog_Time DEFAULT (SYSUTCDATETIME()),
    Caller         NVARCHAR(200) NULL,        -- token subject / client id
    ToolName       NVARCHAR(100) NOT NULL,
    Parameters     NVARCHAR(MAX) NULL,        -- JSON of the arguments
    RowsReturned   INT           NULL,
    DurationMs     INT           NULL,
    Success        BIT           NOT NULL,
    Error          NVARCHAR(500) NULL
) WITH (LEDGER = ON (APPEND_ONLY = ON));
GO

/* Positions are derived from trades, so nothing is stored twice.
   AvgBuyPrice is simply the average of all buy prices; real systems track tax lots or FIFO. */
CREATE VIEW trading.v_Position AS
SELECT  t.AccountId,
        t.InstrumentId,
        SUM(CASE WHEN t.Side = 'BUY' THEN t.Quantity ELSE -t.Quantity END) AS NetQuantity,
        SUM(CASE WHEN t.Side = 'BUY' THEN t.Quantity * t.Price ELSE 0 END)
          / NULLIF(SUM(CASE WHEN t.Side = 'BUY' THEN t.Quantity ELSE 0 END), 0) AS AvgBuyPrice
FROM    trading.Trade t
WHERE   t.Status <> 'CANCELLED'
GROUP BY t.AccountId, t.InstrumentId
HAVING  SUM(CASE WHEN t.Side = 'BUY' THEN t.Quantity ELSE -t.Quantity END) <> 0;
GO

/* Positions valued at the latest available close. */
CREATE VIEW trading.v_PositionValuation AS
SELECT  a.AccountId,
        a.AccountNumber,
        c.ClientId,
        c.FullName,
        i.Ticker,
        i.Name            AS InstrumentName,
        i.Sector,
        i.Currency,
        p.NetQuantity,
        p.AvgBuyPrice,
        lp.PriceDate      AS PriceAsOf,
        lp.ClosePrice     AS LastPrice,
        p.NetQuantity * lp.ClosePrice                    AS MarketValue,
        p.NetQuantity * (lp.ClosePrice - p.AvgBuyPrice)  AS UnrealisedPnL
FROM    trading.v_Position p
JOIN    trading.Account    a ON a.AccountId    = p.AccountId
JOIN    trading.Client     c ON c.ClientId     = a.ClientId
JOIN    trading.Instrument i ON i.InstrumentId = p.InstrumentId
OUTER APPLY (
        SELECT TOP (1) pr.PriceDate, pr.ClosePrice
        FROM   trading.InstrumentPrice pr
        WHERE  pr.InstrumentId = p.InstrumentId
        ORDER BY pr.PriceDate DESC
) lp;
GO

SET NOEXEC OFF;   -- re-enable execution if the safety check above skipped the script
GO
