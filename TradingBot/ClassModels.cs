using Bybit.Net.Enums;
using Skender.Stock.Indicators;


namespace TradingBot;

public abstract class Strategy
{
    public Backtest backtest;

    public KlineInterval interval;
    
    public abstract void Init();
    public abstract void Step();
    public abstract void ShowChart();
}


public abstract class Mt5
{
    public List<Quote> quotes;

    public KlineInterval interval;
    public string devise;
    
    public abstract void Init();
    
    public abstract void NewCandle();
}