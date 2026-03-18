namespace EasyMeeting.Services.Config;

using EasyMeeting.Models;
using System.IO;
using System.Text.Json;

public class ConfigService
{
    private readonly string _configPath;
    private AppConfig _config;
    
    public ConfigService()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(userProfile, ".easymeeting");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "config.json");
        _config = Load();
    }
    
    public AppConfig Current => _config;
    
    public AppConfig Load()
    {
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                var loaded = JsonSerializer.Deserialize<AppConfig>(json);
                if (loaded != null)
                {
                    return loaded;
                }
            }
            catch
            {
            }
        }
        
        var defaultConfig = new AppConfig();
        Save(defaultConfig);
        return defaultConfig;
    }
    
    public void Save(AppConfig config)
    {
        _config = config;
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
