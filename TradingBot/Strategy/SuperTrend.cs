using Bybit.Net.Enums;
using Skender.Stock.Indicators;
using System.Runtime.InteropServices;


namespace TradingBot.Test;

public class SuperTrend : Strategy
{
    public override void Init()
    {
        interval = KlineInterval.FiveMinutes;
    }
    
    public override void Step()
    {
        var quotes = backtest.quotes;


        var ema50 = quotes.TakeLast(70).GetEma(50).ToList();
        var ema200 = quotes.TakeLast(250).GetEma(200).ToList();
        
        var volatility = GetVolatility(quotes, 5);

        if (volatility < 0.1m) return;
        
        var tp = volatility;
        var sl = tp * 0.8m;

        var signal = GetSignal(quotes);
        
        if (signal == 's' && ema200.Last().Ema > ema50.Last().Ema)
            backtest.Sell(tp, sl, 1, 1);
        if (signal == 'b' && ema200.Last().Ema < ema50.Last().Ema)
            backtest.Buy(tp, sl, 1, 1);
    }



    double GetRsiMoy(List<RsiResult> rsi)
    {
        return (double)rsi.Average(x => x.Rsi);
    }
    
    char GetSignal(List<Quote> quotes)
    {
        var rsi = quotes.TakeLast(100).GetRsi().ToList();

        if (rsi.Last().Rsi < 60 && rsi.Last().Rsi > 40) return 'n';

        var rsiAv = GetRsiMoy(rsi);

        if (rsi.Last().Rsi > rsiAv && rsi[^2].Rsi < rsiAv) return 'b';
        if (rsi.Last().Rsi < rsiAv && rsi[^2].Rsi > rsiAv) return 's';
        return 'n';
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

   


    public override void ShowChart()
    {
    }
}