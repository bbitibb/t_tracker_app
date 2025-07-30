namespace t_tracker_app
{
    class App
    {
        private static bool _shouldStop = false;
        
        public static void Run()
        {
            var fetcher = new WindowInfoFetcher();
            var logger = new ScreenLogger();

            (string prevTitle, string prevExe) = ("", "");
            
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("\nStopping tracking and showing statistics...");
                _shouldStop = true;
                e.Cancel = true;
            };
            
            Console.WriteLine("Tracking started. Press CTRL+C to stop...");
            
            while (!_shouldStop)
            {
                var (title, exe) = fetcher.GetActiveWindowInfo();

                if (title != prevTitle || exe != prevExe)
                {
                    logger.Log(title, exe);
                    prevTitle = title;
                    prevExe = exe;
                }

                Thread.Sleep(500);
            }
            
            logger.Stop();
            
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "t_trackerLogs",
                $"{today}_focus_log.csv"
            );

            var stats = new ScreenStatistics();
            var entries = stats.ParseEntries(logPath);
            var usage = stats.CalculateUsage(entries, 10);
            stats.PrintTopApps(usage);
        }
    }
}