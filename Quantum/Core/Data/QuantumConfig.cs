using System;
using Newtonsoft.Json;

namespace Quantum.Core.Data;

public class QuantumConfig
{
    [JsonProperty("chunks")]
    public int Chunks { get; set; } = 4;
    [JsonProperty("openfolder")]
    public bool OpenFileFolder = false;
    [JsonProperty("deletetask")]
    public bool DeleteTask = false;
    [JsonProperty("ua")]
    public string UserAgent { get; set; } = "Quantum/1.0";
    [JsonProperty("dest")]
    public string Destination { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    [JsonProperty("theme")]
    public int Theme { get; set; } = 2;
}