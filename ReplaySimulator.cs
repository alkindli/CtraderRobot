
using System;
using System.Collections.Generic;

public class Trade
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; }
    public double Price { get; set; }
    public int Volume { get; set; }
}

public class ReplaySimulator
{
    private List<Trade> trades;

    public ReplaySimulator()
    {
        trades = new List<Trade>
        {
            new Trade { Timestamp = DateTime.Parse("2023-01-01T00:00:00Z"), Action = "BUY", Price = 1.2345, Volume = 1000 },
            new Trade { Timestamp = DateTime.Parse("2023-01-01T00:01:00Z"), Action = "SELL", Price = 1.2350, Volume = 1000 },
            new Trade { Timestamp = DateTime.Parse("2023-01-01T00:02:00Z"), Action = "BUY", Price = 1.2340, Volume = 1000 },
            new Trade { Timestamp = DateTime.Parse("2023-01-01T00:03:00Z"), Action = "SELL", Price = 1.2345, Volume = 1000 }
        };
    }

    public void Simulate()
    {
        foreach (var trade in trades)
        {
            Console.WriteLine($"Timestamp: {trade.Timestamp}, Action: {trade.Action}, Price: {trade.Price}, Volume: {trade.Volume}");
        }
    }

    public static void Main(string[] args)
    {
        ReplaySimulator simulator = new ReplaySimulator();
        simulator.Simulate();
    }
}
