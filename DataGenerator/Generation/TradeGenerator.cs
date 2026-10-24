using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation;

// Simulates each account's history in date order. An account only trades instruments in its base currency,
// and it can only sell shares it already holds, so positions never go negative.
internal static class TradeGenerator
{
    private const string Buy = "BUY";
    private const string Sell = "SELL";

    public static List<Trade> Generate(
        Random random,
        IReadOnlyList<Account> accounts,
        IReadOnlyList<Instrument> instruments,
        IReadOnlyList<InstrumentPrice> prices,
        IReadOnlyList<DateTime> tradingDays,
        int averageTradesPerAccount)
    {
        var instrumentsByCurrency = instruments.GroupBy(instrument => instrument.Currency)
            .ToDictionary(group => group.Key, group => group.ToList());
        var pricesByInstrumentAndDay = prices.ToDictionary(price => (price.InstrumentId, price.PriceDate));
        var trades = new List<Trade>();

        foreach (var account in accounts)
        {
            trades.AddRange(GenerateForAccount(
                random, account, instrumentsByCurrency[account.BaseCurrency], pricesByInstrumentAndDay, tradingDays, averageTradesPerAccount));
        }

        return trades.OrderBy(trade => trade.Timestamp).ToList();
    }

    private static List<Trade> GenerateForAccount(
        Random random,
        Account account,
        List<Instrument> tradableInstruments,
        Dictionary<(int InstrumentId, DateTime PriceDate), InstrumentPrice> pricesByInstrumentAndDay,
        IReadOnlyList<DateTime> tradingDays,
        int averageTradesPerAccount)
    {
        var daysSinceOpening = tradingDays.Where(day => day >= account.OpenedOn).ToList();
        if (daysSinceOpening.Count == 0)
        {
            return [];
        }

        // Between half and double the average (and at least three), so some accounts are busier than others.
        var tradeCount = random.Next(Math.Max(3, averageTradesPerAccount / 2), averageTradesPerAccount * 2);
        var tradeDays = Enumerable.Range(0, tradeCount).Select(_ => random.Pick(daysSinceOpening)).Order().ToList();
        var holdings = new Dictionary<int, decimal>();
        var trades = new List<Trade>(tradeCount);
        var previousTimestamp = DateTime.MinValue;

        foreach (var tradeDay in tradeDays)
        {
            var (instrumentId, side, quantity) = ChooseOrder(random, tradableInstruments, holdings);
            var price = CreateTradePrice(random, pricesByInstrumentAndDay[(instrumentId, tradeDay)]);

            // An account can trade more than once a day, so keep its trades in time order.
            var timestamp = CreateTimestamp(random, tradeDay);
            if (timestamp < previousTimestamp)
            {
                timestamp = previousTimestamp;
            }

            previousTimestamp = timestamp;
            trades.Add(new Trade(
                AccountId: account.Id,
                InstrumentId: instrumentId,
                Side: side,
                Quantity: quantity,
                Price: price,
                Fees: CalculateFees(quantity, price),
                Timestamp: timestamp,
                Status: GetStatus(tradeDay, tradingDays[^1])));
        }

        return trades;
    }

    // About a third of trades are sells, once the account holds something to sell.
    private static (int InstrumentId, string Side, decimal Quantity) ChooseOrder(
        Random random, List<Instrument> tradableInstruments, Dictionary<int, decimal> holdings)
    {
        var isSell = holdings.Count > 0 && random.NextDouble() < 0.35;
        return isSell ? CreateSellOrder(random, holdings) : CreateBuyOrder(random, tradableInstruments, holdings);
    }

    // Buys are in round lots of 10 to 500 shares.
    private static (int InstrumentId, string Side, decimal Quantity) CreateBuyOrder(
        Random random, List<Instrument> tradableInstruments, Dictionary<int, decimal> holdings)
    {
        var instrumentId = random.Pick(tradableInstruments).Id;
        var quantity = random.Next(1, 51) * 10m;
        holdings[instrumentId] = holdings.GetValueOrDefault(instrumentId) + quantity;
        return (instrumentId, Buy, quantity);
    }

    // Half of sells close the whole position; the rest sell between 20% and 80% of it.
    private static (int InstrumentId, string Side, decimal Quantity) CreateSellOrder(
        Random random, Dictionary<int, decimal> holdings)
    {
        var instrumentId = random.Pick(holdings.Keys.ToList());
        var heldQuantity = holdings[instrumentId];
        var quantity = random.NextDouble() < 0.5
            ? heldQuantity
            : Math.Max(1m, Math.Floor(heldQuantity * (decimal)(0.2 + random.NextDouble() * 0.6)));

        holdings[instrumentId] = heldQuantity - quantity;
        if (holdings[instrumentId] == 0)
        {
            holdings.Remove(instrumentId);
        }

        return (instrumentId, Sell, quantity);
    }

    // Filled within ±0.2% of the day's close, but never outside that day's low-high range.
    private static decimal CreateTradePrice(Random random, InstrumentPrice instrumentPrice)
    {
        var price = Math.Round(instrumentPrice.Close * (decimal)(1 + (random.NextDouble() - 0.5) * 0.004), 2);
        return Math.Clamp(price, instrumentPrice.Low, instrumentPrice.High);
    }

    // Fees are 5 basis points (0.05%) of the trade value, with a minimum of 1.00.
    private static decimal CalculateFees(decimal quantity, decimal price) =>
        Math.Round(Math.Max(1.00m, quantity * price * 0.0005m), 2);

    // Some time during market hours, 09:30 to 16:00.
    private static DateTime CreateTimestamp(Random random, DateTime tradeDay) =>
        tradeDay.AddHours(9.5).AddMinutes(random.Next(0, 390)).AddSeconds(random.Next(0, 60));

    // Trades from the last couple of days haven't settled yet.
    private static string GetStatus(DateTime tradeDay, DateTime lastTradingDay) =>
        tradeDay >= lastTradingDay.AddDays(-2) ? "PENDING" : "SETTLED";
}
