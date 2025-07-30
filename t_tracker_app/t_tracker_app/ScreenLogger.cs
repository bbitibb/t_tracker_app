namespace t_tracker_app;

public class ScreenLogger
{
    private readonly String _logDir;
    private String _focusFile;
    private int _currentDay;

    public String LogDir => _logDir;
    public String FocusFile => _focusFile;
    
    public ScreenLogger()
    {
        _logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "t_trackerLogs"
        );
        
        DateTime now = DateTime.Now;
        _focusFile = "";
        
        InitCurrent();
    }
    public void InitCurrent()
    {
        Directory.CreateDirectory(_logDir);

        DateTime now = DateTime.Now;
        _currentDay = now.Day;
        _focusFile = Path.Combine(_logDir, now.ToString("yyyy-MM-dd") + "_focus_log.csv");
        
        if (!File.Exists(_focusFile))
        {
            File.WriteAllText(_focusFile, "timestamp,window_title,exe_name\n");
        }
    }
    
    private string Quote(string s)
    {
        if (s.Contains(",") || s.Contains("\""))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
    
    public void Log(string windowTitle, string exeName)
    {
        if (DateTime.Now.Day != _currentDay)
        {
            InitCurrent();
        }
        
        DateTime now = DateTime.Now;
        string logLine = $"{now.ToString("yyyy-MM-dd HH:mm:ss")},{Quote(windowTitle)},{Quote(exeName)}\n";
        File.AppendAllText(_focusFile, logLine);
    }

    public void Stop()
    {
        string nowStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string logLine = $"{nowStr},Stopped,Stopped\n";
        File.AppendAllText(_focusFile, logLine);
    }
}
