using TradingDataGenerator.Generation.Builders;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Account_Is_Built
{
    [Fact]
    public void it_should_create_an_account_with_the_supplied_values()
    {
        DateTime openedOn;
        Account account;

        openedOn = new DateTime(2025, 1, 15);
        account = new AccountBuilder()
            .WithId(8)
            .WithClientId(3)
            .WithAccountNumber("ACC12345678")
            .WithAccountType("Brokerage")
            .WithBaseCurrency("USD")
            .WithOpenedOn(openedOn)
            .Build();

        Assert.Equal(8, account.Id);
        Assert.Equal(3, account.ClientId);
        Assert.Equal("ACC12345678", account.AccountNumber);
        Assert.Equal("Brokerage", account.AccountType);
        Assert.Equal("USD", account.BaseCurrency);
        Assert.Equal(openedOn, account.OpenedOn);
    }

    [Theory]
    [InlineData("id", "Account ID has not been provided.")]
    [InlineData("client-id", "Account client ID has not been provided.")]
    [InlineData("account-number", "Account number has not been provided.")]
    [InlineData("account-type", "Account type has not been provided.")]
    [InlineData("base-currency", "Account base currency has not been provided.")]
    [InlineData("opened-on", "Account open date has not been provided.")]
    public void it_should_throw_when_a_required_value_is_missing(string missingValue, string expectedMessage)
    {
        AccountBuilder builder;
        InvalidOperationException exception;

        builder = new AccountBuilder();
        if (missingValue != "id")
        {
            builder.WithId(1);
        }

        if (missingValue != "client-id")
        {
            builder.WithClientId(1);
        }

        if (missingValue != "account-number")
        {
            builder.WithAccountNumber("ACC123");
        }

        if (missingValue != "account-type")
        {
            builder.WithAccountType("Brokerage");
        }

        if (missingValue != "base-currency")
        {
            builder.WithBaseCurrency("USD");
        }

        if (missingValue != "opened-on")
        {
            builder.WithOpenedOn(DateTime.UnixEpoch);
        }

        exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void it_should_reject_null_string_values()
    {
        AccountBuilder builder;

        builder = new AccountBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithAccountNumber(null!));
        Assert.Throws<ArgumentNullException>(() => builder.WithAccountType(null!));
        Assert.Throws<ArgumentNullException>(() => builder.WithBaseCurrency(null!));
    }
}
