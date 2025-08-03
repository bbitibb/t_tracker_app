// at top
using System;
using System.Threading;

namespace t_tracker_app;

class App
{
    private static bool _stop;

    public static void Run()
    {
        var fetcher = new WindowInfoFetcher();
        (string prevT, string prevE) = ("", "");

        Console.CancelKeyPress += (_, e) => { _stop = true; e.Cancel = true; };

        Console.WriteLine("Tracking…  CTRL+C to stop");
        using (var logger = new ScreenLogger())
        {
            
            while (!_stop)
            {
                var (title, exe) = fetcher.GetActiveWindowInfo();
                if (title != prevT || exe != prevE)
                {
                    logger.Log(title, exe);
                    prevT = title; prevE = exe;
                }
                Thread.Sleep(500);
            }

            logger.Stop();
        
        }
        // show stats
        var stats = new ScreenStatistics();
        var entries = stats.LoadDay(DateOnly.FromDateTime(DateTime.Now));
        foreach (var (exe, secs) in stats.TopN(entries, 10))
            Console.WriteLine($"{exe,-20} {TimeSpan.FromSeconds(secs):hh\\:mm\\:ss}");
        var rows = stats.LoadDay(DateOnly.FromDateTime(DateTime.Now));
        Console.WriteLine($"DEBUG: rows loaded = {rows.Count}");
    }
}