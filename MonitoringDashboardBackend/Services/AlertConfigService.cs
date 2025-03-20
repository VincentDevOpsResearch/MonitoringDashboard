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

    public void UpdateThreshold(string category, double threshold)
    {
        if (!_thresholds.ContainsKey(category))
        {
            _thresholds[category] = new AlertThreshold();
        }

        _thresholds[category].Threshold = threshold;
        SaveConfig();
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
