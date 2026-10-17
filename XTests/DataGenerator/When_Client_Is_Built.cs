using TradingDataGenerator.Generation.Builders;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Client_Is_Built
{
    [Fact]
    public void it_should_create_a_client_with_the_supplied_values()
    {
        DateTime createdOn;
        Client client;

        createdOn = new DateTime(2025, 1, 15);
        client = new ClientBuilder()
            .WithId(3)
            .WithFullName("Avery Example")
            .WithEmail("avery.example@example.com")
            .WithCountry("United Kingdom")
            .WithRiskProfile("Balanced")
            .WithCreatedOn(createdOn)
            .Build();

        Assert.Equal(3, client.Id);
        Assert.Equal("Avery Example", client.FullName);
        Assert.Equal("avery.example@example.com", client.Email);
        Assert.Equal("United Kingdom", client.Country);
        Assert.Equal("Balanced", client.RiskProfile);
        Assert.Equal(createdOn, client.CreatedOn);
    }

    [Fact]
    public void it_should_throw_when_required_values_are_missing()
    {
        ClientBuilder clientBuilder;

        clientBuilder = new ClientBuilder();

        Assert.Throws<InvalidOperationException>(() => clientBuilder.Build());
    }

    [Theory]
    [InlineData("id", "Client ID has not been provided.")]
    [InlineData("full-name", "Client full name has not been provided.")]
    [InlineData("email", "Client email has not been provided.")]
    [InlineData("country", "Client country has not been provided.")]
    [InlineData("risk-profile", "Client risk profile has not been provided.")]
    [InlineData("created-on", "Client creation date has not been provided.")]
    public void it_should_throw_for_each_missing_required_value(string missingValue, string expectedMessage)
    {
        ClientBuilder builder;
        InvalidOperationException exception;

        builder = new ClientBuilder();
        if (missingValue != "id")
        {
            builder.WithId(1);
        }

        if (missingValue != "full-name")
        {
            builder.WithFullName("Avery Example");
        }

        if (missingValue != "email")
        {
            builder.WithEmail("avery@example.com");
        }

        if (missingValue != "country")
        {
            builder.WithCountry("United Kingdom");
        }

        if (missingValue != "risk-profile")
        {
            builder.WithRiskProfile("Balanced");
        }

        if (missingValue != "created-on")
        {
            builder.WithCreatedOn(DateTime.UnixEpoch);
        }

        exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void it_should_reject_null_string_values()
    {
        ClientBuilder builder;

        builder = new ClientBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithFullName(null!));
        Assert.Throws<ArgumentNullException>(() => builder.WithEmail(null!));
        Assert.Throws<ArgumentNullException>(() => builder.WithCountry(null!));
        Assert.Throws<ArgumentNullException>(() => builder.WithRiskProfile(null!));
    }
}
