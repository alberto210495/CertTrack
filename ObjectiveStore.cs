using System.Text.Json;
using System.Text.Json.Serialization;

namespace CertTrack;

public class ObjectiveStore(string path = "objectives.json")
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public List<Objective> Load()
    {
        if (!File.Exists(path))
            return [];

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Objective>>(json, Options) ?? [];
    }

    public void Save(List<Objective> objectives)
    {
        var json = JsonSerializer.Serialize(objectives, Options);
        File.WriteAllText(path, json);
    }
}
