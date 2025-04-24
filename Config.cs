using System.IO;
using Newtonsoft.Json;
namespace SimpleVoiceChat;

public class Config
{
    public bool Enabled { get; set; } = true;
    public int Port { get; set; } = 50050;
    public float MaxRange { get; set; } = 64f;
    public string Codec { get; set; } = "opus";
    public bool Muffling { get; set; } = true;

    public static Config Load(string path)
    {
        if (!File.Exists(path))
        {
            return new Config();
        }
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<Config>(json);
    }
}