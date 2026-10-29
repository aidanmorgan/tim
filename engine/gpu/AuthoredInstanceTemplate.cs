using CuriousContraptions.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;

namespace CuriousContraptions.Gpu;

internal enum AssistanceValue
{
    PositionWindow, RotationWindow, MaximumPositionCorrection, MaximumRotationCorrection,
    Blend, CaptureMargin, CaptureSpeed, CaptureDwell, GuideAcceleration, TriggerThreshold
}
internal readonly record struct AuthoredSlot(string Value);
internal readonly record struct AuthoredAssistanceBinding(AssistanceValue Value, ParameterId Parameter);

/// <summary>Explicit current-schema fields. No omitted scalar is replaced by a FlatBuffers default.</summary>
internal sealed record AuthoredInstanceTemplate(PartId Part, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, bool Locked,
    InstanceParameterCollection Parameters, AssistanceProfile Assistance, ImmutableArray<AuthoredAssistanceBinding> AssistanceBindings)
{
    internal AuthoredInstanceTemplate At(PuzzlePrecision precision)
    {
        var parameters = Parameters.ToDictionary();
        var assistance = Assistance.Evaluate(precision);
        foreach (var binding in AssistanceBindings)
        {
            var value = Value(assistance, binding.Value);
            parameters[binding.Parameter] = value;
        }
        return this with { Parameters = new(parameters) };
    }

    internal void Validate()
    {
        if (!DeclaredPhysics.Contains(Part)) throw new ArgumentException("Unknown declared part.");
        Parameters.Validate(Part);
        Rotation.Validate();
        // The current wire instance constructor validates typed scalars or the complete generic parameter set.
        var targets = new HashSet<ParameterId>(4);
        foreach (var binding in AssistanceBindings)
        {
            if (!Enum.IsDefined(binding.Value) || binding.Parameter == default ||
                !targets.Add(binding.Parameter) ||
                (binding.Parameter != default && !Parameters.ContainsKey(binding.Parameter)))
                throw new ArgumentException("Invalid or duplicate assistance target.");
        }
    }

    internal IWorkshopInstance Create(GpuBodyId id, PuzzlePrecision precision)
    {
        var current = At(precision);
        current.Validate();
        var instance = WorkshopAuthoringFlatBuffers.MapAuthoredInstance(current, id);
        instance.Validate();
        return instance;
    }

    private static float Value(AssistanceKnot knot, AssistanceValue field) => field switch
    {
        AssistanceValue.PositionWindow => knot.PositionWindow.Value,
        AssistanceValue.RotationWindow => knot.RotationWindowDegrees,
        AssistanceValue.MaximumPositionCorrection => knot.MaximumPositionCorrection.Value,
        AssistanceValue.MaximumRotationCorrection => knot.MaximumRotationCorrectionDegrees,
        AssistanceValue.Blend => knot.Blend.Value,
        AssistanceValue.CaptureMargin => knot.CaptureMargin.Value,
        AssistanceValue.CaptureSpeed => knot.CaptureSpeed.Value,
        AssistanceValue.CaptureDwell => knot.CaptureDwell.Value,
        AssistanceValue.GuideAcceleration => knot.GuideAcceleration.Value,
        AssistanceValue.TriggerThreshold => knot.TriggerThreshold,
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };
}

internal static class AuthoredInstanceReader
{
    internal static AuthoredInstanceTemplate Parse(JsonElement json, string where)
    {
        var fields = PuzzleContentReader.Fields(json, where);
        Require(fields, where, "part", "cell", "local", "rotation", "locked",
            "parameters", "assistance", "assistance_bindings");
        var part = new PartId(PuzzleContentReader.Text(fields, "part", where));
        if (!DeclaredPhysics.Contains(part)) throw PuzzleContentReader.Invalid(where, "part", "unknown declared part");
        var cell = Array(fields["cell"], 3, where + ".cell");
        var local = Vector(fields["local"], where + ".local");
        var rotation = Array(fields["rotation"], 4, where + ".rotation");
        var parameters = new InstanceParameterCollection(PuzzleContentReader.Fields(fields["parameters"], where + ".parameters")
            .Select(pair => KeyValuePair.Create(new ParameterId(pair.Key), Number(pair.Value, where + ".parameters." + pair.Key))));
        var assistanceBindings = ImmutableArray.CreateBuilder<AuthoredAssistanceBinding>();
        foreach (var row in Rows(fields["assistance_bindings"], where))
        {
            var binding = PuzzleContentReader.Fields(row, where);
            Require(binding, where, "value", "parameter");
            var parameter = binding["parameter"].ValueKind == JsonValueKind.Null ? default :
                new ParameterId(PuzzleContentReader.Text(binding, "parameter", where));
            assistanceBindings.Add(new(Closed<AssistanceValue>(PuzzleContentReader.Text(binding, "value", where), where), parameter));
        }
        var result = new AuthoredInstanceTemplate(part,
            new(Integer(cell[0], where + ".cell[0]"), Integer(cell[1], where + ".cell[1]"), Integer(cell[2], where + ".cell[2]")),
            new(local.X, local.Y, local.Z),
            new(Number(rotation[0], where), Number(rotation[1], where), Number(rotation[2], where), Number(rotation[3], where)),
            Boolean(fields["locked"], where + ".locked"),
            parameters, Profile(fields["assistance"], where), assistanceBindings.ToImmutable());
        try
        {
            result.Validate();
            WorkshopAuthoringFlatBuffers.MapAuthoredInstance(result, new(1)).Validate();
            foreach (var precision in new[] { 0f, .45f, 1f }) result.Create(new(1), new(precision));
        }
        catch (ArgumentException error) { throw PuzzleContentReader.Invalid(where, "instance", error.Message); }
        return result;
    }

    internal static AssistanceProfile Profile(JsonElement value, string where)
    {
        var knots = Array(value, 3, where + ".assistance");
        var result = new AssistanceProfile(Knot(knots[0], where), Knot(knots[1], where), Knot(knots[2], where));
        if (!result.HasAuthoredShape())
            throw PuzzleContentReader.Invalid(where, "assistance", "expected authored precision knots");
        return result;
    }

    private static AssistanceKnot Knot(JsonElement value, string where)
    {
        var row = Array(value, 11, where + ".assistance");
        float N(int index)
        {
            var number = Number(row[index], where);
            var admitted = (float)number;
            if (!float.IsFinite(admitted) || admitted < 0f)
                throw PuzzleContentReader.Invalid(where, "assistance", "nonfinite or negative value");
            return admitted;
        }
        return new(N(0), new(N(1)), N(2), new(N(3)), N(4), new(N(5)),
            new(N(6)), new(N(7)), new(N(8)), new(N(9)), N(10));
    }

    internal static void Require(IReadOnlyDictionary<string, JsonElement> fields, string where, params string[] keys)
    {
        if (fields.Count != keys.Length || keys.Any(key => !fields.ContainsKey(key)))
            throw PuzzleContentReader.Invalid(where, "", "missing or unknown field");
    }
    internal static T Closed<T>(string value, string where) where T : struct, Enum =>
        Enum.TryParse<T>(value, out var result) && Enum.IsDefined(result) && result.ToString() == value
            ? result : throw PuzzleContentReader.Invalid(where, value, "undefined enum value");
    internal static int Integer(JsonElement value, string where) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result) ? result :
            throw PuzzleContentReader.Invalid(where, "", "expected integer");
    internal static bool Boolean(JsonElement value, string where) => value.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => throw PuzzleContentReader.Invalid(where, "", "expected Boolean")
    };
    internal static float Number(JsonElement value, string where) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out var number) && float.IsFinite(number)
            ? number : throw PuzzleContentReader.Invalid(where, "", "expected finite f32 value");
    internal static JsonElement[] Array(JsonElement value, int count, string where)
    {
        var rows = Rows(value, where).ToArray();
        if (rows.Length != count) throw PuzzleContentReader.Invalid(where, "", "wrong vector length");
        return rows;
    }
    internal static JsonElement.ArrayEnumerator Rows(JsonElement value, string where) =>
        value.ValueKind == JsonValueKind.Array ? value.EnumerateArray() :
            throw PuzzleContentReader.Invalid(where, "", "expected array");
    internal static System.Numerics.Vector3 Vector(JsonElement value, string where)
    {
        var row = Array(value, 3, where);
        return new(Number(row[0], where), Number(row[1], where), Number(row[2], where));
    }
}
