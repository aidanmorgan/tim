using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CuriousContraptions;

/// <summary>Puzzle-authored assistance at one difficulty knot; precision 0 is easiest, 1 is strict.</summary>
public sealed class PartDifficulty
{
    public float Precision { get; set; }
    public float PositionWindow { get; set; }
    public float RotationWindow { get; set; }
    public float MaxPositionCorrection { get; set; }
    public float MaxRotationCorrection { get; set; }
    public float BlendSeconds { get; set; } = .4f;
    public float CaptureMargin { get; set; } = .02f;
    public float CaptureSpeed { get; set; } = 1.5f;
    public float CaptureDwell { get; set; } = .35f;
    public float GuideAcceleration { get; set; }
    public float TriggerThreshold { get; set; } = .8f;
}

public sealed class PartSpec
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";
    public List<PartDifficulty> Difficulty { get; set; } = new();
    public bool Locked { get; set; }
    public float[] Position { get; set; } = [0, 1, 0];
    public float[] Rotation { get; set; } = [0, 0, 0];
    public Dictionary<string, float> Properties { get; set; } = new();
}
public sealed class ConnectionSpec
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Type { get; set; } = "power";
}
public sealed class GoalSpec
{
    public string Type { get; set; } = "";
    public string Target { get; set; } = "";
    public string Body { get; set; } = "";
}
public sealed class MachineData
{
    public List<PartSpec> PlacementTargets { get; set; } = new();
    public List<PartSpec> Parts { get; set; } = new();
    public List<ConnectionSpec> Connections { get; set; } = new();
    public List<GoalSpec> Goals { get; set; } = new();
    public float Gravity { get; set; } = 9.81f;
    public float Pressure { get; set; } = 1;
}
public sealed class PuzzleData
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Description { get; set; } = "";
    public string Hint { get; set; } = "";
    public Dictionary<string, int> Inventory { get; set; } = new();
    public List<PartSpec> Parts { get; set; } = new();
    public List<GoalSpec> Goals { get; set; } = new();
    public List<PartSpec> Solution { get; set; } = new();
    public List<ConnectionSpec> SolutionConnections { get; set; } = new();
    public MachineData CreateMachine() => new() { Parts = Parts, Goals = Goals, PlacementTargets = Solution };
}
public sealed class SavedMachine
{
    public int Version { get; set; } = 1;
    public int Level { get; set; }
    public float Precision { get; set; } = .45f;
    public bool Realistic { get; set; }
    public int NextId { get; set; } = 1;
    public MachineData Machine { get; set; } = new();
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, WriteIndented = true)]
[JsonSerializable(typeof(MachineData))]
[JsonSerializable(typeof(SavedMachine))]
[JsonSerializable(typeof(List<PuzzleData>))]
public partial class MachineJson : JsonSerializerContext { }

public static class MachineCodec
{
    public static MachineData Clone(MachineData value) =>
        JsonSerializer.Deserialize(JsonSerializer.Serialize(value, MachineJson.Default.MachineData), MachineJson.Default.MachineData)!;

    public static List<PuzzleData> ReadPuzzles(string json) =>
        JsonSerializer.Deserialize(json, MachineJson.Default.ListPuzzleData) ?? new();
}
