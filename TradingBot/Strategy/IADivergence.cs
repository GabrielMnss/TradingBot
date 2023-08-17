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

        model = BaseModel.ModelFromJson(File.ReadAllText(@"TradingBot/Weights/model_top2.json"));
        model.LoadWeight(@"TradingBot/Weights/top2.h5");
        model_bottom = BaseModel.ModelFromJson(File.ReadAllText(@"TradingBot/Weights/model_bottom2.json"));
        model_bottom.LoadWeight(@"TradingBot/Weights/bottom2.h5");
    }


    public override void Step()
    {
        if (backtest.GetNumberOfPositions() > 0) return;
        var quotes = backtest.quotes;

        var volatility = GetVolatility(quotes, 20);
        
        var rsi = quotes.TakeLast(200).GetRsi().ToList().TakeLast(10).ToArray();
        var lastQ = quotes.TakeLast(10).ToList();

        var index = lastQ.FindIndex(w => w.High == lastQ.Max(x => x.High));

        var rescaled = Rescale(lastQ);

        var prediction = model.Predict(rescaled, verbose: 0);

        var data = prediction.GetData<float>()[0];
        
       

        var limit = 1;
        
        if (data > 0.75f && rsi[index].Rsi-7 > rsi.Last().Rsi)
        {
            
            var tp = volatility;
            tp = tp > limit ? limit : tp;
            var sl = tp * 0.8m;
            backtest.Sell(tp, sl);
        }
        else
        {
            var prediction_bottom = model.Predict(rescaled, verbose: 0);

            var data_bottom = prediction_bottom.GetData<float>()[0];
            
            var index_low = lastQ.FindIndex(w => w.Low == lastQ.Min(x => x.Low));

            if (data_bottom > 0.75f && rsi[index_low].Rsi+7 < rsi.Last().Rsi)
            {
                var tp = volatility;    
                tp = tp > limit ? limit : tp;
                var sl = tp * 0.8m;
                backtest.Buy(tp, sl);
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
        var nDarray = np.array(new float[1, quotes.Count, 4]);

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
                    ((float)quotes[i].High - min) / (max - min),
                    ((float)quotes[i].Low - min) / (max - min),
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