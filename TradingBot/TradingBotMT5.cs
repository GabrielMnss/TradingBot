using Bybit.Net.Enums;


namespace TradingBot.Test;

public class TradingBotMt5 : Mt5
{
    public override void Init()
    {
        interval = KlineInterval.FifteenMinutes;
        devise = "EURUSD";
    }


    public override void NewCandle()
    {
        Console.WriteLine(quotes.Last().Date);
        Console.WriteLine(quotes.Last().Close);

    }
}