using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using t_tracker_app;
using Xunit;

namespace t_tracker_app.Tests;

public class FocusTrackerServiceTests
{
    private sealed class ScriptedFetcher : WindowInfoFetcher
    {
        private readonly Queue<(string Title, string Exe)> _script;
        private (string Title, string Exe) _last;

        public ScriptedFetcher(IEnumerable<(string Title, string Exe)> script, (string,string) fallback)
        {
            _script = new Queue<(string Title, string Exe)>(script);
            _last = (fallback.Item1, fallback.Item2);
        }

        public override (string Title, string ExeName) GetActiveWindowInfo()
        {
            if (_script.Count > 0) _last = _script.Dequeue();
            return (_last.Title, _last.Exe);
        }
    }

    private static string UseTempDb()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "t_tracker_tests_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "focus.db");
        Environment.SetEnvironmentVariable("T_TRACKER_DB_PATH", dbPath);
        return tempDir;
    }

    private static void CleanupTempDb(string tempDir)
    {
        Environment.SetEnvironmentVariable("T_TRACKER_DB_PATH", null);
        try { Directory.Delete(tempDir, recursive: true); } catch { }
    }

}
