using System;
using System.IO;
using System.Text.Json;

namespace SOE_PubEditor.Models;

/// <summary>
/// Stores persistent application settings.
/// </summary>
public class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SOE_PubEditor",
        "settings.json");
    
    public string? PubDirectory { get; set; }
    public string? GfxDirectory { get; set; }
    public string? SaveDirectory { get; set; }
    public bool EnablePubSplitting { get; set; } = true;
    public int MaxEntriesPerFile { get; set; } = 900;
    public int WindowWidth { get; set; } = 1200;
    public int WindowHeight { get; set; } = 800;
    
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Fall through to default
        }
        return new AppSettings();
    }
    
    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail
        }
    }
}
