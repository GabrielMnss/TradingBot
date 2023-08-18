using Bybit.Net.Enums;
using Keras.Constraints;
using Keras.Models;
using Numpy;
using Python.Runtime;
using Skender.Stock.Indicators;
using System.Drawing.Printing;


namespace TradingBot.Test;

public class IaDivergence : Strategy
{
    private BaseModel model;
    private BaseModel model_bottom;


    public override void Init()
    {
        interval = KlineInterval.FifteenMinutes;

        model = BaseModel.ModelFromJson(File.ReadAllText(@"D:\TradingBot\TradingBot\TradingBot\Weights\model_bottom4.json"));
        model.LoadWeight(@"D:\TradingBot\TradingBot\TradingBot\Weights\top4.h5");
        model_bottom = BaseModel.ModelFromJson(File.ReadAllText(@"D:\TradingBot\TradingBot\TradingBot\Weights\model_bottom4.json"));
        model_bottom.LoadWeight(@"D:\TradingBot\TradingBot\TradingBot\Weights\bottom4.h5");
    }


    public override void Step()
    {
        if (backtest.GetNumberOfPositions() > 0) return;
        var quotes = backtest.quotes;

        var volatility = GetVolatility(quotes, 20);
        
        var rsi = quotes.TakeLast(200).GetRsi().ToList().TakeLast(15).ToArray();
        var lastQ = quotes.TakeLast(15).ToList();

        var index = lastQ.FindIndex(w => w.High == lastQ.Max(x => x.High));

        var rescaled = Rescale(lastQ);

        var prediction = model.Predict(rescaled, verbose: 0);

        var data = prediction.GetData<float>()[0];

        var ema50 = quotes.TakeLast(100).GetEma(50).ToList();
        var ema200 = quotes.TakeLast(250).GetEma(200).ToList();
       

        var limit = 1;
        
        if (data > 0.9f && rsi[index].Rsi-7 > rsi.Last().Rsi)
        {
            
            var tp = volatility;
            tp = tp > limit ? limit : tp;
            var sl = tp * 0.6m;
            
            var slPrice = quotes.Last().Close * (1 - sl / 100);
            var lot = 0.01m * backtest.balance * backtest.ACCOUNT_LEVERAGE
                / (Math.Abs(quotes.Last().Close - slPrice) / backtest.PIP) * 0.01m;
            decimal precision = 0.001m;

            lot = Math.Floor(lot / precision) * precision;
            
            backtest.Sell(tp, sl, lot:lot);
        }
        else
        {
            var prediction_bottom = model.Predict(rescaled, verbose: 0);

            var data_bottom = prediction_bottom.GetData<float>()[0];
            
            var index_low = lastQ.FindIndex(w => w.Low == lastQ.Min(x => x.Low));

            if (data_bottom > 0.9f && rsi[index_low].Rsi+7 < rsi.Last().Rsi)
            {
                var tp = volatility;    
                tp = tp > limit ? limit : tp;
                var sl = tp * 0.6m;
                
                var slPrice = quotes.Last().Close * (1 - sl / 100);
                var lot = 0.01m * backtest.balance * backtest.ACCOUNT_LEVERAGE
                    / (Math.Abs(quotes.Last().Close - slPrice) / backtest.PIP) * 0.01m;
                decimal precision = 0.001m;

                lot = Math.Floor(lot / precision) * precision;
                
                backtest.Buy(tp, sl, lot:lot);
            }
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
            if (quote.High > max) max = quote.High;
            if (quote.Low < min) min = quote.Low;
        }

        return ((max / min) - 1) * 100;
    }
    private static decimal StandVolatility(List<Quote> quotes, int periods)
    {
        decimal volatility = 0;

        for (int i = 0; i < periods; i++)
        {
            var quote = quotes[quotes.Count - i - 1];
            volatility += quote.High - quote.Low;
        }

        return volatility / periods;
    }

    NDarray Rescale(List<Quote> quotes)
    {
        var nDarray = np.array(new float[1, quotes.Count, 2]);

        float max = 0;
        float min = 100;

        foreach (var quote in quotes)
        {
            if (quote.High > (decimal)max) max = (float)quote.High;
            if (quote.Low < (decimal)min) min = (float)quote.Low;
        }
            
            
        for (int i = 0; i < quotes.Count; i++)
        {
            var ohlc = np.array(
                new float[]
                {
                    ((float)quotes[i].Open - min) / (max - min),
                    //((float)quotes[i].High - min) / (max - min),
                    //((float)quotes[i].Low - min) / (max - min),
                    ((float)quotes[i].Close - min) / (max - min)
                }
            );
            nDarray[0, i] = ohlc;
        }

        return nDarray;
    }


    public override void ShowChart()
    {
    }
}