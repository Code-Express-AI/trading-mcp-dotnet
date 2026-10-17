using TradingDataGenerator.Generation.Generators;

namespace XTests.DataGenerator;

public class When_Trading_Days_Are_Generated
{
    [Fact]
    public void it_should_include_weekdays_and_exclude_weekends()
    {
        TradingCalendarGenerator tradingCalendarGenerator;
        List<DateTime> tradingDays;

        tradingCalendarGenerator = new TradingCalendarGenerator();
        tradingDays = tradingCalendarGenerator.GetTradingDays(
            new DateTime(2025, 1, 3),
            new DateTime(2025, 1, 7));

        Assert.Equal(
            [new DateTime(2025, 1, 3), new DateTime(2025, 1, 6), new DateTime(2025, 1, 7)],
            tradingDays);
    }
}
