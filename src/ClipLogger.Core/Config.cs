using System.Text.Json;

namespace ClipLogger.Core;

public class Config
{
    public string LogFolder { get; set; } = "";
    public int CheckInMinutes { get; set; } = 60;
    public bool AutoStart { get; set; } = false;

    public void Save(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static Config Load(string path)
    {
        if (!File.Exists(path)) return new Config();
        try { return JsonSerializer.Deserialize<Config>(File.ReadAllText(path)) ?? new Config(); }
        catch { return new Config(); }
    }
}
