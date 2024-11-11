using System;
using System.IO;
using Newtonsoft.Json;

namespace FanControlWPF.Config;

public class ConfigManager
{
    private readonly object _fileLock = new object();
    private readonly string _configFilePath;
    private static readonly Lazy<ConfigManager> _instance = new Lazy<ConfigManager>(() => new ConfigManager());
    public static ConfigManager Instance => _instance.Value;

    private ConfigManager()
    {
        try
        {
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _configFilePath = Path.Combine(projectDirectory, "setting.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cannot create config file, Ex: {ex}");
        }
    }

    public SystemFanControlConfig LoadConfig()
    {
        if (!File.Exists(_configFilePath))
        {
            var defaultConfig = new SystemFanControlConfig();
            SaveConfig(defaultConfig);
        }

        lock (_fileLock)
        {
            var json = File.ReadAllText(_configFilePath);
            var config = JsonConvert.DeserializeObject<SystemFanControlConfig>(json);

            return config ?? new SystemFanControlConfig();
        }
    }
    
    public void SaveConfig(SystemFanControlConfig config)
    {
        try
        {
            lock (_fileLock)
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cannot save config file, Ex: {ex}");
        }
    }
}