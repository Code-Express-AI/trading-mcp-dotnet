using TradingDataGenerator.Generation.Builders;
using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation.Generators;

public sealed class TradesGenerator : ITradesGenerator
{
    /// <summary>
    /// Simulates accounts chronologically. An account only trades instruments in its base currency,
    /// and it can only sell shares that it has already bought.
    /// </summary>
    public List<Trade> GenerateTrades(
        Random randomNumberGenerator,
        List<Account> generatedAccounts,
        List<Instrument> generatedInstruments,
        Dictionary<(int InstrumentId, DateTime TradingDate), decimal> closingPricesByInstrumentAndDate,
        List<DateTime> tradingDays,
        int targetTradesPerAccount)
    {
        Dictionary<string, List<Instrument>> instrumentsByCurrency;
        DateTime lastTradingDay;
        List<Trade> generatedTrades;
        List<Trade> tradesOrderedByTimestamp;

        instrumentsByCurrency = GroupInstrumentsByCurrency(generatedInstruments);
        lastTradingDay = tradingDays[^1];
        generatedTrades = new List<Trade>();

        foreach (var account in generatedAccounts)
        {
            AccountTradingState accountTradingState;
            accountTradingState = new AccountTradingState(
                account,
                instrumentsByCurrency[account.BaseCurrency],
                closingPricesByInstrumentAndDate,
                lastTradingDay);
            generatedTrades.AddRange(GenerateTradesForAccount(
                randomNumberGenerator,
                accountTradingState,
                tradingDays,
                targetTradesPerAccount));
        }

        tradesOrderedByTimestamp = generatedTrades.OrderBy(generatedTrade =>
        {
            return generatedTrade.Timestamp;
        }).ToList();
        return tradesOrderedByTimestamp;
    }

    private Dictionary<string, List<Instrument>> GroupInstrumentsByCurrency(
        IEnumerable<Instrument> generatedInstruments)
    {
        Dictionary<string, List<Instrument>> instrumentsByCurrency;

        instrumentsByCurrency = generatedInstruments
            .GroupBy(instrument =>
            {
                return instrument.Currency;
            })
            .ToDictionary(
                instrumentGroup =>
                {
                    return instrumentGroup.Key;
                },
                instrumentGroup =>
                {
                    return instrumentGroup.ToList();
                });
        return instrumentsByCurrency;
    }

    private List<Trade> GenerateTradesForAccount(
        Random randomNumberGenerator,
        AccountTradingState accountTradingState,
        List<DateTime> tradingDays,
        int targetTradesPerAccount)
    {
        List<DateTime> selectedTradingDays;
        List<Trade> accountTradesToReturn;

        selectedTradingDays = SelectTradingDaysForAccount(
            randomNumberGenerator,
            tradingDays,
            accountTradingState.Account.OpenedOn,
            targetTradesPerAccount);
        accountTradesToReturn = new List<Trade>(selectedTradingDays.Count);

        foreach (var tradingDay in selectedTradingDays)
        {
            accountTradesToReturn.Add(CreateTradeForDay(
                randomNumberGenerator, accountTradingState, tradingDay));
        }

        return accountTradesToReturn;
    }

    private List<DateTime> SelectTradingDaysForAccount(
        Random randomNumberGenerator,
        List<DateTime> tradingDays,
        DateTime accountOpenedOn,
        int targetTradesPerAccount)
    {
        List<DateTime> eligibleTradingDays;
        int tradeCount;
        List<DateTime> selectedTradingDays;

        eligibleTradingDays = tradingDays.Where(tradingDay =>
        {
            return tradingDay >= accountOpenedOn;
        }).ToList();
        if (eligibleTradingDays.Count == 0)
        {
            return new List<DateTime>();
        }

        tradeCount = randomNumberGenerator.Next(
            Math.Max(3, targetTradesPerAccount / 2), targetTradesPerAccount * 2);
        selectedTradingDays = Enumerable.Range(0, tradeCount)
            .Select(ignoredIndex =>
            {
                return eligibleTradingDays[randomNumberGenerator.Next(eligibleTradingDays.Count)];
            })
            .OrderBy(tradingDay =>
            {
                return tradingDay;
            })
            .ToList();
        return selectedTradingDays;
    }

    private Trade CreateTradeForDay(
        Random randomNumberGenerator,
        AccountTradingState accountTradingState,
        DateTime tradingDay)
    {
        TradeDetails tradeDetails;
        decimal closingPrice;
        decimal tradePrice;
        decimal tradeFees;
        DateTime tradeTimestamp;
        string tradeStatus;

        tradeDetails = SelectTradeDetails(randomNumberGenerator, accountTradingState);
        closingPrice = accountTradingState.ClosingPricesByInstrumentAndDate[
            (tradeDetails.InstrumentId, tradingDay)];
        tradePrice = CalculateTradePrice(randomNumberGenerator, closingPrice);
        tradeFees = CalculateTradeFees(tradeDetails.Quantity, tradePrice);
        tradeTimestamp = CreateTradeTimestamp(randomNumberGenerator, accountTradingState, tradingDay);
        tradeStatus = GetTradeStatus(tradingDay, accountTradingState.LastTradingDay);

        return new TradeBuilder()
            .WithAccountId(accountTradingState.Account.Id)
            .WithInstrumentId(tradeDetails.InstrumentId)
            .WithSide(tradeDetails.Side)
            .WithQuantity(tradeDetails.Quantity)
            .WithPrice(tradePrice)
            .WithFees(tradeFees)
            .WithTimestamp(tradeTimestamp)
            .WithStatus(tradeStatus)
            .Build();
    }

    private TradeDetails SelectTradeDetails(
        Random randomNumberGenerator,
        AccountTradingState accountTradingState)
    {
        bool isSellTrade;
        (int InstrumentId, decimal Quantity) tradeDetails;
        string tradeSide;

        isSellTrade = accountTradingState.ShareHoldingsByInstrumentId.Count > 0
                      && randomNumberGenerator.NextDouble() < 0.35;
        if (isSellTrade)
        {
            tradeDetails = CreateSellTradeDetails(
                randomNumberGenerator, accountTradingState.ShareHoldingsByInstrumentId);
            tradeSide = "SELL";
        }
        else
        {
            tradeDetails = CreateBuyTradeDetails(
                randomNumberGenerator,
                accountTradingState.CurrencyMatchedInstruments,
                accountTradingState.ShareHoldingsByInstrumentId);
            tradeSide = "BUY";
        }

        return new TradeDetails(tradeDetails.InstrumentId, tradeDetails.Quantity, tradeSide);
    }

    private (int InstrumentId, decimal Quantity) CreateBuyTradeDetails(
        Random randomNumberGenerator,
        List<Instrument> currencyMatchedInstruments,
        Dictionary<int, decimal> shareHoldingsByInstrumentId)
    {
        int instrumentId;
        decimal shareQuantity;

        instrumentId = currencyMatchedInstruments[randomNumberGenerator.Next(currencyMatchedInstruments.Count)].Id;
        shareQuantity = randomNumberGenerator.Next(1, 51) * 10;
        shareHoldingsByInstrumentId[instrumentId] =
            shareHoldingsByInstrumentId.GetValueOrDefault(instrumentId) + shareQuantity;
        return (instrumentId, shareQuantity);
    }

    private (int InstrumentId, decimal Quantity) CreateSellTradeDetails(
        Random randomNumberGenerator,
        Dictionary<int, decimal> shareHoldingsByInstrumentId)
    {
        List<int> heldInstrumentIds;
        int instrumentId;
        decimal ownedShareQuantity;
        decimal sharesToSell;

        heldInstrumentIds = shareHoldingsByInstrumentId.Keys.ToList();
        instrumentId = heldInstrumentIds[randomNumberGenerator.Next(heldInstrumentIds.Count)];
        ownedShareQuantity = shareHoldingsByInstrumentId[instrumentId];
        sharesToSell = randomNumberGenerator.NextDouble() < 0.5
            ? ownedShareQuantity
            : Math.Max(1m, Math.Floor(ownedShareQuantity
                                     * (decimal)(0.2 + randomNumberGenerator.NextDouble() * 0.6)));
        shareHoldingsByInstrumentId[instrumentId] = ownedShareQuantity - sharesToSell;
        if (shareHoldingsByInstrumentId[instrumentId] == 0)
        {
            shareHoldingsByInstrumentId.Remove(instrumentId);
        }

        return (instrumentId, sharesToSell);
    }

    private decimal CalculateTradePrice(Random randomNumberGenerator, decimal closingPrice)
    {
        decimal tradePrice;

        tradePrice = Math.Round(
            closingPrice * (decimal)(1 + (randomNumberGenerator.NextDouble() - 0.5) * 0.004), 2);
        return tradePrice;
    }

    private decimal CalculateTradeFees(decimal tradeQuantity, decimal tradePrice)
    {
        decimal tradeFees;

        tradeFees = Math.Round(Math.Max(1.00m, tradeQuantity * tradePrice * 0.0005m), 2);
        return tradeFees;
    }

    private DateTime CreateTradeTimestamp(Random randomNumberGenerator, DateTime tradingDay)
    {
        DateTime tradeTimestamp;

        tradeTimestamp = tradingDay.AddHours(9.5)
            .AddMinutes(randomNumberGenerator.Next(0, 390))
            .AddSeconds(randomNumberGenerator.Next(0, 60));
        return tradeTimestamp;
    }

    private DateTime CreateTradeTimestamp(
        Random randomNumberGenerator,
        AccountTradingState accountTradingState,
        DateTime tradingDay)
    {
        DateTime tradeTimestamp;

        tradeTimestamp = CreateTradeTimestamp(randomNumberGenerator, tradingDay);
        if (accountTradingState.LastTradeTimestamp is { } previousTradeTimestamp
            && tradeTimestamp < previousTradeTimestamp)
        {
            tradeTimestamp = previousTradeTimestamp;
        }

        accountTradingState.LastTradeTimestamp = tradeTimestamp;
        return tradeTimestamp;
    }

    private string GetTradeStatus(DateTime tradingDay, DateTime lastTradingDay)
    {
        string tradeStatus;

        tradeStatus = tradingDay >= lastTradingDay.AddDays(-2) ? "PENDING" : "SETTLED";
        return tradeStatus;
    }

    private sealed class AccountTradingState
    {
        public AccountTradingState(
            Account account,
            List<Instrument> currencyMatchedInstruments,
            Dictionary<(int InstrumentId, DateTime TradingDate), decimal> closingPricesByInstrumentAndDate,
            DateTime lastTradingDay)
        {
            Account = account;
            CurrencyMatchedInstruments = currencyMatchedInstruments;
            ClosingPricesByInstrumentAndDate = closingPricesByInstrumentAndDate;
            LastTradingDay = lastTradingDay;
        }

        public Account Account { get; }
        public List<Instrument> CurrencyMatchedInstruments { get; }
        public Dictionary<(int InstrumentId, DateTime TradingDate), decimal> ClosingPricesByInstrumentAndDate { get; }
        public DateTime LastTradingDay { get; }
        public Dictionary<int, decimal> ShareHoldingsByInstrumentId { get; } = new();
        public DateTime? LastTradeTimestamp { get; set; }
    }

    private sealed record TradeDetails(int InstrumentId, decimal Quantity, string Side);
}
