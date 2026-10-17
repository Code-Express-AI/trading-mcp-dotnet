using TradingMcp.Model;

namespace XTests.McpServer;

public class When_Models_Are_Created
{
    [Fact]
    public void dataset_info_should_expose_its_constructor_values()
    {
        var firstPriceDate = new DateTime(2025, 1, 2);
        var lastPriceDate = new DateTime(2025, 12, 31);
        var model = new DatasetInfo(firstPriceDate, lastPriceDate, 12, 30, 450);

        Assert.Equal(firstPriceDate, model.FirstPriceDate);
        Assert.Equal(lastPriceDate, model.LastPriceDate);
        Assert.Equal(12, model.Instruments);
        Assert.Equal(30, model.Accounts);
        Assert.Equal(450, model.Trades);
    }

    [Fact]
    public void instrument_should_expose_its_constructor_values()
    {
        var model = new Instrument("EXMP", "Example Holdings", "Financials", "USD");

        Assert.Equal("EXMP", model.Ticker);
        Assert.Equal("Example Holdings", model.Name);
        Assert.Equal("Financials", model.Sector);
        Assert.Equal("USD", model.Currency);
    }

    [Fact]
    public void price_should_expose_its_constructor_values()
    {
        var priceDate = new DateTime(2025, 1, 2);
        var model = new Price(priceDate, 100.25m, 105.50m, 98.75m, 103.10m, 250_000);

        Assert.Equal(priceDate, model.PriceDate);
        Assert.Equal(100.25m, model.Open);
        Assert.Equal(105.50m, model.High);
        Assert.Equal(98.75m, model.Low);
        Assert.Equal(103.10m, model.Close);
        Assert.Equal(250_000, model.Volume);
    }

    [Fact]
    public void mover_should_expose_its_constructor_values()
    {
        var priceDate = new DateTime(2025, 1, 2);
        var model = new Mover("EXMP", "Example Holdings", priceDate, 103.10m, 100m, 3.1m);

        Assert.Equal("EXMP", model.Ticker);
        Assert.Equal("Example Holdings", model.Name);
        Assert.Equal(priceDate, model.PriceDate);
        Assert.Equal(103.10m, model.ClosePrice);
        Assert.Equal(100m, model.PrevClose);
        Assert.Equal(3.1m, model.ChangePct);
    }

    [Fact]
    public void account_should_expose_its_constructor_values()
    {
        var openedOn = new DateTime(2024, 5, 6);
        var model = new Account(42, "****1234", "Avery Example", "GB", "Balanced", "Brokerage", "GBP", openedOn);

        Assert.Equal(42, model.AccountId);
        Assert.Equal("****1234", model.AccountNumber);
        Assert.Equal("Avery Example", model.ClientName);
        Assert.Equal("GB", model.Country);
        Assert.Equal("Balanced", model.RiskProfile);
        Assert.Equal("Brokerage", model.AccountType);
        Assert.Equal("GBP", model.BaseCurrency);
        Assert.Equal(openedOn, model.OpenedOn);
    }

    [Fact]
    public void trade_should_expose_its_constructor_values()
    {
        var timestamp = new DateTime(2025, 1, 2, 10, 30, 0);
        var model = new Trade(123, timestamp, "EXMP", "BUY", 5m, 100.25m, 1.50m, "SETTLED");

        Assert.Equal(123, model.TradeId);
        Assert.Equal(timestamp, model.TradeTimestamp);
        Assert.Equal("EXMP", model.Ticker);
        Assert.Equal("BUY", model.Side);
        Assert.Equal(5m, model.Quantity);
        Assert.Equal(100.25m, model.Price);
        Assert.Equal(1.50m, model.Fees);
        Assert.Equal("SETTLED", model.Status);
    }

    [Fact]
    public void position_should_expose_its_constructor_values()
    {
        var priceAsOf = new DateTime(2025, 1, 2);
        var model = new Position(
            "EXMP",
            "Example Holdings",
            "Financials",
            "USD",
            10m,
            90m,
            priceAsOf,
            100m,
            1000m,
            100m,
            11.11m);

        Assert.Equal("EXMP", model.Ticker);
        Assert.Equal("Example Holdings", model.InstrumentName);
        Assert.Equal("Financials", model.Sector);
        Assert.Equal("USD", model.Currency);
        Assert.Equal(10m, model.NetQuantity);
        Assert.Equal(90m, model.AvgBuyPrice);
        Assert.Equal(priceAsOf, model.PriceAsOf);
        Assert.Equal(100m, model.LastPrice);
        Assert.Equal(1000m, model.MarketValue);
        Assert.Equal(100m, model.UnrealisedPnL);
        Assert.Equal(11.11m, model.UnrealisedPnLPct);
    }

    [Fact]
    public void position_should_allow_missing_optional_profit_values()
    {
        var model = new Position(
            "EXMP",
            "Example Holdings",
            "Financials",
            "USD",
            10m,
            null,
            DateTime.UnixEpoch,
            100m,
            1000m,
            0m,
            null);

        Assert.Null(model.AvgBuyPrice);
        Assert.Null(model.UnrealisedPnLPct);
    }

    [Fact]
    public void sector_summary_should_expose_its_constructor_values()
    {
        var model = new SectorSummary("Financials", "USD", 4, 12_500m, 750m);

        Assert.Equal("Financials", model.Sector);
        Assert.Equal("USD", model.Currency);
        Assert.Equal(4, model.Positions);
        Assert.Equal(12_500m, model.MarketValue);
        Assert.Equal(750m, model.UnrealisedPnL);
    }
}
