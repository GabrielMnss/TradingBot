using Bybit.Net.Enums;
using ScottPlot;
using ScottPlot.Drawing.Colormaps;
using Skender.Stock.Indicators;
using System.Drawing;


namespace TradingBot.Test;

public class Divergence : Strategy
{
    public override void Init()
    {
        interval = KlineInterval.FifteenMinutes;
    }


    enum States
    {
        FirstUp,
        FirstDown,
        SecUp,
        SecDown
    }


    private bool isTop;
    private bool isBottom;
    private decimal targetLastQuote;

    public List<double> posX = new();
    public List<double> posY = new();
    private int l = 0;

    public override void Step()
    {
        if (backtest.GetNumberOfPositions() > 0) return;

        var quotes = backtest.quotes;

        bool tops = false;
        bool bottoms = false;
        if (isTop == false)
            tops = GetTops3(quotes.TakeLast(50).ToList(), quotes.TakeLast(100).GetRsi().TakeLast(50).ToList());

        if (isBottom == false)
            bottoms = GetBottoms(quotes.TakeLast(50).ToList(), quotes.TakeLast(100).GetRsi().TakeLast(50).ToList());

        if (tops) isTop = true;

        if (bottoms) isBottom = true;

        var ema50 = quotes.TakeLast(200).GetEma(50).ToList();
        var ema200 = quotes.TakeLast(250).GetEma(200).ToList();

        var volatility = GetVolatility(quotes, 10);
        var limit = 0.4m;

        var tp = volatility;
        tp = tp > limit ? limit : tp;
        var sl = tp * 0.6m;
        decimal precision = 0.001m;
        
        if (isTop)
        {
            var slPrice = quotes.Last().Close * (1 - sl / 100);
            var lot = 0.01m * backtest.balance * backtest.ACCOUNT_LEVERAGE
                / (Math.Abs(quotes.Last().Close - slPrice) / backtest.PIP) * 0.01m;

            lot = Math.Floor(lot / precision) * precision;

            isTop = false;
            backtest.Sell(tp, sl, lot: 0.01m);

            posX.Add(backtest.quotes.IndexOf(quotes.Last()));
            posY.Add((double)quotes.Last().Close);

            if (l < 10)
            {
                var plt = new ScottPlot.Plot(1920, 1080);
                
                List<OHLC> prices = new List<OHLC>();
                foreach (var quote in backtest.quotes.TakeLast(50))
                {
                    OHLC price = new(
                        open: (int)(quote.Open * 100000),
                        high: (int)(quote.High * 100000),
                        low: (int)(quote.Low * 100000),
                        close: (int)(quote.Close * 100000),
                        timeStart: quote.Date,
                        timeSpan: TimeSpan.FromMinutes(15)
                    );
                    prices.Add(price);
                }

                plt.AddCandlesticks(prices.ToArray());
                plt.SaveFig($"D:\\TradingBot\\TradingBot\\TradingBot\\Chart\\{l}.png");
                l++;
            }
        }

        return;
        if (isBottom)
        {
            var slPrice = quotes.Last().Close * (1 + sl / 100);
            var lot = 0.01m * backtest.balance * backtest.ACCOUNT_LEVERAGE
                / (Math.Abs(quotes.Last().Close - slPrice) / backtest.PIP) * 0.01m;

            lot = Math.Floor(lot / precision) * precision;

            isBottom = false;
            backtest.Buy(tp, sl, lot: 0.01m);

            posX.Add(backtest.quotes.IndexOf(quotes.Last()));
            posY.Add((double)quotes.Last().Close);
        }
    }


    bool GetTops3(List<Quote> quotes, List<RsiResult> rsiResults)
    {
        int period = 10;
        int count = quotes.Count;
        var HH = new Dictionary<int, decimal>();
        var LL = new Dictionary<int, decimal>();

        bool isHH = true;
        
        for (int i = 0; i < quotes.Count - period; i+=10)
        {
            if (isHH)
            {
                var higher = quotes.GetRange(i, period).Max(x => x.High);
                var index = quotes.FindIndex(x => x.High == higher);
                HH.TryAdd(index, higher);
                isHH = false;
            }
            else
            {
                var lower = quotes.GetRange(i, period).Min(x => x.Low);
                var index = quotes.FindIndex(x => x.Low == lower);
                LL.TryAdd(index, lower);
                isHH = true;
            }
        }

        if (l > 2) return false;
        var plt = new ScottPlot.Plot(1920, 1080);
                
        List<OHLC> prices = new List<OHLC>();
        for (int i = 0; i < quotes.Count; i++)
        {

            OHLC price = new(
                open: (int)(quotes[i].Open * 100000),
                high: (int)(quotes[i].High * 100000),
                low: (int)(quotes[i].Low * 100000),
                close: (int)(quotes[i].Close * 100000),
                timeStart: new DateTime(i),
                timeSpan: TimeSpan.FromMinutes(1)
            );
            prices.Add(price);
        }

        plt.AddCandlesticks(prices.ToArray());
        plt.SaveFig($"D:\\TradingBot\\TradingBot\\TradingBot\\Chart\\coucou.png");
        return false;
    }
    
    bool GetTops2(List<Quote> quotes, List<RsiResult> rsiResults)
    {
        int indexMaxTopOne = 0, indexMaxTopTwo = 0, indexMin = 0;
        decimal maxTopOne = 0, maxTopTwo = 0, minBot = 100;

        bool down = false;

        for (int i = 0; i < quotes.Count; i++)
        {
            if (maxTopOne < quotes[i].Close && !down)
            {
                indexMaxTopOne = i;
                maxTopOne = quotes[i].Close;
                continue;
            }

            if (minBot > quotes[i].Close)
            {
                indexMin = i;
                minBot = quotes[i].Close;
                if (i - indexMaxTopOne > 2)
                {
                    down = true;
                    i -= 2;
                }
            }
            else if (down)
            {
                if (quotes[indexMin].Close > quotes[indexMaxTopOne].Close * 0.9997m) return false;
                return true;
            }
        }

        return false;
    }


    bool GetTops(List<Quote> quotes, List<RsiResult> rsiResult)
    {
        int indexFirstDown = 0;
        decimal target = 0;
        States state = States.FirstUp;

        for (int i = 1; i < quotes.Count - 1; i++)
        {
            switch (state)
            {
                case States.FirstUp:
                    if (quotes[i].Close < quotes[i - 1].Open)
                    {
                        var maxClose = quotes.GetRange(0, i + 1).Max(x => x.Close);
                        indexFirstDown = quotes.IndexOf(
                            quotes.Where(x => x.Close == maxClose).ToList().FirstOrDefault() ?? quotes[i - 1]
                        );
                        state = States.FirstDown;
                    }

                    break;

                case States.FirstDown:
                    if (quotes[i].Close < quotes[i + 1].Close)
                    {
                        if (Math.Abs(quotes[i].Close - quotes[indexFirstDown].Close) / quotes[indexFirstDown].Close
                            * 100
                            < 0.015m)
                            return false;
                        state = States.SecUp;
                        target = quotes.GetRange(0, i).Min(w => w.Close);
                    }

                    break;

                case States.SecUp:
                    if (quotes.Last().Close > target)
                        state = States.SecDown;
                    else
                        return false;
                    break;

                case States.SecDown:
                    if (rsiResult[indexFirstDown].Rsi > rsiResult.Last().Rsi + 5
                        && quotes[indexFirstDown].Close < quotes.Last().Close)
                        return true;
                    return false;
            }
        }

        return false;
    }


    bool GetBottoms(List<Quote> quotes, List<RsiResult> rsiResult)
    {
        int indexFirstup = 0;
        decimal firstTarget = 0;
        States state = States.FirstDown;

        for (int i = 1; i < quotes.Count - 1; i++)
        {
            switch (state)
            {
                case States.FirstDown:
                    if (quotes[i].Close > quotes[i - 1].Open)
                    {
                        var maxClose = quotes.GetRange(0, i + 1).Min(x => x.Close);
                        indexFirstup = quotes.IndexOf(
                            quotes.Where(x => x.Close == maxClose).ToList().FirstOrDefault() ?? quotes[i - 1]
                        );
                        state = States.FirstUp;
                    }

                    break;

                case States.FirstUp:
                    if (quotes[i].Close > quotes[i + 1].Close)
                    {
                        if (Math.Abs(quotes[i].Close - quotes[indexFirstup].Close) / quotes[indexFirstup].Close * 100
                            < 0.015m)
                            return false;
                        state = States.SecDown;
                        firstTarget = quotes[i].Close * 1.0015m;
                    }

                    break;

                case States.SecDown:
                    if (quotes.Last().Close < firstTarget)
                        state = States.SecUp;
                    else
                        return false;
                    break;

                case States.SecUp:
                    if (rsiResult[indexFirstup].Rsi < rsiResult.Last().Rsi
                        && quotes[indexFirstup].Close > quotes.Last().Close)
                        return true;
                    return false;
            }
        }

        return false;
    }


    public override void ShowChart()
    {
        return;
        List<double> xs = new();
        for (int i = 0; i < backtest.quotes.Count; i++)
        {
            xs.Add(i);
        }

        var plt = new ScottPlot.Plot(7680, 4320);

        for (int i = 0; i < posX.Count; i++)
        {
            plt.AddPoint(posX[i], posY[i], Color.Red, size: 25);
        }

        var closes = backtest.quotes.Select(x => (double)x.Close).ToArray();
        List<OHLC> prices = new List<OHLC>();
        foreach (var quote in backtest.quotes)
        {
            OHLC price = new(
                open: (int)(quote.Open * 1000),
                high: (int)(quote.High * 1000),
                low: (int)(quote.Low * 1000),
                close: (int)(quote.Close * 1000),
                timeStart: quote.Date,
                timeSpan: TimeSpan.FromMinutes(15)
            );
            prices.Add(price);
        }

        plt.AddOHLCs(prices.ToArray());
        plt.SaveFig($"D:\\TradingBot\\TradingBot\\TradingBot\\Chart\\0.png");
    }


    private decimal GetVolatility(List<Quote> quotes, int periods)
    {
        var max = quotes.Last().High;
        var min = quotes.Last().Low;
        var quotesCount = quotes.Count;

        for (var i = 0; i < periods; i++)
        {
            var quote = quotes[quotesCount - i - 1];
            if (quote.High > max) max = quote.High;
            if (quote.Low < min) min = quote.Low;
        }

        return ((max / min) - 1) * 100;
    }


}