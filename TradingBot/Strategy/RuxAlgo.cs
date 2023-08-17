using Bybit.Net.Enums;
using Skender.Stock.Indicators;
using System.Diagnostics.CodeAnalysis;


namespace TradingBot.Test;

public class RuxAlgo : Strategy
{
    public override void Init()
    {
        interval = KlineInterval.FiveMinutes;
    }

    private List<double> rsiResults = new();
    private List<double> triggerList = new();
    
    public override void Step()
    {

        var quotes = backtest.quotes;

        char signal = 'n';
        if(backtest.GetNumberOfPositions() < 1)
            signal = GetRsiLux(quotes);
        
        if(signal == 'b')
            backtest.Buy(0.06m, 0.03m, 1, 1);
        if(signal == 's')
            backtest.Sell(0.06m, 0.03m, 1, 1);
        
     
            
    }


    public override void ShowChart()
    {
        List<double> xs = new List<double>();
        for (int i = 0; i < rsiResults.Count; i++)
        {
            xs.Add(i);
        }
        var plt = new ScottPlot.Plot(1920, 1080);
        plt.AddScatter(xs.ToArray(), rsiResults.ToArray());
        plt.AddScatter(xs.ToArray(), triggerList.ToArray());
        plt.SaveFig(@"D:\TradingBot\TradingBot\TradingBot\Chart\quickstart3.png");
    }

    int rsiLength = 15;
    int power = 1;
    private List<Quote> ama = new();

    [SuppressMessage("ReSharper.DPA", "DPA0000: DPA issues")]
    private char GetRsiLux(List<Quote> quotes)
    {
        float alpha = 0;
     
        if (ama.Count < 1)
        {
            ama.AddRange(quotes);
        }
        else
        {
            var newQuotesList = new List<Quote>();
            for (int i = 0; i < quotes.Count; i++)
            {
                newQuotesList.Add(new Quote
                {
                   Close = quotes[i].Close - ama[^2].Close,
                   Open = quotes[i].Open,
                   High = quotes[i].High,
                   Low = quotes[i].Low,
                });
            }
            var lasQuotes = newQuotesList.Last();
            alpha = MathF.Abs((float)(newQuotesList.TakeLast(100).GetRsi(rsiLength).Last().Rsi / 100 - 0.5f));
         
            var v = (decimal)MathF.Pow(alpha, power);

            var newQuotes = new Quote
            {
                Close = lasQuotes.Close * v + ama[^2].Close,
                Open = lasQuotes.Open,
                High = lasQuotes.High,
                Low = lasQuotes.Low
            };
           
            ama.Add(newQuotes);
        }
        var ema1 = quotes.TakeLast(100).GetEma(rsiLength / 2).ToList();
        var rsi1 = ema1.GetRsi(rsiLength).ToList();
        var trigger = rsi1.GetEma(rsiLength / 2).ToList();
        
        var rsi2 = ama.TakeLast(100).GetRsi(rsiLength).ToList();
        rsiResults.Add((double)rsi2.Last().Rsi);
        triggerList.Add((double)trigger.Last().Ema);
        Console.WriteLine($"alpha : {alpha}, ama : {ama.Last().Close}, rsi2 : {rsi2.Last().Rsi}, trigger : {trigger.Last().Ema}, Date : {quotes.Last().Date}");

        return 'n';
    }
    
}