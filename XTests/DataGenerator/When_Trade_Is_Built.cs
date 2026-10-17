using TradingDataGenerator.Generation.Builders;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Trade_Is_Built
{
    [Fact]
    public void it_should_create_a_trade_with_the_supplied_values()
    {
        DateTime timestamp;
        Trade trade;

        timestamp = new DateTime(2025, 1, 15, 10, 30, 45);
        trade = new TradeBuilder()
            .WithAccountId(8)
            .WithInstrumentId(4)
            .WithSide("BUY")
            .WithQuantity(50)
            .WithPrice(125.50m)
            .WithFees(3.14m)
            .WithTimestamp(timestamp)
            .WithStatus("SETTLED")
            .Build();

        Assert.Equal(8, trade.AccountId);
        Assert.Equal(4, trade.InstrumentId);
        Assert.Equal("BUY", trade.Side);
        Assert.Equal(50, trade.Quantity);
        Assert.Equal(125.50m, trade.Price);
        Assert.Equal(3.14m, trade.Fees);
        Assert.Equal(timestamp, trade.Timestamp);
        Assert.Equal("SETTLED", trade.Status);
    }

    [Theory]
    [InlineData("account-id", "Trade account ID has not been provided.")]
    [InlineData("instrument-id", "Trade instrument ID has not been provided.")]
    [InlineData("side", "Trade side has not been provided.")]
    [InlineData("quantity", "Trade quantity has not been provided.")]
    [InlineData("price", "Trade price has not been provided.")]
    [InlineData("fees", "Trade fees have not been provided.")]
    [InlineData("timestamp", "Trade timestamp has not been provided.")]
    [InlineData("status", "Trade status has not been provided.")]
    public void it_should_throw_when_a_required_value_is_missing(string missingValue, string expectedMessage)
    {
        TradeBuilder builder;
        InvalidOperationException exception;

        builder = new TradeBuilder();
        if (missingValue != "account-id")
        {
            builder.WithAccountId(1);
        }

        if (missingValue != "instrument-id")
        {
            builder.WithInstrumentId(1);
        }

        if (missingValue != "side")
        {
            builder.WithSide("BUY");
        }

        if (missingValue != "quantity")
        {
            builder.WithQuantity(10);
        }

        if (missingValue != "price")
        {
            builder.WithPrice(100);
        }

        if (missingValue != "fees")
        {
            builder.WithFees(1);
        }

        if (missingValue != "timestamp")
        {
            builder.WithTimestamp(DateTime.UnixEpoch);
        }

        if (missingValue != "status")
        {
            builder.WithStatus("SETTLED");
        }

        exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void it_should_reject_null_string_values()
    {
        TradeBuilder builder;

        builder = new TradeBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithSide(null!));
        Assert.Throws<ArgumentNullException>(() => builder.WithStatus(null!));
    }
}
