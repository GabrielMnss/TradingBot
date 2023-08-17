using Bybit.Net.Enums;
using Skender.Stock.Indicators;
using System.Drawing.Printing;


namespace TradingBot.Test;

public class SimpleEMA : Strategy
{
    public override void Step()
    {
        var quotes = backtest.quotes;


        var trend = Trend(quotes);
        var stcSignal = GetStc(quotes);

        if(backtest.GetNumberOfPositions() < 1)
        {
            if (trend == 'b' && stcSignal == 'b') backtest.Buy(0.002m, 0.0015m, 1);
            //if (trend == 's' && stcSignal == 's') backtest.Sell(0.002m, 0.002m, 1);
        }
        
    }


    private char GetStc(List<Quote> quotes)
    {
        var stc = quotes.TakeLast(100).GetStc().ToList();
        //Console.WriteLine($"{stc.Last().Stc} {quotes.Last().Date}");

        if (stc.Last().Stc <= 2) return 'b';
        if (stc.Last().Stc <= 99.5f && stc[^2].Stc >= 99.8f) return 's';

        return 'n';
    }

    private char Trend(List<Quote> quotes)
    {
        var ema = quotes.TakeLast(250).GetEma(200).ToList();

        bool upTrend = true, downTrend = true;
               
        for (int i = 0; i < 120; i++)
        {
            if (ema[^(i + 1)].Ema > (double)quotes[^(i + 1)].Close) upTrend = false;
            if (ema[^(i + 1)].Ema < (double)quotes[^(i + 1)].Close) downTrend = false;
        }

        return upTrend ? 'b' : downTrend ? 's' : 'n';
    }


    public override void Init()
    {
        interval = KlineInterval.OneMinute;
    }


    public override void ShowChart()
    {
    }
}