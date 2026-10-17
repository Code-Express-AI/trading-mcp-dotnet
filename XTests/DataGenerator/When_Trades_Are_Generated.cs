using TradingDataGenerator.Generation.Generators;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Trades_Are_Generated
{
    [Fact]
    public void it_should_keep_trades_currency_matched_and_ordered_by_timestamp()
    {
        TradesGenerator tradesGenerator;
        Account account;
        List<Instrument> instruments;
        List<DateTime> tradingDays;
        Dictionary<(int InstrumentId, DateTime TradingDate), decimal> closingPrices;
        List<Trade> generatedTrades;

        tradesGenerator = new TradesGenerator();
        account = new Account(1, 1, "ACC12345678", "Brokerage", "USD", new DateTime(2025, 1, 6));
        instruments =
        [
            new Instrument(1, "USDX", "USD Example", "Technology", "USD", 100, 0.07, 0.2),
            new Instrument(2, "GBPX", "GBP Example", "Financials", "GBP", 100, 0.06, 0.2)
        ];
        tradingDays =
        [
            new DateTime(2025, 1, 6),
            new DateTime(2025, 1, 7),
            new DateTime(2025, 1, 8),
            new DateTime(2025, 1, 9),
            new DateTime(2025, 1, 10)
        ];
        closingPrices = new Dictionary<(int InstrumentId, DateTime TradingDate), decimal>();
        foreach (var tradingDay in tradingDays)
        {
            closingPrices[(1, tradingDay)] = 100m;
            closingPrices[(2, tradingDay)] = 100m;
        }

        generatedTrades = tradesGenerator.GenerateTrades(
            new Random(789), [account], instruments, closingPrices, tradingDays, 10);

        Assert.NotEmpty(generatedTrades);
        Assert.Equal(
            generatedTrades.OrderBy(trade => trade.Timestamp),
            generatedTrades);
        Assert.All(generatedTrades, trade =>
        {
            Assert.Equal(account.Id, trade.AccountId);
            Assert.Equal(1, trade.InstrumentId);
            Assert.InRange(trade.Price, 99.80m, 100.20m);
            Assert.True(trade.Quantity > 0);
        });
    }

    [Fact]
    public void it_should_never_sell_more_shares_than_the_account_owns()
    {
        TradesGenerator tradesGenerator;
        Account account;
        Instrument instrument;
        List<DateTime> tradingDays;
        Dictionary<(int InstrumentId, DateTime TradingDate), decimal> closingPrices;
        List<Trade> generatedTrades;
        decimal ownedShareQuantity;

        tradesGenerator = new TradesGenerator();
        account = new Account(1, 1, "ACC12345678", "Brokerage", "USD", new DateTime(2025, 1, 6));
        instrument = new Instrument(1, "EXMP", "Example Holdings", "Financials", "USD", 100, 0.07, 0.2);
        tradingDays =
        [
            new DateTime(2025, 1, 6),
            new DateTime(2025, 1, 7),
            new DateTime(2025, 1, 8),
            new DateTime(2025, 1, 9),
            new DateTime(2025, 1, 10)
        ];
        closingPrices = tradingDays.ToDictionary(
            tradingDay => (instrument.Id, tradingDay),
            _ => 100m);
        generatedTrades = tradesGenerator.GenerateTrades(
            new Random(789), [account], [instrument], closingPrices, tradingDays, 20);
        ownedShareQuantity = 0;

        foreach (var trade in generatedTrades)
        {
            if (trade.Side == "BUY")
            {
                ownedShareQuantity += trade.Quantity;
            }
            else
            {
                Assert.True(trade.Quantity <= ownedShareQuantity);
                ownedShareQuantity -= trade.Quantity;
            }
        }

        Assert.True(ownedShareQuantity >= 0);
    }
}
