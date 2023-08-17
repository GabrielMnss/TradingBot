using Bybit.Net.Enums;
using static TradingBot.Display;
using Skender.Stock.Indicators;

namespace TradingBot;

public class Backtest
{
    public decimal balance = 100;

    public List<Quote> quotes = new();
    private List<Quote> totalQuotes;

    private List<Position> positions = new();
    public List<Position> historic = new();

    public bool running;

    private DateTime from;
    private DateTime to;
    private DateTime actualTo;
    
    private KlineInterval interval;

    public decimal PIP = 0.0001m;
    public decimal ONELOT = 100000;
    public decimal ACCOUNT_LEVERAGE = 30;
    
    public Backtest(List<Quote> quotesList, DateTime from, DateTime to, KlineInterval interval)
    {
        this.from = from;
        this.to = to;
        this.interval = interval;
        actualTo = from;
        
        quotesList.Reverse();
        totalQuotes = quotesList;
    }


    public void Step()
    {
        running = balance >= 0 && actualTo < to;
        
        actualTo = actualTo.AddMinutes((int)interval/60);
        quotes = totalQuotes.Where(quote => quote.Date < actualTo).ToList();
        
        CheckPositionsStatus();
    }


    private void CheckPositionsStatus()
    {
        for (int i = positions.Count - 1; i >= 0; i--)
        {
            if(positions[i].status != 'r') continue;
            
            var position = positions[i];
            var tpPrice = position.tpPrice;
            var slPrice = position.slPrice;

            if (position.type == "b")
            {
                if (quotes.Last().Low <= slPrice)
                    Loose(position);
                else if (quotes.Last().High >= tpPrice)
                    Win(position);
            }
            else
            {
                if (quotes.Last().High >= slPrice)
                    Loose(position);
                else if (quotes.Last().Low <= tpPrice)
                    Win(position);
            }
        }
    }


    private bool GetMinMargin(decimal lot, decimal leverage = 30)
    {
        return ONELOT * lot > balance * leverage;
    }
    public void Buy(decimal tp, decimal sl, decimal leverage = 30, decimal lot = 0.01m)
    {
        if (lot < 0.01m) return;
        if (balance < 1 || GetNumberOfPositions() > 0 || GetMinMargin(lot, leverage)) return;
        tp /= 100;
        sl /= 100;
        var newPosition = new Position()
        {
            tp = tp,
            sl = sl,
            leverage = leverage,
            lot = lot,
            tpPrice = quotes.Last().Close * (1 + tp),
            slPrice = quotes.Last().Close * (1 - sl),
            entryPrice = quotes.Last().Close,
            type = "b",
        };

        print($"Buy     ", ConsoleColor.Blue);
        print($"    [{quotes.Last().Date}]  ->  ", ConsoleColor.White);
        positions.Add(newPosition);
    }
    
    public void Sell(decimal tp, decimal sl, decimal leverage = 30, decimal lot = 0.01m)
    {
        if (lot < 0.01m) return;
        if (GetMinMargin(lot, leverage))
        {
            print($"Error lot too small", ConsoleColor.Red);
            lot = 0.01m;
        }
        if (balance < 1 || GetNumberOfPositions() > 0 || GetMinMargin(lot, leverage)) return;
        
        tp /= 100;
        sl /= 100;

        var newPosition = new Position()
        {
            tp = tp,
            sl = sl,
            leverage = leverage,
            tpPrice = quotes.Last().Close * (1 - tp),
            slPrice = quotes.Last().Close * (1 + sl),
            entryPrice = quotes.Last().Close,
            lot = lot,
            type = "s",
        };
        
        print($"Sell    ", ConsoleColor.Magenta);
        print($"    [{quotes.Last().Date}]  ->  ", ConsoleColor.White);
        positions.Add(newPosition);
    }


    private void Loose(Position position)
    {
        decimal diff = Math.Abs(position.entryPrice - position.slPrice) / PIP;
        decimal value = PIP * diff * position.lot * ONELOT;
        balance -= value;
        
        position.status = 'l';
        historic.Add(position);
        positions.Remove(position);
        
        print($"Loose   (-{value:0.00}$)    ", ConsoleColor.Red);
        print($"[{quotes.Last().Date}]", ConsoleColor.White);
        print($"    {balance:0.00}$\n", ConsoleColor.Yellow);
    }


    private void Win(Position position)
    {
        decimal diff = Math.Abs(position.entryPrice - position.tpPrice) / PIP;
        decimal value = PIP * diff * position.lot * ONELOT;
        balance += value;

        position.status = 'w';
        historic.Add(position);
        positions.Remove(position);
        
        print($"Win     (+{value:0.00}$)    ", ConsoleColor.Green);
        print($"[{quotes.Last().Date}]", ConsoleColor.White);
        print($"    {balance:0.00}$\n", ConsoleColor.Yellow);
    }


    public int GetNumberOfPositions() => positions.Count;
    
    public class Position
    {
        public decimal tp;
        public decimal sl;
        public decimal leverage;
        public decimal entryPrice;
        public decimal lot;
        public decimal tpPrice;
        public decimal slPrice;
        public string type;
        public char status = 'r';
    }
}