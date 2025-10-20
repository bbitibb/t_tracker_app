using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System;

namespace t_tracker_app.core;

public class AppConfig
{
    public Dictionary<string, string> DisplayNames { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);
    public List<string> ExcludedApps { get; set; } = new List<string>
    {
        "explorer",
        "SystemSettings",
        "t_tracker_ui"
    };
    public int IdleTimeoutSeconds { get; set; } = 900;
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";
    public bool EnableProxyTracking { get; set; } = true;
    public int  ProxyPort           { get; set; } = 8888;
    public bool SetSystemProxy      { get; set; } = true;

    [JsonIgnore]
    public string FilePath { get; private set; }

    public AppConfig()
    {
        FilePath = GetDefaultConfigFilePath();
        NormalizeExcludedApps();
    }

    private static string GetDefaultConfigFilePath()
    {
        var configDir = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "t_tracker");
        Directory.CreateDirectory(configDir);
        return Path.Combine(configDir, "config.json");
    }

    public static AppConfig Load()
    {
        var config = new AppConfig();
        if (File.Exists(config.FilePath))
        {
            try
            {
                var jsonString = File.ReadAllText(config.FilePath);
                var loadedConfig = JsonSerializer.Deserialize<AppConfig>(jsonString);
                if (loadedConfig != null)
                {
                    loadedConfig.FilePath = config.FilePath;
                    loadedConfig.NormalizeExcludedApps();
                    loadedConfig.NormalizeDisplayNames();
                    return loadedConfig;
                }
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"Error loading config file: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"Error reading config file: {ex.Message}");
            }
        }
        config.Save();
        config.NormalizeExcludedApps();
        return config;
    }
    public bool IsExcludedApp(string exeName)
    {
        if (string.IsNullOrWhiteSpace(exeName))
            return false;

        var normalized = NormalizeExeName(exeName);
        return ExcludedApps.Any(app =>
            string.Equals(NormalizeExeName(app), normalized, StringComparison.OrdinalIgnoreCase));
    }

    public void SetDisplayName(string exeOrProcessName, string? displayName)
    {
        var key = NormalizeExeName(exeOrProcessName);
        var val = displayName?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(key)) return;

        if (string.IsNullOrWhiteSpace(val))
        {
            DisplayNames.Remove(key);
        }
        else if (string.Equals(key, val, StringComparison.Ordinal))
        {
            DisplayNames.Remove(key);
        }
        else
        {
            DisplayNames[key] = val;
        }

        NormalizeDisplayNames();
        Save();
    }
    
    public void NormalizeDisplayNames()
    {
        DisplayNames = DisplayNames
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
            .GroupBy(kv => NormalizeExeName(kv.Key), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().Value?.Trim() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
    }
    
    public string GetDisplayNameOrExe(string exeOrProcessName)
    {
        var key = NormalizeExeName(exeOrProcessName);
        return DisplayNames.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val)
            ? val
            : key;
    }
    
    public void NormalizeExcludedApps()
    {
        ExcludedApps = ExcludedApps
            .Select(NormalizeExeName)
            .Where(e => !string.IsNullOrEmpty(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string NormalizeExeName(string? exeOrProcessName)
    {
        if (string.IsNullOrWhiteSpace(exeOrProcessName))
            return string.Empty;

        var trimmed = exeOrProcessName.Trim();

        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^4]
            : trimmed;
    }
    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = JsonSerializer.Serialize(this, options);
            File.WriteAllText(FilePath, jsonString);
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Error saving config file: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Error serializing config: {ex.Message}");
        }
    }
}