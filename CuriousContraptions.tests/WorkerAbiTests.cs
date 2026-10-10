using System.Globalization;
using System.Text.RegularExpressions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The simulation worker reads and writes records whose layouts the C# ABI owns. Each layout constant the worker declares must
/// equal its C# source, so a change on either side fails here instead of silently misreading committed bodies, motion or responses.</summary>
public sealed class WorkerAbiTests(ITestOutputHelper output)
{
    private static readonly IReadOnlyDictionary<string, int> Owned = new Dictionary<string, int>
    {
        ["ELECTRICAL_SOURCES_OFFSET"] = PhysicsGpuAbi.ElectricalSourcesOffset,
        ["ELECTRICAL_SOURCE_BYTES"] = PhysicsGpuAbi.ElectricalSourceBytes,
        ["ELECTRICAL_BINDINGS_OFFSET"] = PhysicsGpuAbi.ElectricalBindingsOffset,
        ["ELECTRICAL_BINDING_BYTES"] = PhysicsGpuAbi.ElectricalBindingBytes,
        ["CONTACT_WORKS_OFFSET"] = PhysicsGpuAbi.ContactWorksOffset,
        ["BODIES_OFFSET"] = PhysicsGpuAbi.BodiesOffset,
        ["BODY_BYTES"] = PhysicsGpuAbi.BodyBytes,
        ["BODY_VELOCITY"] = PhysicsGpuAbi.BodyVelocityOffset,
        ["BODY_ANGULAR_VELOCITY"] = PhysicsGpuAbi.BodyAngularVelocityOffset,
        ["BODY_MASS"] = PhysicsGpuAbi.BodyMassOffset,
        ["BODY_DRAG"] = PhysicsGpuAbi.BodyDragOffset,
        ["BODY_GRAVITY"] = PhysicsGpuAbi.BodyGravityOffset,
        ["BODY_CENTRE_OF_MASS"] = PhysicsGpuAbi.BodyCentreOfMassOffset,
        ["ORIENTATION_SENSORS_OFFSET"] = PhysicsGpuAbi.OrientationSensorsOffset,
        ["MOTION_OFFSET"] = PhysicsGpuAbi.MotionOffset,
        ["MOTION_HEADER_BYTES"] = PhysicsMotionRead.HeaderBytes,
        ["MOTION_PIECE_BYTES"] = PhysicsMotionRead.PieceBytes,
        ["MOTION_DRAG_RATE"] = PhysicsMotionRead.DragRateOffset,
        ["MOTION_VELOCITY"] = PhysicsMotionRead.VelocityOffset,
        ["MOTION_ANGULAR_VELOCITY"] = PhysicsMotionRead.AngularVelocityOffset,
        ["MOTION_GRAVITY"] = PhysicsMotionRead.GravityOffset,
        ["READ_BODIES_OFFSET"] = WorkshopWire.ReadBodiesOffset,
        ["BODY_WIRE_BYTES"] = PhysicsBodyWire.ByteLength,
        ["BODY_WIRE_VELOCITY"] = PhysicsBodyWire.VelocityOffset,
    };

    [Fact]
    public void EveryWorkerLayoutConstantEqualsItsCSharpOwner()
    {
        // The normal suite always checks the real worker. A candidate copy (WORKER_SOURCE, as the Node rigid-body harness uses) is
        // checked only on the explicit opt-in WORKER_ABI_CANDIDATE=1, and the resolved path is always reported.
        var candidate = Environment.GetEnvironmentVariable("WORKER_ABI_CANDIDATE") == "1";
        var path = candidate
            ? Environment.GetEnvironmentVariable("WORKER_SOURCE") ?? throw new InvalidOperationException("WORKER_ABI_CANDIDATE=1 requires WORKER_SOURCE.")
            : Path.Combine(twodog.Engine.ResolveProjectDir(), "CuriousContraptions.Simulation/wwwroot/worker.js");
        output.WriteLine($"Worker source checked: {path}");
        // A duplicated declaration throws in ToDictionary; a missing or drifted one fails the comparison.
        var declared = Regex.Matches(File.ReadAllText(path), @"^const ([A-Z_]+) = (\d+);", RegexOptions.Multiline)
            .Where(match => Owned.ContainsKey(match.Groups[1].Value))
            .ToDictionary(match => match.Groups[1].Value, match => int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
        Assert.Equal(Owned.OrderBy(entry => entry.Key), declared.OrderBy(entry => entry.Key));
    }
}
