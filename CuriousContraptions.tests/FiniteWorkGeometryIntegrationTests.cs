using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

public sealed class FiniteWorkGeometryIntegrationTests
{
    public enum PayloadGeometry { Sphere, Cylinder, Box, Compound }

    [Theory]
    [InlineData(PayloadGeometry.Sphere, false)]
    [InlineData(PayloadGeometry.Cylinder, false)]
    [InlineData(PayloadGeometry.Box, false)]
    [InlineData(PayloadGeometry.Compound, false)]
    [InlineData(PayloadGeometry.Sphere, true)]
    [InlineData(PayloadGeometry.Cylinder, true)]
    [InlineData(PayloadGeometry.Box, true)]
    [InlineData(PayloadGeometry.Compound, true)]
    public void RotatedMaterialEitherClearsOrRefusesBeforePaying(PayloadGeometry kind, bool blocked)
    {
        var fixture = new LawScene(false);
        var centre = new Vector3(0, 3, 0);
        var turn = kind switch
        {
            PayloadGeometry.Sphere => Quaternion.Identity,
            PayloadGeometry.Cylinder => Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f),
            PayloadGeometry.Box => Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f),
            PayloadGeometry.Compound => Quaternion.CreateFromYawPitchRoll(.7f, .4f, .9f),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var axis = Vector3.Transform(Vector3.UnitY, turn);
        var transverse = Vector3.Transform(Vector3.UnitX, turn) * .4f;
        var material = new LawMaterial(0f, 0f, 0f, 0f);
        var frame = fixture.StaticBox(centre, new(.1f), turn, material, new(0, -2, 0));
        var payload = kind switch
        {
            PayloadGeometry.Sphere => fixture.Sphere(centre, .35f, 1f, material, transverse, Vector3.Zero, 0, 0),
            PayloadGeometry.Cylinder => fixture.Cylinder(centre, .35f, .35f, 1f, material, turn, transverse, gravity: 0),
            PayloadGeometry.Box => fixture.Box(centre, new(.31f), 1f, material, turn, transverse, gravity: 0),
            PayloadGeometry.Compound => fixture.Sphere(centre, .31f, 1f, material, transverse, Vector3.Zero, 0, 0,
                new(0, -.15f, 0), turn),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        if (kind == PayloadGeometry.Compound)
        {
            fixture.ChildCollider(payload, ColliderShapeKind.Sphere, new(0, .15f, 0), .31f);
            fixture.DeclareMassFrame(payload, new(.1f), Vector3.Zero);
        }
        if (blocked) fixture.StaticBox(centre + axis * .8f, new(1f, .001f, 1f), turn, material);
        var actuator = fixture.FiniteWork(frame, payload, 45f, 45f);
        var world = fixture.Admit();
        var candidate = new byte[world.Admitted.Length];
        var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Contact, new(1), new(0, 0f),
            new(new(1), frame, new(1), new(0, 0f), new(0f), ActivationCauseKind.Physical));
        var input = new PhysicsTickInputs([], [])
        { PhysicalRequests = [new(PhysicalRequestTarget.ForActuator(actuator), PhysicalRequestOperation.Release, occurrence)] };
        var interactions = new WorkshopPhysicsEngine().Advance(world.Admitted, candidate, input, PhysicsInteractionSnapshot.Empty);
        _ = PhysicsGpuAbi.ValidateCandidate(candidate, world.Admitted, new(1), interactions, PhysicsInteractionSnapshot.Empty);
        var work = Assert.Single(PhysicsGpuAbi.ReadFiniteWorkActuators(candidate));
        var store = Assert.Single(PhysicsGpuAbi.ReadEnergyStores(candidate));
        var body = LawState.Read(world.Scene, candidate, payload);
        Assert.Equal(blocked ? FiniteWorkRequestResult.Obstructed : FiniteWorkRequestResult.Fired, work.LastResult);
        Assert.Equal(blocked ? 0u : 1u, work.OccurrenceCount);
        if (blocked)
        {
            Assert.Equal(45f, store.Energy.Value);
            Assert.Equal(0f, store.Released.Value);
            Assert.InRange((body.Velocity - transverse).Length(), 0f, .01f);
        }
        else
        {
            // A centimetre of aim displacement over one second detects wrong-axis or lost transverse motion.
            Assert.InRange((body.Velocity - (transverse + axis * MathF.Sqrt(90f))).Length(), 0f, .01f);
            Assert.InRange(store.Released.Value - 45f, -.005f, .005f);
        }
    }

    [Fact]
    public void ReleasedPayloadReboundsFromTwoMillimetreWallWithoutExtraWork()
    {
        var fixture = new LawScene(false);
        var centre = new Vector3(0, 3, 0);
        var material = new LawMaterial(.8f, 0f, 0f, 0f);
        var frame = fixture.StaticBox(centre, new(.1f), material: material, colliderOffset: new(0, -2, 0));
        var payload = fixture.Sphere(centre, .35f, 1f, material, Vector3.Zero, Vector3.Zero, 0, 0);
        fixture.StaticBox(centre + Vector3.UnitY * 2f, new(1f, .001f, 1f), material: material);
        var actuator = fixture.FiniteWork(frame, payload, 45f, 45f);
        var world = fixture.Admit();
        var source = world.Admitted;
        var engine = new WorkshopPhysicsEngine();
        var previous = PhysicsInteractionSnapshot.Empty;
        var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Contact, new(1), new(0, 0f),
            new(new(1), frame, new(1), new(0, 0f), new(0f), ActivationCauseKind.Physical));
        var rebound = false;
        for (var tick = 0; tick < 120; tick++)
        {
            var candidate = new byte[source.Length];
            var inputs = new PhysicsTickInputs([], [])
            { PhysicalRequests = tick == 0 ? [new(PhysicalRequestTarget.ForActuator(actuator), PhysicalRequestOperation.Release, occurrence)] : [] };
            var interactions = engine.Advance(source, candidate, inputs, previous);
            _ = PhysicsGpuAbi.ValidateCandidate(candidate, source, new((ulong)tick + 1), interactions, previous);
            var body = LawState.Read(world.Scene, candidate, payload);
            rebound |= body.Velocity.Y < -1f; // actual reversed flight, not resting contact noise
            var store = Assert.Single(PhysicsGpuAbi.ReadEnergyStores(candidate));
            Assert.InRange(.5f * body.Velocity.LengthSquared(), 0f, store.Released.Value + .005f);
            Assert.Equal(1u, Assert.Single(PhysicsGpuAbi.ReadFiniteWorkActuators(candidate)).OccurrenceCount);
            source = candidate; previous = interactions;
            if (rebound) break;
        }
        Assert.True(rebound);
    }
}
