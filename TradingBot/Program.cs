using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects;
using CryptoExchange.Net.Authentication;
using Newtonsoft.Json;
using Skender.Stock.Indicators;
using System.Globalization;
using System.Reflection;
using TradingBot;
using static TradingBot.Display;


namespace Program;

class TradingBotHeart
{
    private const string apiKey = "Za9tuvbvZiel2WW0QD"; 
    private const string apiSecret = "El8hrqvGQfkSvAUuOiX9qEeNCHyuJ90W3sc5";

    private static string strategyName = "IaDivergence";

    private static bool backtesting = true;

    static void Main(string[] args)
    {
        if(backtesting)
        {
            DateTime from = new(2023, 01, 01);
            DateTime to = new(2023, 07, 01);

            StartBacktest(from, to);
            return;
        }
        
        StartTrading();
    }


    private static void StartTrading()
    {
        var targetType = Assembly.GetExecutingAssembly().GetTypes().ToList().Find(x => x.Name == strategyName);
        if (targetType == null)
            throw new Exception("Unable to find strategy");
        var newStrategy = (Mt5) Activator.CreateInstance(targetType, new object[] { })!;
        newStrategy.Init();

        var newTrading = new MetaTrader5Api();
        newTrading.strategy = newStrategy;
        newTrading.StartMt5Api();

    }
    private static void StartBacktest(DateTime from, DateTime to)
    {
        var bybitClient = new BybitClient(new BybitClientOptions()
        {
            ApiCredentials = new ApiCredentials(apiKey, apiSecret)
        });

        var targetType = Assembly.GetExecutingAssembly().GetTypes().ToList().Find(x => x.Name == strategyName);
        if (targetType == null)
            throw new Exception("Unable to find strategy");
        var newStrategy = (Strategy) Activator.CreateInstance(targetType, new object[] { })!;
        newStrategy.Init();

        var interval = newStrategy.interval;
        var quotes = GetKlines(from, to, Category.Linear, "EURUSD", interval, bybitClient).Result;

        if (quotes == null)
        {
            Console.WriteLine("Error No Quotes");
            return;
        }
        
        if(quotes.Last().Date > quotes.FirstOrDefault()!.Date)
            quotes.Reverse();
        
        var newBacktest = new Backtest(quotes, from, to, interval);
        newStrategy.backtest = newBacktest;
        
        RunBacktest(newBacktest, newStrategy);
    }


    private static void RunBacktest(Backtest backtest, Strategy strategy)
    {
        do
        {
            backtest.Step();
            strategy.Step();
        }
        while (backtest.running);
        strategy.ShowChart();
        ShowBacktestResults(backtest);
    }


    private static void ShowBacktestResults(Backtest backtest)
    {
        var winRate = GetWinrate(backtest.historic);
        print($"\n\n Balance : {backtest.balance:0.00}$\n WinRate : {winRate:0.00}%\n"
              + $" Totals positions : {backtest.historic.Count}\n", ConsoleColor.DarkYellow);

    }


    private static double GetWinrate(List<Backtest.Position> positions)
    {
        double winPos = positions.Count(x => x.status == 'w');
        return winPos / positions.Count * 100;
    }
    
    
    private static async Task<List<Quote>?> GetKlines(DateTime from, DateTime to, Category category, string symbol, KlineInterval interval, BybitClient bybitClient)
    {
        var marge = - 300 * ((int)interval/60);
        var newFrom = from.AddMinutes(marge);

        List<Quote>? quotes = new List<Quote>();
            
        var dinfo = new DirectoryInfo(@"D:\TradingBot\TradingBot\TradingBot\Klines\");
            
        foreach (var fileInfo in dinfo.GetFiles("*.json"))
        {
            var values = fileInfo.Name.Split("_");

            var fileSymbol = values[1];
            var fileFrom = DateTime.ParseExact($"{values[2]}_{values[3]}_{values[4]}", "yyyy_MM_dd", CultureInfo.InvariantCulture);
            var fileTo = DateTime.ParseExact($"{values[5]}_{values[6]}_{values[7]}", "yyyy_MM_dd", CultureInfo.InvariantCulture);
            var fileInterval = values[8].Split(".")[0];
                
            var path = @"D:\TradingBot\TradingBot\TradingBot\Klines\" + fileInfo.Name;
            if (fileSymbol == symbol && fileInterval == interval.ToString())
            {
                if(fileFrom <= newFrom && fileTo >= to)
                {
                    print("Klines exist\n", ConsoleColor.Green);
                        
                    var json = File.ReadAllText(path);
                    quotes = JsonConvert.DeserializeObject<List<Quote>>(json);
                    return quotes;
                }
            }
        }
        print("Klines don't exist, loading", ConsoleColor.Red);

        var actualTo = to;
        
        Console.Write("Loading");
        
        while(actualTo > newFrom)
        {
            Console.Write(".");
            var result = await bybitClient.V5Api.ExchangeData.GetKlinesAsync(category, symbol, interval, newFrom, actualTo);

            if (result.Success)
            {
                var klines = result.Data.List;
                foreach (var kline in klines)
                {
                    quotes.Add(
                        new Quote()
                        {
                            Date = kline.StartTime,
                            Close = kline.ClosePrice,
                            Open = kline.OpenPrice,
                            Volume = kline.Volume,
                            High = kline.HighPrice,
                            Low = kline.LowPrice
                        }
                    );
                }
                actualTo = quotes.Last().Date;
            }
            else print("Error downloading\n", ConsoleColor.Red);
        }
        print("Loaded\n", ConsoleColor.Green);
        SaveFile(quotes, newFrom, to, symbol, interval);

        return quotes;
    }


    private static void SaveFile(List<Quote>? quotes, DateTime from, DateTime to, string symbol, KlineInterval interval)
    {
        var jsonQuotes = JsonConvert.SerializeObject(quotes, Formatting.Indented);
            
        var name = $"backtest_{symbol}_{from:yyyy_MM_dd}_{to:yyyy_MM_dd}_{interval.ToString()}";

        var path = @"D:\TradingBot\TradingBot\TradingBot\Klines\" + name + ".json";
        using (StreamWriter sw = File.CreateText(path))
            sw.Write(jsonQuotes);
    }
        
}