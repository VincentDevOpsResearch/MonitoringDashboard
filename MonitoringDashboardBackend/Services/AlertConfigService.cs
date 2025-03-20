using System.Text.Json;
using MonitoringDashboard.Models;


public class AlertConfigService
{
    private readonly string _configPath = "config.json";
    private Dictionary<string, AlertThreshold> _thresholds = new();

    public AlertConfigService()
    {
        LoadConfig();
    }

    public Dictionary<string, AlertThreshold> GetThresholds() => _thresholds;

    public bool UpdateThreshold(string category, double threshold)
    {
        if (!_thresholds.ContainsKey(category))
        {
            return false; // Category not found, return failure
        }

        _thresholds[category].Threshold = threshold;
        SaveConfig();
        return true; // Successfully updated
    }

    private void LoadConfig()
    {
        if (File.Exists(_configPath))
        {
            var json = File.ReadAllText(_configPath);
            _thresholds = JsonSerializer.Deserialize<Dictionary<string, AlertThreshold>>(json)
                         ?? new Dictionary<string, AlertThreshold>();
        }
    }

    private void SaveConfig()
    {
        var json = JsonSerializer.Serialize(_thresholds, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
