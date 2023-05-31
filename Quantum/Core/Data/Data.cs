using System;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using Newtonsoft.Json;

namespace Quantum.Core.Data;

public class Data
{
    public static string QuantumDataPath =
        Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Quantum");
    public static string QuantumConfigPath = Path.Join(QuantumDataPath, "config.json");
    public static string QuantumPluginPath = Path.Join(QuantumDataPath, "Plugins");
    public static QuantumConfig CurrentQuantumConfig = new QuantumConfig();

    public static void Ensure()
    {
        if (!Directory.Exists(QuantumDataPath))
        {
            Directory.CreateDirectory(QuantumDataPath);
        }
        if (!Directory.Exists(QuantumPluginPath))
        {
            Directory.CreateDirectory(QuantumPluginPath);
        }
        if (!File.Exists(QuantumConfigPath))
        {
            File.Create(QuantumConfigPath).Close();
            File.WriteAllText(QuantumConfigPath, JsonConvert.SerializeObject(CurrentQuantumConfig), Encoding.UTF8);
        }
        else
        {
            if (!ParseConfig())
            {
                File.WriteAllText(QuantumConfigPath, JsonConvert.SerializeObject(CurrentQuantumConfig), Encoding.UTF8);
            }
        }
    }

    public static bool ParseConfig()
    {
        string txt = File.ReadAllText(QuantumConfigPath).Trim();
        if (txt.StartsWith("{") && txt.EndsWith("}"))
        {
            return true;
        }

        return false;
    }

    public static void ReadConfig()
    {
        Ensure();
        try
        {
            QuantumConfig obj = JsonConvert.DeserializeObject<QuantumConfig>(File.ReadAllText(QuantumConfigPath));
            CurrentQuantumConfig = obj;
        }
        catch (Exception)
        {
            CurrentQuantumConfig = new QuantumConfig();
        }
    }

    public static void WriteConfig()
    {
        Ensure();
        File.WriteAllText(QuantumConfigPath, JsonConvert.SerializeObject(CurrentQuantumConfig), Encoding.UTF8);
    }
}