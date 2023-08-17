using Bybit.Net.Enums;
using Skender.Stock.Indicators;


namespace TradingBot.Test;

public class Macd : Strategy
{
    public override void Init()
    {
        interval = KlineInterval.FifteenMinutes;
    }
    
    public override void Step()
    {
        var quotes = backtest.quotes;
        
        var signal = GetSignal(quotes);
        var longEma = quotes.TakeLast(205).GetEma(200).Last().Ema;

        var volatility = GetVolatility(quotes, 30);
        
        //Console.WriteLine(volatility);
        
        if(backtest.GetNumberOfPositions() < 1)
        {
            if (volatility < 3) return;
            var sl = Decimal.Abs(quotes.Last().Close / (decimal)(longEma * 1.02d) - 1);
            var tp = 1.5m * sl;
            if (signal == 'b') backtest.Buy(tp, sl, 1, 1);
            if (signal == 's') backtest.Sell(tp, sl, 1, 1);
        }
    }


    private decimal GetVolatility(List<Quote> quotes, int periods)
    {
        var max = quotes.Last().High;
        var min = quotes.Last().Low;
        var quotesCount = quotes.Count;

        for (var i = 0; i < periods; i++)
        {
            var quote = quotes[quotesCount - i - 1];
            if (quote.High > max)
                max = quote.High;
            if (quote.Low < min)
                min = quote.Low;
        }

        return ((max / min) - 1) * 100;
    }

    private char GetSignal(List<Quote> quotes)
    {
        var macdResults = quotes.TakeLast(100).GetMacd(12, 26, 9).ToList();
        var emaResults = quotes.TakeLast(205).GetEma(200).ToList();
        

        Console.WriteLine($"{macdResults.Last().Macd} {macdResults.Last().Signal} {quotes.Last().Date}");
        if(macdResults.Last().Macd >= macdResults.Last().Signal && macdResults[^2].Macd <= macdResults[^2].Signal && macdResults.Last().Signal <= 0)
            if (emaResults.Last().Ema < (double)quotes.Last().Low)
                return 'b';
        
        if(macdResults.Last().Macd <= macdResults.Last().Signal && macdResults[^2].Macd >= macdResults[^2].Signal && macdResults.Last().Signal >= 0)
            if (emaResults.Last().Ema > (double)quotes.Last().High)
                return 's';
        
        return 'n';
    }

    public override void ShowChart()
    {
    }
}