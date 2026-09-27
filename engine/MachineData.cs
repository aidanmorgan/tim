using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CuriousContraptions;

public enum TubeBendAngle { Degrees45 = 45, Degrees90 = 90 }

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
[JsonConverter(typeof(ConnectionDomainJsonConverter))]
public enum ConnectionDomain { Unknown, Activation, Electrical, Signal, Mechanical, Rope }
public sealed class ConnectionDomainJsonConverter() :
    JsonStringEnumConverter<ConnectionDomain>(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false);

public static class PipeParameters
{
    public const string Length = "length";
    public const float MinimumLength = 1;
    public const float MaximumLength = 8;
    public const float BoreDiameter = 1.3f;
}

public static class ReceiverParameters
{
    public const string Threshold = "threshold";
}

public static class ClockParameters
{
    public const string Seconds = "interval_seconds";
}

public static class CounterParameters
{
    public const string Target = "target_count";
}

public static class PressurePlateParameters
{
    public const string MinimumMass = "minimum_mass";
}

public static class HoldTimerParameters
{
    public const string Seconds = "hold_seconds";
}

public static class DelayParameters
{
    public const string Seconds = "delay_seconds";
}

public static class WeightParameters
{
    public const string Mass = "mass";
}

// Engine-independent geometry used by authored rope lengths and runtime sockets.
public static class RopeGeometry
{
    public const float PulleyRadius = .4f;
    public const float PulleySocketDepth = .12f;
    public static float WeightRadius(float mass) => .32f * MathF.Cbrt(mass);
    public static float WeightTieHeight(float mass) => WeightRadius(mass) + .08f;
}

public static class SocketIds
{
    public const string ActivationOut = "activation_out";
    public const string SetIn = "set_in";
    public const string ResetIn = "reset_in";
    public const string ActivationIn = "activation_in";
    public const string FirstIn = "first_in";
    public const string SecondIn = "second_in";
    public const string Supply = "supply";
    public const string PowerIn = "power_in";
    public const string Drive = "drive";
    public const string DriveIn = "drive_in";
    public const string Tie = "tie";
}

public sealed class ConnectionSpec
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public ConnectionDomain Type { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FromPort { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToPort { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? RopeLength { get; set; }
}
[JsonConverter(typeof(GoalKindJsonConverter))]
public enum GoalKind { Unknown, Captured, Activated, Powered, Turned, PoweredAfter, ActivatedAfter }
public sealed class GoalKindJsonConverter() :
    JsonStringEnumConverter<GoalKind>(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false);

public sealed class GoalSpec
{
    public GoalKind Type { get; set; }
    public string Target { get; set; } = "";
    public string Body { get; set; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float MinimumDelaySeconds { get; set; }
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
    public const int CurrentVersion = 3;
    public int Version { get; set; } // Absent or older versions are rejected.
    public string PuzzleId { get; set; } = "";
    public float Precision { get; set; } = .45f;
    public bool Realistic { get; set; }
    public int NextId { get; set; } = 1;
    public MachineData Machine { get; set; } = new();
}

// Only the current schema is supported; campaign positions are never save identities.
public static class CampaignProgress
{
    public static int ResolveLevel(SavedMachine saved, IReadOnlyList<PuzzleData> puzzles)
    {
        if (saved.Version != SavedMachine.CurrentVersion) return -1;
        if (saved.PuzzleId == "") return puzzles.Count; // Free workshop.
        for (var i = 0; i < puzzles.Count; i++)
            if (puzzles[i].Id == saved.PuzzleId) return i;
        return -1;
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, WriteIndented = true)]
[JsonSerializable(typeof(ConnectionDomain))]
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
