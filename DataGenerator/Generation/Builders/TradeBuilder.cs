using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation.Builders;

public sealed class TradeBuilder
{
    private int? _accountId;
    private int? _instrumentId;
    private string? _side;
    private decimal? _quantity;
    private decimal? _price;
    private decimal? _fees;
    private DateTime? _timestamp;
    private string? _status;

    public TradeBuilder WithAccountId(int accountId)
    {
        _accountId = accountId;
        return this;
    }

    public TradeBuilder WithInstrumentId(int instrumentId)
    {
        _instrumentId = instrumentId;
        return this;
    }

    public TradeBuilder WithSide(string side)
    {
        _side = side ?? throw new ArgumentNullException(nameof(side));
        return this;
    }

    public TradeBuilder WithQuantity(decimal quantity)
    {
        _quantity = quantity;
        return this;
    }

    public TradeBuilder WithPrice(decimal price)
    {
        _price = price;
        return this;
    }

    public TradeBuilder WithFees(decimal fees)
    {
        _fees = fees;
        return this;
    }

    public TradeBuilder WithTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    public TradeBuilder WithStatus(string status)
    {
        _status = status ?? throw new ArgumentNullException(nameof(status));
        return this;
    }

    public Trade Build()
    {
        int accountId;
        int instrumentId;
        string side;
        decimal quantity;
        decimal price;
        decimal fees;
        DateTime timestamp;
        string status;
        Trade tradeToReturn;

        accountId = _accountId ?? throw new InvalidOperationException("Trade account ID has not been provided.");
        instrumentId = _instrumentId ?? throw new InvalidOperationException("Trade instrument ID has not been provided.");
        side = _side ?? throw new InvalidOperationException("Trade side has not been provided.");
        quantity = _quantity ?? throw new InvalidOperationException("Trade quantity has not been provided.");
        price = _price ?? throw new InvalidOperationException("Trade price has not been provided.");
        fees = _fees ?? throw new InvalidOperationException("Trade fees have not been provided.");
        timestamp = _timestamp ?? throw new InvalidOperationException("Trade timestamp has not been provided.");
        status = _status ?? throw new InvalidOperationException("Trade status has not been provided.");

        tradeToReturn = new Trade(accountId, instrumentId, side, quantity, price, fees, timestamp, status);
        return tradeToReturn;
    }
}
