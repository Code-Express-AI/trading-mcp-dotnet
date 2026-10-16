using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation.Builders;

public sealed class PriceBarBuilder
{
    private int? _instrumentId;
    private DateTime? _priceDate;
    private decimal? _open;
    private decimal? _high;
    private decimal? _low;
    private decimal? _close;
    private long? _volume;

    public PriceBarBuilder WithInstrumentId(int instrumentId)
    {
        _instrumentId = instrumentId;
        return this;
    }

    public PriceBarBuilder WithPriceDate(DateTime priceDate)
    {
        _priceDate = priceDate;
        return this;
    }

    public PriceBarBuilder WithOpen(decimal open)
    {
        _open = open;
        return this;
    }

    public PriceBarBuilder WithHigh(decimal high)
    {
        _high = high;
        return this;
    }

    public PriceBarBuilder WithLow(decimal low)
    {
        _low = low;
        return this;
    }

    public PriceBarBuilder WithClose(decimal close)
    {
        _close = close;
        return this;
    }

    public PriceBarBuilder WithVolume(long volume)
    {
        _volume = volume;
        return this;
    }

    public PriceBar Build()
    {
        int instrumentId;
        DateTime priceDate;
        decimal open;
        decimal high;
        decimal low;
        decimal close;
        long volume;
        PriceBar priceBarToReturn;

        instrumentId = _instrumentId ?? throw new InvalidOperationException("Price bar instrument ID has not been provided.");
        priceDate = _priceDate ?? throw new InvalidOperationException("Price bar date has not been provided.");
        open = _open ?? throw new InvalidOperationException("Price bar open price has not been provided.");
        high = _high ?? throw new InvalidOperationException("Price bar high price has not been provided.");
        low = _low ?? throw new InvalidOperationException("Price bar low price has not been provided.");
        close = _close ?? throw new InvalidOperationException("Price bar close price has not been provided.");
        volume = _volume ?? throw new InvalidOperationException("Price bar volume has not been provided.");

        priceBarToReturn = new PriceBar(instrumentId, priceDate, open, high, low, close, volume);
        return priceBarToReturn;
    }
}
