using TradingDataGenerator.Generation;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Trades_Are_Generated
{
    private static readonly List<DateTime> TradingDays = TradingCalendar.GetTradingDays(new DateTime(2025, 1, 6), new DateTime(2025, 3, 31));
    private static readonly Account UsdAccount = new(1, 1, "ACC12345678", "Brokerage", "USD", TradingDays[0]);
    private static readonly Instrument UsdInstrument = new(1, "USDX", "USD Example", "Technology", "USD", 100, 0.07, 0.2);
    private static readonly Instrument GbpInstrument = new(2, "GBPX", "GBP Example", "Financials", "GBP", 100, 0.06, 0.2);

    private static List<Trade> GenerateTrades(int averageTradesPerAccount = 20)
    {
        List<Instrument> instruments = [UsdInstrument, GbpInstrument];
        var prices = PriceGenerator.Generate(new Random(456), instruments, TradingDays);
        return TradeGenerator.Generate(new Random(789), [UsdAccount], instruments, prices, TradingDays, averageTradesPerAccount);
    }

    [Fact]
    public void it_should_only_trade_instruments_in_the_account_currency()
    {
        var trades = GenerateTrades();

        Assert.NotEmpty(trades);
        Assert.All(trades, trade =>
        {
            Assert.Equal(UsdAccount.Id, trade.AccountId);
            Assert.Equal(UsdInstrument.Id, trade.InstrumentId);
        });
    }

    [Fact]
    public void it_should_order_trades_by_timestamp_during_market_hours()
    {
        var trades = GenerateTrades();

        Assert.Equal(trades.OrderBy(trade => trade.Timestamp), trades);
        Assert.All(trades, trade => Assert.InRange(trade.Timestamp.TimeOfDay, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0)));
    }

    [Fact]
    public void it_should_price_every_trade_within_that_day_s_low_and_high()
    {
        var instruments = new List<Instrument> { UsdInstrument };
        var prices = PriceGenerator.Generate(new Random(456), instruments, TradingDays);
        var pricesByDay = prices.ToDictionary(price => price.PriceDate);

        var trades = TradeGenerator.Generate(new Random(789), [UsdAccount], instruments, prices, TradingDays, 100);

        Assert.All(trades, trade =>
        {
            var priceBar = pricesByDay[trade.Timestamp.Date];
            Assert.InRange(trade.Price, priceBar.Low, priceBar.High);
        });
    }

    [Fact]
    public void it_should_never_sell_more_shares_than_the_account_holds()
    {
        var holding = 0m;

        foreach (var trade in GenerateTrades(averageTradesPerAccount: 100))
        {
            Assert.True(trade.Quantity > 0);
            if (trade.Side == "SELL")
            {
                Assert.True(trade.Quantity <= holding, $"Sold {trade.Quantity} while holding {holding}.");
                holding -= trade.Quantity;
            }
            else
            {
                holding += trade.Quantity;
            }
        }
    }

    [Fact]
    public void it_should_charge_at_least_the_minimum_fee()
    {
        Assert.All(GenerateTrades(), trade => Assert.True(trade.Fees >= 1.00m));
    }
}
