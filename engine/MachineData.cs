using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CuriousContraptions;

public enum TubeBendAngle { Degrees45 = 45, Degrees90 = 90 }

/// <summary>Puzzle-authored assistance at one difficulty knot; precision 0 is easiest, 1 is strict.</summary>
public sealed record PartDifficulty
{
    public float Precision { get; init; }
    public float PositionWindow { get; init; }
    public float RotationWindow { get; init; }
    public float MaxPositionCorrection { get; init; }
    public float MaxRotationCorrection { get; init; }
    public float BlendSeconds { get; init; } = .4f;
    public float CaptureMargin { get; init; } = .02f;
    public float CaptureSpeed { get; init; } = 1.5f;
    public float CaptureDwell { get; init; } = .35f;
    public float GuideAcceleration { get; init; }
    public float TriggerThreshold { get; init; } = .8f;
}

[JsonConverter(typeof(InternalBodyRoleJsonConverter))]
public enum InternalBodyRole { Plunger }

/// <summary>Canonical serialization names only; unsupported roles are rejected.</summary>
public sealed class InternalBodyRoleJsonConverter : JsonConverter<InternalBodyRole>
{
    public override InternalBodyRole Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.String && reader.GetString() == "plunger"
            ? InternalBodyRole.Plunger : throw new JsonException("Unknown internal body role.");
    public override void Write(Utf8JsonWriter writer, InternalBodyRole value, JsonSerializerOptions options)
    {
        if (value != InternalBodyRole.Plunger) throw new JsonException("Unknown internal body role.");
        writer.WriteStringValue("plunger");
    }
}

public sealed class InternalBodySpec
{
    [JsonRequired]
    public InternalBodyRole Role { get; set; }
    public float[] InitialVelocity { get; set; } = [0, 0, 0];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PartSpec
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";
    public List<PartDifficulty> Difficulty { get; set; } = new();
    public bool Locked { get; set; }
    public float[] Position { get; set; } = [0, 1, 0];
    [JsonRequired]
    public PartOrientation Orientation
    {
        get;
        set { ArgumentNullException.ThrowIfNull(value); field=value; }
    } = PartOrientation.Identity;
    public float[] InitialVelocity { get; set; } = [0, 0, 0];
    public List<InternalBodySpec> InternalBodies { get; set; } = new();
    public Dictionary<string, float> Properties { get; set; } = new();
}
[JsonConverter(typeof(ConnectionDomainJsonConverter))]
public enum ConnectionDomain { Unknown, Activation, Electrical, Signal, Mechanical, Rope }
public sealed class ConnectionDomainJsonConverter() :
    JsonStringEnumConverter<ConnectionDomain>(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false);

public static class PipeParameters
{
    public const float MinimumLength = 1;
    public const float MaximumLength = 8;
    public const float BoreDiameter = 1.3f;
}

public enum PipeParameter { Length }
public enum ReceiverParameter { Threshold }

public enum ClockParameter { IntervalSeconds }

public enum CounterParameter { TargetCount }

public enum PressurePlateParameter { MinimumMass }

public enum HoldTimerParameter { HoldSeconds }

public enum DelayParameter { DelaySeconds }

public enum WeightParameter { Mass }

// Engine-independent geometry used by authored rope lengths and runtime sockets.
public static class RopeGeometry
{
    public const float PulleyRadius = .4f;
    public const float PulleySocketDepth = .12f;
    public static float WeightRadius(float mass) => .32f * MathF.Cbrt(mass);
    public static float WeightTieHeight(float mass) => WeightRadius(mass) + .08f;
}

[JsonConverter(typeof(SocketIdJsonConverter))]
public enum SocketId
{
    ActivationOut = 1, SetIn, ResetIn, ActivationIn, FirstIn, SecondIn,
    Supply, PowerIn, Drive, DriveIn, Tie, ExtendIn, RetractIn, ExtendedOut, RetractedOut
}

/// <summary>Exact current wire names only; no aliases, numeric values or case folding.</summary>
public sealed class SocketIdJsonConverter : JsonConverter<SocketId>
{
    private static readonly Dictionary<string, SocketId> FromWire = new(StringComparer.Ordinal);
    private static readonly Dictionary<SocketId, string> ToWire = new();
    static SocketIdJsonConverter()
    {
        foreach (var value in Enum.GetValues<SocketId>())
        {
            var name = JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());
            FromWire.Add(name, value);
            ToWire.Add(value, name);
        }
    }
    public override SocketId Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String ||
            !FromWire.TryGetValue(reader.GetString()!, out var value))
            throw new JsonException("Unknown socket identity.");
        return value;
    }
    public override void Write(Utf8JsonWriter writer, SocketId value, JsonSerializerOptions options)
    {
        if (!ToWire.TryGetValue(value, out var name))
            throw new JsonException("Unknown socket identity.");
        writer.WriteStringValue(name);
    }
}

public sealed record ConnectionSpec
{
    public string From { get; init; } = "";
    public string To { get; init; } = "";
    public ConnectionDomain Type { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SocketId? FromPort { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SocketId? ToPort { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? RopeLength { get; init; }
}
[JsonConverter(typeof(GoalKindJsonConverter))]
public enum GoalKind { Unknown, Captured, Activated, Powered, Turned, PoweredAfter, ActivatedAfter }
public sealed class GoalKindJsonConverter() :
    JsonStringEnumConverter<GoalKind>(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false);

public sealed record GoalSpec
{
    public GoalKind Type { get; init; }
    public string Target { get; init; } = "";
    public string Body { get; init; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float MinimumDelaySeconds { get; init; }
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
    public const int CurrentVersion = 4;
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
[JsonSerializable(typeof(SocketId))]
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
