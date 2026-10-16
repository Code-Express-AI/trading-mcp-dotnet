using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation.Builders;

public sealed class InstrumentBuilder
{
    private int? _id;
    private string? _ticker;
    private string? _name;
    private string? _sector;
    private string? _currency;
    private double? _startPrice;
    private double? _mu;
    private double? _sigma;

    public InstrumentBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public InstrumentBuilder WithTicker(string ticker)
    {
        _ticker = ticker ?? throw new ArgumentNullException(nameof(ticker));
        return this;
    }

    public InstrumentBuilder WithName(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    public InstrumentBuilder WithSector(string sector)
    {
        _sector = sector ?? throw new ArgumentNullException(nameof(sector));
        return this;
    }

    public InstrumentBuilder WithCurrency(string currency)
    {
        _currency = currency ?? throw new ArgumentNullException(nameof(currency));
        return this;
    }

    public InstrumentBuilder WithStartPrice(double startPrice)
    {
        _startPrice = startPrice;
        return this;
    }

    public InstrumentBuilder WithMu(double mu)
    {
        _mu = mu;
        return this;
    }

    public InstrumentBuilder WithSigma(double sigma)
    {
        _sigma = sigma;
        return this;
    }

    public Instrument Build()
    {
        int id;
        string ticker;
        string name;
        string sector;
        string currency;
        double startPrice;
        double mu;
        double sigma;
        Instrument instrumentToReturn;

        id = _id ?? throw new InvalidOperationException("Instrument ID has not been provided.");
        ticker = _ticker ?? throw new InvalidOperationException("Instrument ticker has not been provided.");
        name = _name ?? throw new InvalidOperationException("Instrument name has not been provided.");
        sector = _sector ?? throw new InvalidOperationException("Instrument sector has not been provided.");
        currency = _currency ?? throw new InvalidOperationException("Instrument currency has not been provided.");
        startPrice = _startPrice ?? throw new InvalidOperationException("Instrument start price has not been provided.");
        mu = _mu ?? throw new InvalidOperationException("Instrument drift has not been provided.");
        sigma = _sigma ?? throw new InvalidOperationException("Instrument volatility has not been provided.");

        instrumentToReturn = new Instrument(id, ticker, name, sector, currency, startPrice, mu, sigma);
        return instrumentToReturn;
    }
}
