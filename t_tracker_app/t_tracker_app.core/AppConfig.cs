using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace t_tracker_app.core;

public class AppConfig
{
    // Default settings
    public List<string> ExcludedApps { get; set; } = new List<string>
    {
        "explorer", 
        "SystemSettings",
        "t_tracker_ui"
    };
    public int IdleTimeoutSeconds { get; set; } = 900;
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";


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
        config.NormalizeExcludedApps();
        config.Save();
        return config;
    }

    public void Save()
    {
        try
        {
            NormalizeExcludedApps();
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

    public bool IsExcludedApp(string exeName)
    {
        if (string.IsNullOrWhiteSpace(exeName))
            return false;

        var normalized = NormalizeExeName(exeName);
        return ExcludedApps.Any(app =>
            string.Equals(NormalizeExeName(app), normalized, StringComparison.OrdinalIgnoreCase));
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
}