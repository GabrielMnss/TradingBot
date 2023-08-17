using Newtonsoft.Json;
using Skender.Stock.Indicators;
using System.Net;
using System.Timers;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Timer = System.Threading.Timer;


namespace TradingBot;

class Messages
{
    public string status { get; set; }

    public string content { get; set; }
}


public class MetaTrader5Api
{
    public Mt5 strategy;


    public void StartMt5Api()
    {
        HttpWebRequest request =
            (HttpWebRequest)WebRequest.Create("http://127.0.0.1:8000/Init?interval=15&devise=EURUSD");
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        using (Stream stream = response.GetResponseStream())
        using (StreamReader reader = new(stream))
        {
            var result = reader.ReadToEnd();

            var message = JsonConvert.DeserializeObject<Messages>(result);

            if (message.status == "error") return;

            var jsonResult = JsonConvert.DeserializeObject(message.content).ToString();
            var candles = JsonConvert.DeserializeObject<List<Quote>>(jsonResult);

            candles.RemoveAt(candles.Count - 1);

            strategy.quotes = candles;
            Console.WriteLine(candles.Last().Date);

            stream.Close();
            response.Close();

            var minutes = 15 - DateTime.Now.Minute % 15;
            Console.WriteLine($"Wait {minutes}min");

            Thread.Sleep(minutes * 60 * 1000 + 1); //
            Console.WriteLine("Bot Started");
            OnTimedEvent();
        }
    }


    public void OnTimedEvent()
    {
        while (true)
        {
            HttpWebRequest request =
                (HttpWebRequest)WebRequest.Create("http://127.0.0.1:8000/GetLastCandle?interval=15&devise=EURUSD");
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new(stream))
            {
                var message = JsonConvert.DeserializeObject<Messages>(reader.ReadToEnd());

                if (message.status == "error") return;

                var jsonQuotes = JsonConvert.DeserializeObject(message.content)?.ToString();

                var newQuote = JsonConvert.DeserializeObject<List<Quote>>(jsonQuotes)[0];

                strategy.quotes.Insert(strategy.quotes.Count - 1, newQuote);

                strategy.NewCandle();
                stream.Close();
                response.Close();
            }
            Thread.Sleep(1000 * 60 * 15);
        }
    }
}