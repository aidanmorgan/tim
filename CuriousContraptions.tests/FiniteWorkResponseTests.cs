using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

public sealed class FiniteWorkResponseTests
{

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MovingRotatingOffsetCarrierPaysRelativeWorkAndReceivesTheFullReaction(bool offset)
    {
        var fixture = new LawScene(false);
        var centre = new Vector3(0f, 3f, 0f);
        var arm = offset ? new Vector3(.25f, 0f, 0f) : Vector3.Zero;
        var translation = new Vector3(.4f, 0f, .2f);
        var spin = offset ? new Vector3(0f, 0f, .4f) : Vector3.Zero;
        var frame = fixture.Sphere(centre, .1f, 2f, new(0, 0, 0, 0), translation, spin, 0f, 0f, new(0f, -2f, 0f));
        fixture.DeclareMassFrame(frame, new(2f), Vector3.Zero);
        var carried = translation + Vector3.Cross(spin, arm);
        var payload = fixture.Sphere(centre + arm, .35f, 1f, new(0, 0, 0, 0), carried, Vector3.Zero, 0f, 0f);
        var actuator = fixture.FiniteWork(frame, payload, 45f, 45f, regionCentre: arm);
        var world = fixture.Admit();
        var result = await OwnedRelease(world, actuator, payload);
        var ball = LawState.Read(world.Scene, result.World, payload);
        var carrier = LawState.Read(world.Scene, result.World, frame);
        var cross = Vector3.Cross(arm, Vector3.UnitY);
        var inverseMass = 1f + .5f + Vector3.Dot(cross, cross) / 2f;
        var impulse = MathF.Sqrt(90f / inverseMass);
        var expectedFrameSpin = spin - cross * (impulse / 2f);
        // Existing flight allowance: 1 cm/s excludes 1 cm aiming error over one second.
        Assert.InRange((ball.Velocity - carried - impulse * Vector3.UnitY).Length(), 0f, .01f);
        Assert.InRange((carrier.Velocity - translation + impulse / 2f * Vector3.UnitY).Length(), 0f, .01f);
        // The .01 rad/s angular allowance contributes at most .0025 m/s at the .25 m arm:
        // 2.5 mm over one second, within the existing 1 cm flight allowance.
        // The centered zero-spin control excludes spurious angular reaction.
        Assert.InRange((carrier.Angular - expectedFrameSpin).Length(), 0f, .01f);
        Assert.Equal(Vector3.Zero, ball.Angular);
        var relative = Vector3.Dot(ball.Velocity - carrier.Velocity - Vector3.Cross(carrier.Angular, arm), Vector3.UnitY);
        Assert.InRange(relative - inverseMass * impulse, -.01f, .01f);
        Assert.True(relative < 12f);
        var beforeEnergy = .5f * carried.LengthSquared() + translation.LengthSquared() + spin.LengthSquared();
        var afterEnergy = .5f * ball.Velocity.LengthSquared() + carrier.Velocity.LengthSquared() + carrier.Angular.LengthSquared();
        // Reuse the existing .005 J work allowance: approximately a 0.5 kg load lifted 1 mm.
        Assert.InRange(afterEnergy - beforeEnergy - 45f, -.005f, .005f);
        Assert.InRange((ball.Velocity - carried + 2f * (carrier.Velocity - translation)).Length(), 0f, 3f * .01f); // total mass times velocity allowance
        Assert.InRange((Vector3.Cross(arm, ball.Velocity - carried) + 2f * (carrier.Angular - spin)).Length(), 0f, .25f * .01f + 2f * .01f); // lever/rotational inertia weighted allowances
        Assert.Equal(world.Initial(payload).Position, ball.Position);
        Assert.Equal(world.Initial(frame).Position, carrier.Position);
        Assert.Equal(world.Initial(frame).Rotation, carrier.Rotation);
        Assert.Equal(new SimulationTick(0), result.After.Tick);
        Assert.InRange(result.After.FiniteWorkActuators[0].LastDebit.Value - 45f, -.005f, .005f);
        Assert.Equal(1u, result.After.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Equal(actuator.Value, Assert.IsType<SimulationEventRecord>(result.Event).Source.Value);
    }

    [Theory]
    [InlineData(11f)]
    [InlineData(12f)]
    [InlineData(13f)]
    public async Task EligibleIncomingCapRefusesWithoutWorkWhileBelowCapUsesOnlyTheGap(float incoming)
    {
        var fixture = new LawScene(false);
        var centre = new Vector3(0f, 3f, 0f);
        var frame = fixture.StaticBox(centre, new(.1f), colliderOffset: new(0f, -2f, 0f));
        var payload = fixture.Sphere(centre, .35f, 1f, new(0, 0, 0, 0), incoming * Vector3.UnitY, Vector3.Zero, 0f, 0f);
        var actuator = fixture.FiniteWork(frame, payload, 45f, 45f, maximumRelativeSpeed: 16f);
        var world = fixture.Admit();
        var result = await OwnedRelease(world, actuator, payload);
        var body = LawState.Read(world.Scene, result.World, payload);
        var work = result.After.FiniteWorkActuators[0];
        var store = result.After.EnergyStores[0];
        if (incoming >= 12f)
        {
            Assert.Equal(ChamberPhase.Seated, result.After.ChamberRegions[0].Phase);
            Assert.Equal(FiniteWorkClearance.Clear, work.Clearance);
            Assert.Equal(FiniteWorkRequestResult.SpeedLimited, work.LastResult);
            Assert.Equal(world.Initial(payload), body);
            Assert.Equal(45f, store.Energy.Value); Assert.Equal(0f, store.Released.Value);
            Assert.Equal(0f, work.LastDebit.Value); Assert.Equal(0u, work.OccurrenceCount);
            Assert.Null(result.Event);
        }
        else
        {
            Assert.Equal(FiniteWorkRequestResult.Fired, work.LastResult);
            Assert.InRange(body.Velocity.Y - 12f, -.01f, .01f);
            Assert.InRange(work.LastDebit.Value - 11.5f, -.005f, .005f);
            Assert.InRange(store.Energy.Value - 33.5f, -.005f, .005f);
            Assert.Equal(1u, work.OccurrenceCount);
            Assert.Equal(SimulationEventKind.FiniteWorkReleased, Assert.IsType<SimulationEventRecord>(result.Event).Kind);
        }
        Assert.Equal(result.Before.Tick, result.After.Tick);
    }

    private static async Task<(WorkshopRead Before, WorkshopRead After, byte[] World, SimulationEventRecord? Event)> OwnedRelease(
        LawWorld world, FiniteWorkActuatorId actuator, GpuBodyId payload)
    {
        var settings = WorkshopCadenceSettings.Default();
        var scene = RuntimeTestServices.Core.CreateScene(new(1), new(1), settings, world.Scene,
            RuntimeTestServices.Core.CreateActivation([], []), [], world.Scene.Bodies.ToArray().Select(body => body.Id), []);
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new EventRingPublication()), world.Scene.Document);
        var admitted = await device.Admit(scene, new(1), new(settings.Simulation, settings.Physical, new(1)), new(1));
        device.Commit(new(1)); device.DrainEvents();
        var released = await device.ApplyFiniteWork(admitted.Read, new(2), new(new(1), actuator, payload));
        device.Commit(new(2));
        var bytes = device.DrainEvents();
        SimulationEventRecord? occurrence = device.Events.WriteSequence == 0 ? null :
            SimulationEventRecord.Read(bytes.AsSpan(EventRingBuffer.HeaderBytes, EventRingBuffer.SlotBytes));
        Assert.Equal(occurrence.HasValue ? 1UL : 0UL, device.Events.WriteSequence);
        return (admitted.Read, released.Read, device.CommittedWorld.ToArray(), occurrence);
    }

    public enum ResponseCase { Free, Carrier, Coupled, Capped, Fixed, Approaching, Depleted }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TautTetherRefusesWorkWhileSlackTetherAllowsThePaidResponse(bool taut)
    {
        var fixture = new LawScene(false);
        var frame = fixture.StaticBox(new(0f, 3f, 0f), new(.1f), colliderOffset: new(0f, -2f, 0f));
        var payload = fixture.Sphere(new(0f, 3f, 0f), .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0f, 0f);
        var anchor = fixture.StaticBox(new(0f, 1f, 0f), new(.1f));
        fixture.Tensile(anchor, payload, taut ? 2f : 3f);
        var actuator = fixture.FiniteWork(frame, payload, 45f, 45f);
        var world = fixture.Admit();
        var candidate = new byte[world.Admitted.Length];
        var input = new PhysicsTickInputs([], []) { OwnedFiniteWork = new(new(1), actuator, payload) };
        var interactions = new WorkshopPhysicsEngine().ApplyFiniteWork(world.Admitted, candidate, input, PhysicsInteractionSnapshot.Empty);
        _ = PhysicsGpuAbi.ValidateCandidate(candidate, world.Admitted, new(0), interactions, PhysicsInteractionSnapshot.Empty);
        var work = Assert.Single(PhysicsGpuAbi.ReadFiniteWorkActuators(candidate));
        var store = Assert.Single(PhysicsGpuAbi.ReadEnergyStores(candidate));
        Assert.Equal(taut ? FiniteWorkRequestResult.NoResponse : FiniteWorkRequestResult.Fired, work.LastResult);
        Assert.Equal(taut ? 0u : 1u, work.OccurrenceCount);
        var body = LawState.Read(world.Scene, candidate, payload);
        if (taut)
        {
            Assert.Equal(Vector3.Zero, body.Velocity);
            Assert.Equal(45f, store.Energy.Value);
            Assert.Equal(0f, store.Released.Value);
        }
        else
        {
            Assert.InRange(body.Velocity.Y - MathF.Sqrt(90f), -.01f, .01f);
            Assert.InRange(store.Released.Value - 45f, -.005f, .005f);
        }
    }

    [Theory]
    [InlineData(ResponseCase.Free)]
    [InlineData(ResponseCase.Carrier)]
    [InlineData(ResponseCase.Coupled)]
    [InlineData(ResponseCase.Capped)]
    [InlineData(ResponseCase.Fixed)]
    [InlineData(ResponseCase.Approaching)]
    [InlineData(ResponseCase.Depleted)]
    public void PaidReleaseUsesActualResponseAndExactStore(ResponseCase kind)
    {
        var scene = new LawScene(false);
        var material = new LawMaterial(0f, 0f, 0f, 0f);
        var centre = new Vector3(0f, 3f, 0f);
        var carrier = kind == ResponseCase.Carrier;
        var frame = carrier
            ? scene.Sphere(centre, .1f, 2f, material, Vector3.Zero, Vector3.Zero, 0f, 0f, new(0f, -2f, 0f))
            : scene.StaticBox(centre, new(.1f), material: material, colliderOffset: new(0f, -2f, 0f));
        var mass = kind == ResponseCase.Approaching ? 10f : kind == ResponseCase.Capped ? .25f : 1f;
        var initial = kind == ResponseCase.Approaching ? -1f : 0f;
        var payload = scene.Sphere(centre, .35f, mass, material, new(0f, initial, 0f), Vector3.Zero, 0f, 0f);
        // The admitted prismatic permits world Z travel and locks the launch direction Y.
        var jointRotation = new Quaternion(.5f, .5f, .5f, .5f);
        var jointFrame = new ConstraintFrame(Vector3.Zero, jointRotation);
        if (kind == ResponseCase.Fixed)
            scene.Prismatic(frame, payload, jointFrame, jointFrame, -1f, 1f, 0f, 0f, ConnectedCollision.Disabled);
        var companion = default(GpuBodyId);
        if (kind == ResponseCase.Coupled)
        {
            companion = scene.Sphere(centre, .2f, 1f, material, Vector3.Zero, Vector3.Zero, 0f, 0f, new(2f, 0f, 0f));
            // This control declares a centred mass frame independently of its offset collision material.
            scene.DeclareMassFrame(companion, new(.1f), Vector3.Zero);
            scene.Prismatic(payload, companion, jointFrame, jointFrame,
                -1f, 1f, 0f, 0f, ConnectedCollision.Disabled);
        }
        var capacity = kind == ResponseCase.Approaching ? 1f : 45f;
        var stored = kind == ResponseCase.Depleted ? 0f : capacity;
        var actuator = scene.FiniteWork(frame, payload, capacity, stored);
        var world = scene.Admit();
        var source = world.Admitted;
        var candidate = new byte[source.Length];
        var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Contact, new(1), new(0, 0f),
            new(new(1), frame, new(1), new(0, 0f), new(0f), ActivationCauseKind.Physical));
        var inputs = new PhysicsTickInputs([], kind is ResponseCase.Fixed or ResponseCase.Coupled ? [false] : [])
        {
            PhysicalRequests = [new(PhysicalRequestTarget.ForActuator(actuator), PhysicalRequestOperation.Release, occurrence)]
        };
        var engine = new WorkshopPhysicsEngine();
        var interactions = engine.Advance(source, candidate, inputs, PhysicsInteractionSnapshot.Empty);
        _ = PhysicsGpuAbi.ValidateCandidate(candidate, source, new(1), interactions, PhysicsInteractionSnapshot.Empty);
        var release = Assert.Single(PhysicsGpuAbi.ReadFiniteWorkActuators(candidate));
        var store = Assert.Single(PhysicsGpuAbi.ReadEnergyStores(candidate));
        var body = LawState.Read(world.Scene, candidate, payload);
        if (kind is ResponseCase.Fixed or ResponseCase.Depleted)
        {
            Assert.Equal(kind == ResponseCase.Fixed ? FiniteWorkRequestResult.NoResponse : FiniteWorkRequestResult.Uncharged, release.LastResult);
            Assert.Equal(0u, release.OccurrenceCount);
            Assert.Equal(0f, release.LastDebit.Value);
            Assert.Equal(stored, store.Energy.Value);
            Assert.Equal(Vector3.Zero, body.Velocity);
            return;
        }
        Assert.Equal(FiniteWorkRequestResult.Fired, release.LastResult);
        Assert.Equal(1u, release.OccurrenceCount);
        Assert.Equal(payload, release.LastPayload);
        // .01 m/s excludes a 1 cm aim displacement over a one-second flight; .005 J excludes
        // free energy sufficient to raise a 0.5 kg load by 1 mm (existing mechanical-work allowance).
        const float speedAllowance = .01f, energyAllowance = .005f;
        var expected = carrier ? MathF.Sqrt(60f) : kind == ResponseCase.Coupled ? MathF.Sqrt(45f) :
            MathF.Min(12f, MathF.Sqrt(initial * initial + 2f * capacity / mass));
        var expectedDebit = kind == ResponseCase.Capped ? .5f * mass * 12f * 12f : capacity;
        Assert.InRange(body.Velocity.Y - expected, -speedAllowance, speedAllowance);
        Assert.InRange(release.LastDebit.Value - expectedDebit, -energyAllowance, energyAllowance);
        Assert.InRange(store.Released.Value - expectedDebit, -energyAllowance, energyAllowance);
        Assert.InRange(store.Energy.Value - (capacity - expectedDebit), -energyAllowance, energyAllowance);
        if (kind == ResponseCase.Coupled)
            Assert.InRange(LawState.Read(world.Scene, candidate, companion).Velocity.Y - expected, -speedAllowance, speedAllowance);
        if (carrier)
        {
            var reaction = LawState.Read(world.Scene, candidate, frame);
            Assert.InRange(reaction.Velocity.Y + expected / 2f, -speedAllowance, speedAllowance);
            Assert.InRange(body.Velocity.Y + 2f * reaction.Velocity.Y, -speedAllowance, speedAllowance);
        }
        // No input latch is retained by the physical evaluator itself.
        var next = new byte[source.Length];
        _ = engine.Advance(candidate, next, inputs with { PhysicalRequests = [] }, interactions);
        Assert.Equal(1u, Assert.Single(PhysicsGpuAbi.ReadFiniteWorkActuators(next)).OccurrenceCount);
    }

    [Fact]
    public void OverlappingRegionsReevaluateThePayloadAfterAnEarlierRelease()
    {
        var scene = new LawScene(false);
        var centre = new Vector3(0f, 3f, 0f);
        var frame = scene.StaticBox(centre, new(.1f), colliderOffset: new(0f, -2f, 0f));
        var payload = scene.Sphere(centre, .35f, 1f, new(0f, 0f, 0f, 0f), Vector3.Zero, Vector3.Zero, 0f, 0f);
        var first = scene.FiniteWork(frame, payload, 45f, 45f);
        var second = scene.FiniteWork(frame, payload, 45f, 45f);
        var world = scene.Admit();
        var candidate = new byte[world.Admitted.Length];
        var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Contact, new(1), new(0, 0f),
            new(new(1), frame, new(1), new(0, 0f), new(0f), ActivationCauseKind.Physical));
        var inputs = new PhysicsTickInputs([], [])
        {
            PhysicalRequests =
            [
                new(PhysicalRequestTarget.ForActuator(first), PhysicalRequestOperation.Release, occurrence),
                new(PhysicalRequestTarget.ForActuator(second), PhysicalRequestOperation.Release, occurrence)
            ]
        };
        var interactions = new WorkshopPhysicsEngine().Advance(world.Admitted, candidate, inputs, PhysicsInteractionSnapshot.Empty);
        _ = PhysicsGpuAbi.ValidateCandidate(candidate, world.Admitted, new(1), interactions, PhysicsInteractionSnapshot.Empty);
        var actuators = PhysicsGpuAbi.ReadFiniteWorkActuators(candidate);
        Assert.Equal(FiniteWorkRequestResult.Fired, actuators[0].LastResult);
        Assert.Equal(1u, actuators[0].OccurrenceCount);
        Assert.Equal(FiniteWorkRequestResult.Unseated, actuators[1].LastResult);
        Assert.Equal(0u, actuators[1].OccurrenceCount);
        Assert.Equal(0f, actuators[1].LastDebit.Value);
        var stores = PhysicsGpuAbi.ReadEnergyStores(candidate);
        Assert.Equal(45f, stores[1].Energy.Value);
        Assert.Equal(0f, stores[1].Released.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InstantaneousContactPaysForPayloadSpinAndAnIndirectDrivenRotor(bool friction)
    {
        var fixture = new LawScene(false);
        var centre = new Vector3(0, 3, 0);
        var material = new LawMaterial(0f, 0f, friction ? 1f : 0f, 0f);
        // A 1 mm seated overlap selects the existing closed contact row; exact touching uses
        // the ordinary solver's speculative slop allowance and is not this rigid-row oracle.
        var frame = fixture.StaticBox(centre, new(.1f, 2f, 2f), material: material, colliderOffset: new(-.449f, 0, 0));
        var payload = fixture.Sphere(centre, .35f, 1f, material, Vector3.Zero, Vector3.Zero, 0f, 0f);
        var rotorCentre = centre - 3 * Vector3.UnitY;
        var rotor = fixture.Sphere(rotorCentre, .1f, 1f, material, Vector3.Zero, Vector3.Zero, 0f, 0f);
        fixture.DeclareMassFrame(rotor, Vector3.One);
        var shaft = fixture.Revolute(frame, rotor, new(-3 * Vector3.UnitY, Quaternion.Identity),
            new(Vector3.Zero, Quaternion.Identity), -MathF.PI, MathF.PI, 0f, travel: RevoluteTravel.Continuous);
        fixture.DrivenSurface(frame, shaft, Vector3.UnitY, .2f);
        var actuator = fixture.FiniteWork(frame, payload, 45f, 45f,
            regionRotation: Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 4f));
        var world = fixture.Admit();
        var candidate = new byte[world.Admitted.Length];
        var engine = new WorkshopPhysicsEngine(world.Scene.DrivenSurfaces.ToArray());
        var interactions = engine.ApplyFiniteWork(world.Admitted, candidate,
            new PhysicsTickInputs([], [false]) { OwnedFiniteWork = new(new(1), actuator, payload) },
            PhysicsInteractionSnapshot.Empty);
        _ = PhysicsGpuAbi.ValidateCandidate(candidate, world.Admitted, new(0), interactions, PhysicsInteractionSnapshot.Empty);
        var release = Assert.Single(PhysicsGpuAbi.ReadFiniteWorkActuators(candidate));
        Assert.Equal(FiniteWorkRequestResult.Fired, release.LastResult);
        var ball = LawState.Read(world.Scene, candidate, payload);
        var shaftBody = LawState.Read(world.Scene, candidate, rotor);
        // Independent sticking-contact oracle: normal reaction holds X; tangent impulse Jy
        // is shared by translation, sphere spin and the remote rotor through surface travel.
        const float inertia = .4f * .35f * .35f, lever = .449f - .1f; // sphere COM to the authored wall face
        var fraction = friction ? 1f / (1f + lever * lever / inertia + .2f * .2f) : 0f;
        var tangentImpulse = MathF.Sqrt(90f / (1f - fraction));
        var frictionImpulse = tangentImpulse * fraction;
        var expectedSpeed = tangentImpulse - frictionImpulse;
        Assert.InRange(ball.Velocity.X, -.01f, .01f); // wall prevents a centimetre of penetration over one second
        Assert.InRange(ball.Velocity.Y - expectedSpeed, -.01f, .01f);
        Assert.InRange(MathF.Abs(ball.Angular.Z) - lever * frictionImpulse / inertia, -.02f, .02f); // <1 degree over half a second
        Assert.InRange(MathF.Abs(shaftBody.Angular.Z) - .2f * frictionImpulse, -.02f, .02f);
        var kinetic = .5f * ball.Velocity.LengthSquared() + .5f * inertia * ball.Angular.LengthSquared() +
            .5f * shaftBody.Angular.LengthSquared();
        Assert.InRange(kinetic - release.LastDebit.Value, -.005f, .005f); // paid work cannot create a 0.5 kg / 1 mm lift for free
        Assert.InRange(release.LastDebit.Value - 45f, -.005f, .005f);
        Assert.Equal(world.Initial(payload).Position, ball.Position);
        Assert.Equal(world.Initial(rotor).Position, shaftBody.Position);
        if (!friction) Assert.Equal(Vector3.Zero, shaftBody.Angular);
    }

}
