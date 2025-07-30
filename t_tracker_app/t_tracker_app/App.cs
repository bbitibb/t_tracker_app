namespace t_tracker_app
{
    class App
    {
        public static void Run()
        {
            var fetcher = new WindowInfoFetcher();
            var logger = new ScreenLogger();

            (string prevTitle, string prevExe) = ("", "");
                
            Console.WriteLine("Tracking started. Press CTRL+C to stop...");
            
            while (true)
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
        }
    }
}