using System.Collections.Immutable;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using Xunit;
using CuriousContraptions.Simulation;
using Shape = CuriousContraptions.Abstractions.ColliderShapeKind;

namespace CuriousContraptions.Tests;

public sealed class DrivenSurfaceBindingTests
{
    private static readonly PartId Bed = new("independent_surface_bed");
    private static readonly PartId Cargo = new("surface_cargo");

    private static BundleEntry BedEntry(float speed = 6f, BoxFace face = BoxFace.PositiveY, bool reordered = false)
    {
        var motor = TestBundle.Entry(new("motor"));
        var passive = motor.Mechanical!.Hinges[0] with
        {
            BaseFrame = new(new(0, -1, -1), Quaternion.Identity),
            Drive = null, FrictionTorque = MechanicalValue.Fixed(0f),
            MovingBody = motor.Mechanical.Hinges[0].MovingBody with
            {
                MassFrame = motor.Mechanical.Hinges[0].MovingBody.MassFrame with { Gravity = Vector3.Zero }
            }
        };
        var driven = passive with
        {
            BaseFrame = new(new(0, -1, 1), Quaternion.Identity),
            Drive = new(MechanicalValue.FromParameter("speed"), MechanicalValue.Fixed(3f))
        };
        return motor with
        {
            Metadata = motor.Metadata with { Id = Bed.Value, Parameters =
                [new("speed", ParameterUnit.RadianPerSecond, ParameterKind.Real, -10f, 10f, speed, [], RunEdit: ParameterRunEdit.RunControl)],
                PaletteOrder = 201 },
            Colliders = [Box(new(2, .15f, .5f), Vector3.Zero)],
            ColliderBindings = [],
            Presentation = new(3f, PresentationAxes.None, [],
                [new("Other", new(1), Vector3.Zero), new("Source", new(2), Vector3.Zero)], [], []),
            Footprint = new(3, 2, 0, 2),
            Mechanical = motor.Mechanical with
            {
                Hinges = reordered ? [driven, passive] : [passive, driven],
                DrivenSurfaces = [new(new(1), new(0), new(0), face,
                    new(CoordinateKind.Revolute, reordered ? 0 : 1), Vector3.UnitX, MechanicalValue.Fixed(.2f))]
            }
        };
    }

    private static ColliderDescriptor Box(Vector3 size, Vector3 offset) =>
        new(Shape.Box, size, offset, Quaternion.Identity, new(0f, .6f, 0f, .1f), 0f);

    private static BundleEntry CargoEntry()
    {
        var weight = TestBundle.Entry(new("weight"));
        return weight with
        {
            Metadata = weight.Metadata with { Id = Cargo.Value, Parameters = [], PaletteOrder = 202 },
            Colliders = [Box(new(.15f), Vector3.Zero)], ColliderBindings = [], Ports = [],
            Presentation = new(.3f, PresentationAxes.None, [], [], [], []),
            Mechanical = weight.Mechanical! with
            {
                MassFrame = MassFrameDeclaration.Constant(.5f, Vector3.Zero, new(.0075f), Quaternion.Identity, 0f, new(0, -9.81f, 0))
            }
        };
    }

    private static WorkshopConstruction Construction(bool loaded = true) =>
        new(new(2), WorkshopCadenceSettings.Default(),
            loaded ? new(PlacedParts.Battery(new(1), -4, 4, 0, enabled: ElectricalEnable.Enabled),
                WorkshopInput.Generic(RuntimeTestServices.Bundle, Bed, new(2), 0, 4, 0, 0, 0, 0, 1),
                WorkshopInput.Generic(RuntimeTestServices.Bundle, Cargo, new(3), 0, 4.30f, 0, 0, 0, 0, 1)) :
                new(PlacedParts.Battery(new(1), -4, 4, 0, enabled: ElectricalEnable.Enabled),
                WorkshopInput.Generic(RuntimeTestServices.Bundle, Bed, new(2), 0, 4, 0, 0, 0, 0, 1)),
            Connections: new(new WorkshopConnection(new(1), PortId.Supply, new(2), PortId.PowerIn, WorkshopConnectionDomain.Electrical)));

    [Theory]
    [InlineData(6f, BoxFace.PositiveY, false)]
    [InlineData(-6f, BoxFace.PositiveY, false)]
    [InlineData(6f, BoxFace.PositiveY, true)]
    [InlineData(0f, BoxFace.PositiveY, false)]
    [InlineData(6f, BoxFace.NegativeY, false)]
    [InlineData(6f, BoxFace.PositiveZ, false)]
    public async Task OnlyTheDeclaredFaceCarriesCargoInItsSelectedCoordinatesDirection(float speed, BoxFace face, bool reordered)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var bed = BedEntry(speed, face, reordered);
        var cargo = CargoEntry();
        Assert.Empty(BundleValidator.Validate([new("bed", bed), new("cargo", cargo)]));
        using var physics = DeclaredPhysics.Scoped([bed, cargo]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Bed, bed.Ports), (Cargo, cargo.Ports)]));
        var construction = Construction();
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();
        await harness.Advance(120);
        Assert.True(harness.Committed.Bodies.TryGet(new(3), out var load));
        var displacement = (load.Body.Cell.X + load.Body.Local.X) / 16f;
        // A 10 cm motion over one second distinguishes transport from resting contact jitter.
        if (face == BoxFace.PositiveY && speed != 0f)
            Assert.True(displacement * MathF.Sign(speed) > .1f, $"Cargo did not follow the selected shaft: x={displacement}.");
        else
            Assert.InRange(displacement, -.05f, .05f); // 5 cm clearance to the adjoining region.
        Assert.InRange((load.Body.Cell.Y + load.Body.Local.Y) / 16f, 4.25f, 4.35f); // Supported on the 4.15 m face, not falling through it.
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void ASourceOutsideItsDeclaredJointTableIsRefused(int source)
    {
        var bed = BedEntry();
        bed = bed with { Mechanical = bed.Mechanical! with
        {
            DrivenSurfaces = [bed.Mechanical.DrivenSurfaces[0] with { Source = new(CoordinateKind.Revolute, source) }]
        } };
        Assert.Contains(BundleValidator.Validate([new("bed", bed)]),
            error => error.Code == BundleErrorCode.OutOfRange && error.Detail.Contains("DrivenSurfaces"));
    }
    [Fact]
    public async Task IndependentCarriersReverseOnlyTheSelectedLoadAndRestoreAuthoredBindings()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        await using var replay = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var bed = BedEntry(); var cargo = CargoEntry();
        using var physics = DeclaredPhysics.Scoped([bed, cargo]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Bed, bed.Ports), (Cargo, cargo.Ports)]));
        var initial = Construction();
        var construction = initial with
        {
            Instances = new(initial.Instances.Concat(new IWorkshopInstance[]
            {
                WorkshopInput.Generic(RuntimeTestServices.Bundle, Bed, new(4), 0, 4, 3, 0, 0, 0, 1),
                WorkshopInput.Generic(RuntimeTestServices.Bundle, Cargo, new(5), 0, 4.3f, 3, 0, 0, 0, 1)
            }).ToArray()),
            Connections = initial.Connections.With(new(new(1), PortId.Supply, new(4), PortId.PowerIn, WorkshopConnectionDomain.Electrical))
        };
        await harness.Require(WorkshopCommandKind.Construct, construction);
        var authored = harness.Committed;
        await harness.Run(); await harness.Advance(60);
        Assert.True(harness.Committed.Bodies.TryGet(new(3), out var first));
        Assert.True(harness.Committed.Bodies.TryGet(new(5), out var second));
        Assert.True(first.Body.Velocity.X > .2f && second.Body.Velocity.X > .2f);
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(new(2), RunControlLane.RevoluteDriveTarget, RunControlValue.Of(-6f)));
        await harness.Advance(60);
        Assert.True(harness.Committed.Bodies.TryGet(new(3), out first));
        Assert.True(harness.Committed.Bodies.TryGet(new(5), out second));
        Assert.True(first.Body.Velocity.X < -.2f && second.Body.Velocity.X > .2f,
            "Changing one shaft must reverse only its own contacting load within one second.");
        await harness.Require(WorkshopCommandKind.Pause);
        var paused = harness.Committed.Tick;
        await harness.Require(WorkshopCommandKind.Step);
        Assert.Equal(paused.Value + 1, harness.Committed.Tick.Value);
        await harness.Require(WorkshopCommandKind.Resume); await harness.Advance(12);
        await harness.Reset();
        for (var index = 0; index < authored.Bodies.Count; index++)
        {
            var body = authored.Bodies[index];
            Assert.True(harness.Committed.Bodies.TryGet(body.Body.Id, out var reset));
            Assert.Equal(body.Body.Id, reset.Body.Id);
            Assert.Equal(body.Body.Cell, reset.Body.Cell);
            Assert.Equal(body.Body.Local, reset.Body.Local);
            Assert.Equal(body.Body.Velocity, reset.Body.Velocity);
            Assert.Equal(body.Rotation, reset.Rotation);
            Assert.Equal(body.AngularVelocity, reset.AngularVelocity);
        }
        await harness.Require(WorkshopCommandKind.Save);
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements.Concat([Bed, Cargo]).ToHashSet(),
            WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new(construction, new(42))));
        await replay.Require(WorkshopCommandKind.Construct, loaded.Construction);
        var surfaces = replay.Simulation.Scene.Physics.DrivenSurfaces.ToArray();
        Assert.Equal(2, surfaces.Length);
        foreach (var surface in surfaces)
        {
            Assert.Equal(BoxFace.PositiveY, surface.Face);
            Assert.Equal(.2f, surface.TravelPerRadian);
            Assert.Equal(Vector3.UnitX, surface.LocalDirection);
        }
        await replay.Run(); await replay.Advance(60);
        Assert.True(replay.Committed.Bodies.TryGet(new(3), out first));
        Assert.True(first.Body.Velocity.X > .2f, "Save/load must restore the authored positive source.");
    }

    public enum InvalidSurface { ZeroId, DuplicateId, DuplicateFace, Carrier, Collider, SourceKind, Face, Shape, Tangent, ZeroTravel, NonfiniteTravel }

    [Theory]
    [InlineData(InvalidSurface.ZeroId)]
    [InlineData(InvalidSurface.DuplicateId)]
    [InlineData(InvalidSurface.DuplicateFace)]
    [InlineData(InvalidSurface.Carrier)]
    [InlineData(InvalidSurface.Collider)]
    [InlineData(InvalidSurface.SourceKind)]
    [InlineData(InvalidSurface.Face)]
    [InlineData(InvalidSurface.Shape)]
    [InlineData(InvalidSurface.Tangent)]
    [InlineData(InvalidSurface.ZeroTravel)]
    [InlineData(InvalidSurface.NonfiniteTravel)]
    public void InvalidSurfaceBindingsReceiveTypedBundleRefusal(InvalidSurface defect)
    {
        var bed = BedEntry();
        var row = bed.Mechanical!.DrivenSurfaces[0];
        row = defect switch
        {
            InvalidSurface.ZeroId => row with { Id = default },
            InvalidSurface.Carrier => row with { Carrier = new(3) },
            InvalidSurface.Collider => row with { Collider = new(1) },
            InvalidSurface.SourceKind => row with { Source = new(CoordinateKind.Prismatic, 0) },
            InvalidSurface.Face => row with { Face = (BoxFace)255 },
            InvalidSurface.Tangent => row with { LocalDirection = Vector3.UnitY },
            InvalidSurface.ZeroTravel => row with { TravelPerRadian = MechanicalValue.Fixed(0f) },
            InvalidSurface.NonfiniteTravel => row with { TravelPerRadian = MechanicalValue.Fixed(float.PositiveInfinity) },
            _ => row
        };
        var rows = defect switch
        {
            InvalidSurface.DuplicateId => new[] { row, row with { Face = BoxFace.NegativeY } },
            InvalidSurface.DuplicateFace => new[] { row, row with { Id = new(2) } },
            _ => new[] { row }
        };
        bed = bed with { Mechanical = bed.Mechanical with { DrivenSurfaces = rows } };
        if (defect == InvalidSurface.Shape) bed = bed with { Colliders = [bed.Colliders[0] with { ShapeKind = Shape.Sphere }] };
        var errors = BundleValidator.Validate([new("bed", bed)]);
        var expected = defect is InvalidSurface.ZeroId or InvalidSurface.DuplicateId or InvalidSurface.DuplicateFace
            ? BundleErrorCode.DuplicateId : defect is InvalidSurface.Carrier or InvalidSurface.Collider
                ? BundleErrorCode.UnresolvedReference : BundleErrorCode.OutOfRange;
        Assert.Contains(errors, error => error.Code == expected && error.Detail.Contains("DrivenSurfaces"));
    }

    [Fact]
    public async Task ChildCarrierUsesItsRotatedColliderAndIndependentSourceFrame()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var bed = BedEntry(); var cargo = CargoEntry();
        var child = bed.Mechanical!.Hinges[0];
        child = child with
        {
            BaseFrame = new(Vector3.Zero, Quaternion.Identity),
            MovingBody = child.MovingBody with
            {
                MassFrame = MassFrameDeclaration.Constant(1000f, Vector3.Zero, new(1000f), Quaternion.Identity, 0f, Vector3.Zero),
                Colliders = [Box(new(.15f, 2f, .5f), Vector3.Zero) with
                    { Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f) }]
            }
        };
        var source = bed.Mechanical.Hinges[1] with
        {
            BaseFrame = new(new(0, -1, 1), Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f))
        };
        bed = bed with
        {
            Colliders = [Box(new(.1f), new(0, -1.5f, 0))],
            Mechanical = bed.Mechanical with
            {
                Hinges = [child, source],
                DrivenSurfaces = [bed.Mechanical.DrivenSurfaces[0] with { Carrier = new(1), Face = BoxFace.PositiveX }]
            }
        };
        Assert.Empty(BundleValidator.Validate([new("bed", bed), new("cargo", cargo)]));
        using var physics = DeclaredPhysics.Scoped([bed, cargo]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Bed, bed.Ports), (Cargo, cargo.Ports)]));
        await harness.Require(WorkshopCommandKind.Construct, Construction());
        await harness.Run(); await harness.Advance(120);
        Assert.True(harness.Committed.Bodies.TryGet(new(3), out var load));
        Assert.True((load.Body.Cell.X + load.Body.Local.X) / 16f > .1f,
            "A child carrier with a rotated box must carry cargo along its declared tangent.");
        Assert.InRange((load.Body.Cell.Y + load.Body.Local.Y) / 16f, 4.25f, 4.35f);
    }

    [Fact]
    public void SharedBodyJacobianIncludesCrossTermsAndReciprocalImpulseWork()
    {
        var carrier = UnitBody(); var cargo = UnitBody(); var rotor = UnitBody();
        carrier.Vx = 1f; carrier.Wz = 2f; rotor.Wz = 3f;
        var patch = new ContactPatch();
        patch.Points.Add(new ContactPoint { A = carrier, B = cargo });
        patch.Linear[0] = Vector3.UnitX;
        patch.AngularA[0] = -Vector3.UnitZ;
        patch.SurfaceA = new()
        {
            Collider = new() { Body = carrier }, Source = new() { A = carrier, B = rotor, Qa = Quaternion.Identity },
            Face = BoxFace.PositiveY, LocalDirection = Vector3.UnitX, TravelPerRadian = .2f
        };
        patch.Jacobian.Build(patch);
        // Unit masses/inertias: K = 1 + 1 + (-1-.2)^2 + .2^2.
        // Slip = 1 + (-1.2)*2 + .2*3 = -.8; impulse work = lambda*v + lambda^2*K/2.
        Assert.InRange(patch.Jacobian.Mass(0, 0), 3.48f - 1e-5f, 3.48f + 1e-5f);
        Assert.InRange(patch.Jacobian.Velocity(0), -.8f - 1e-5f, -.8f + 1e-5f);
        var before = Kinetic(carrier) + Kinetic(cargo) + Kinetic(rotor);
        patch.Jacobian.Apply(new(2f, 0, 0));
        var change = Kinetic(carrier) + Kinetic(cargo) + Kinetic(rotor) - before;
        Assert.InRange(change, 5.36f - 1e-5f, 5.36f + 1e-5f);
        Assert.InRange(carrier.Wz, -.4f - 1e-5f, -.4f + 1e-5f);
        Assert.InRange(rotor.Wz, 3.4f - 1e-5f, 3.4f + 1e-5f);
    }

    private static SimBody UnitBody() => new()
    {
        Motion = RigidMotionKind.Dynamic, Mass = 1f, InvMass = 1f, InvI = Vector3.One,
        Qw = 1f, Principal = Quaternion.Identity, PrincipalX = Vector3.UnitX,
        PrincipalY = Vector3.UnitY, PrincipalZ = Vector3.UnitZ
    };

    private static float Kinetic(SimBody body) =>
        .5f * (body.LinearVelocity.LengthSquared() + body.AngularVelocity.LengthSquared());

    [Theory]
    [InlineData(-1f, true)]
    [InlineData(1f, false)]
    // Equal support on the ordinary half can hold a spanning load through static friction.
    [InlineData(0f, false)]
    public async Task AdjacentOrdinaryFaceDoesNotAcquireItsNeighboursDrive(float x, bool transported)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var bed = BedEntry(-6f); var cargo = CargoEntry();
        bed = bed with { Colliders = [Box(new(1, .15f, .5f), new(-1, 0, 0)), Box(new(1, .15f, .5f), new(1, 0, 0))] };
        using var physics = DeclaredPhysics.Scoped([bed, cargo]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Bed, bed.Ports), (Cargo, cargo.Ports)]));
        var initial = Construction(false);
        var construction = initial with { Instances = new(initial.Instances.Append(
            WorkshopInput.Generic(RuntimeTestServices.Bundle, Cargo, new(3), x, 4.3f, 0, 0, 0, 0, 1)).ToArray()) };
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run(); await harness.Advance(120);
        Assert.True(harness.Committed.Bodies.TryGet(new(3), out var load));
        var travel = (load.Body.Cell.X + load.Body.Local.X) / 16f - x;
        if (transported) Assert.True(travel < -.1f, $"Selected or spanning load did not travel: {travel}.");
        else Assert.InRange(travel, -.05f, .05f); // Ordinary region retains its 5 cm functional clearance.
        Assert.InRange((load.Body.Cell.Y + load.Body.Local.Y) / 16f, 4.25f, 4.35f);
    }

    [Theory]
    [InlineData(6f, false)]
    [InlineData(-6f, true)]
    public async Task TwoDrivenFacesContributeTheirRelativeMotion(float upperSpeed, bool transported)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var lower = BedEntry(); var upper = BedEntry(upperSpeed, BoxFace.NegativeY);
        var upperId = new PartId("upper_surface");
        upper = upper with
        {
            Metadata = upper.Metadata with { Id = upperId.Value, PaletteOrder = 203 },
            Footprint = new(3, 3, 0, 2),
            Mechanical = upper.Mechanical! with
            {
                KinematicMode = KinematicMode.Dynamic,
                MassFrame = MassFrameDeclaration.Constant(5f, Vector3.Zero, new(10f), Quaternion.Identity, 0f, new(0, -9.81f, 0))
            }
        };
        using var physics = DeclaredPhysics.Scoped([lower, upper]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Bed, lower.Ports), (upperId, upper.Ports)]));
        var initial = Construction(false);
        var construction = initial with
        {
            Instances = new(initial.Instances.Append(WorkshopInput.Generic(RuntimeTestServices.Bundle, upperId, new(3), 0, 4.3f, 0, 0, 0, 0, 1)).ToArray()),
            Connections = initial.Connections.With(new(new(1), PortId.Supply, new(3), PortId.PowerIn, WorkshopConnectionDomain.Electrical))
        };
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run(); await harness.Advance(120);
        Assert.True(harness.Committed.Bodies.TryGet(new(3), out var load));
        var travel = (load.Body.Cell.X + load.Body.Local.X) / 16f;
        if (transported) Assert.True(travel > .1f, $"Opposing surface velocities must transport their carrier: {travel}.");
        else Assert.InRange(travel, -.05f, .05f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CoastingSourcePaysForCargoMotionAndUnloadedControlKeepsItsSpin(bool loaded)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var bed = BedEntry(); var cargo = CargoEntry();
        using var physics = DeclaredPhysics.Scoped([bed, cargo]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Bed, bed.Ports), (Cargo, cargo.Ports)]));
        var construction = Construction(false);
        if (loaded) construction = construction with { Instances = new(construction.Instances.Append(
            WorkshopInput.Generic(RuntimeTestServices.Bundle, Cargo, new(3), 0, 4.55f, 0, 0, 0, 0, 1)).ToArray()) };
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run(); await harness.Advance(18);
        var shaft = construction.Instances.Identities[new(2)].Hinges[1].Body;
        var unrelated = construction.Instances.Identities[new(2)].Hinges[0].Body;
        Assert.True(harness.Committed.Bodies.TryGet(shaft, out var before));
        Assert.True(before.AngularVelocity.Z > 5f);
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(new(1), RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        var initialEnergy = MechanicalEnergy(harness.Committed, harness.Simulation.Scene.Physics);
        for (var tick = 0; tick < 120; tick++)
        {
            await harness.Advance(1);
            // 0.05 J is less than lifting this 0.5 kg cargo by 1.1 cm; excludes useful unprovided work.
            Assert.True(MechanicalEnergy(harness.Committed, harness.Simulation.Scene.Physics) <= initialEnergy + .05f,
                "The unpowered contact must not create usable mechanical energy.");
        }
        Assert.True(harness.Committed.Bodies.TryGet(shaft, out var after));
        Assert.True(harness.Committed.Bodies.TryGet(unrelated, out var idle));
        Assert.InRange(idle.AngularVelocity.Z, -.1f, .1f);
        if (loaded)
        {
            Assert.True(after.AngularVelocity.Z < before.AngularVelocity.Z - .3f, "Cargo must react on the selected finite-inertia shaft.");
            Assert.True(harness.Committed.Bodies.TryGet(new(3), out var load));
            Assert.True(load.Body.Velocity.X > .2f);
        }
        else Assert.InRange(after.AngularVelocity.Z - before.AngularVelocity.Z, -.1f, .1f);
    }

    private static float MechanicalEnergy(WorkshopRead read, IPhysicsSceneDeclaration scene)
    {
        var energy = 0f;
        foreach (var body in scene.Bodies)
        {
            if (body.Motion != RigidMotionKind.Dynamic) continue;
            Assert.True(read.Bodies.TryGet(body.Id, out var state));
            var q = state.Rotation; var principal = body.MassProperties.PrincipalFrame;
            var rotation = new Quaternion(q.X, q.Y, q.Z, q.W) * new Quaternion(principal.X, principal.Y, principal.Z, principal.W);
            var w = state.AngularVelocity;
            var local = Vector3.Transform(new(w.X, w.Y, w.Z), Quaternion.Conjugate(rotation));
            var v = state.Body.Velocity;
            var position = new Vector3(state.Body.Cell.X + state.Body.Local.X, state.Body.Cell.Y + state.Body.Local.Y,
                state.Body.Cell.Z + state.Body.Local.Z) / 16f;
            energy += .5f * body.Mass.Value * (v.X * v.X + v.Y * v.Y + v.Z * v.Z) +
                .5f * (body.MassProperties.X.Moment * local.X * local.X + body.MassProperties.Y.Moment * local.Y * local.Y +
                    body.MassProperties.Z.Moment * local.Z * local.Z) -
                body.Mass.Value * Vector3.Dot(new(body.Gravity.X, body.Gravity.Y, body.Gravity.Z), position);
        }
        return energy;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(12)]
    public async Task SurfaceCollectionsSurviveCompilationWireAndRuntimeGrowth(int count)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var bed = BedEntry();
        bed = bed with
        {
            Colliders = Enumerable.Range(0, Math.Max(count, 1)).Select(i => Box(new(.3f, .15f, .3f), new(i - 6f, 0, 0))).ToArray(),
            Mechanical = bed.Mechanical! with
            {
                DrivenSurfaces = Enumerable.Range(0, count).Select(i => bed.Mechanical!.DrivenSurfaces[0] with
                    { Id = new((uint)i + 1), Collider = new(i) }).ToArray()
            }
        };
        using var physics = DeclaredPhysics.Scoped([bed]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Bed, bed.Ports)]));
        var construction = Construction(false);
        await harness.Require(WorkshopCommandKind.Construct, construction);
        var compiled = CompiledSceneFixture.Compile(construction);
        Assert.Equal(count, compiled.Physics.DrivenSurfaces.Length);
        Assert.Equal(count, compiled.Physics.DrivenSurfaces.ToArray().Select(row => row.Collider).Distinct().Count());
        foreach (var surface in compiled.Physics.DrivenSurfaces)
        {
            Assert.Equal(construction.Instances.Identities[new(2)].Hinges[1].Joint, surface.Source);
            Assert.Equal(BoxFace.PositiveY, surface.Face);
            Assert.Equal(.2f, surface.TravelPerRadian);
        }
        await harness.Run(); await harness.Advance(60);
        Assert.True(harness.Committed.Bodies.TryGet(construction.Instances.Identities[new(2)].Hinges[1].Body, out var shaft));
        Assert.True(shaft.AngularVelocity.Z > 5f);
    }

    public enum WireDefect { Identity, Carrier, Collider, Source, Face, Direction, ZeroTravel, NonfiniteTravel, OverflowTravel, MissingCollection }

    [Theory]
    [InlineData(WireDefect.Identity)]
    [InlineData(WireDefect.Carrier)]
    [InlineData(WireDefect.Collider)]
    [InlineData(WireDefect.Source)]
    [InlineData(WireDefect.Face)]
    [InlineData(WireDefect.Direction)]
    [InlineData(WireDefect.ZeroTravel)]
    [InlineData(WireDefect.NonfiniteTravel)]
    [InlineData(WireDefect.OverflowTravel)]
    [InlineData(WireDefect.MissingCollection)]
    public async Task MalformedSurfaceCommandAndResponseLeaveTheInstalledWorldUntouched(WireDefect defect)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var bed = BedEntry(); var cargo = CargoEntry();
        using var physics = DeclaredPhysics.Scoped([bed, cargo]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Bed, bed.Ports), (Cargo, cargo.Ports)]));
        var construction = Construction();
        await harness.Require(WorkshopCommandKind.Construct, construction);
        var before = harness.Committed;
        var command = harness.Command(WorkshopCommandKind.Construct, construction with { Revision = new(3) });
        var request = WorkshopWireFlatBuffers.EncodeCommand(command);
        CorruptSurface(request, 11, defect);
        var reason = defect == WireDefect.MissingCollection ? WireFaultReason.VerifierRejected : WireFaultReason.OutOfRange;
        Assert.Equal(reason, Assert.Throws<WireFaultException>(() => harness.Router.Dispatch(request)).Reason);
        var response = WorkshopWireFlatBuffers.EncodeResponse(harness.Latest with { Scene = harness.Simulation.Scene });
        CorruptSurface(response, 27, defect);
        Assert.Equal(reason, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(response)).Reason);
        Assert.Equal(before.Revision, harness.Committed.Revision);
        Assert.Equal(before.Epoch, harness.Committed.Epoch);
        Assert.Equal(construction, harness.Construction);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Dispatch(command)).Result.Outcome);
        await harness.Run(); await harness.Advance(60);
        Assert.True(harness.Committed.Bodies.TryGet(new(3), out var cargoRead));
        Assert.True(cargoRead.Body.Velocity.X > .2f);
    }

    private static void CorruptSurface(byte[] bytes, int sceneField, WireDefect defect)
    {
        var buffer = new Google.FlatBuffers.ByteBuffer(bytes);
        var root = 4 + buffer.GetInt(4); // Size-prefixed current schema.
        var sceneSlot = Field(buffer, root, sceneField);
        var scene = sceneSlot + buffer.GetInt(sceneSlot);
        if (defect == WireDefect.MissingCollection)
        {
            buffer.PutUshort(scene - buffer.GetInt(scene) + 74, 0);
            return;
        }
        var vectorSlot = Field(buffer, scene, 35);
        var first = vectorSlot + buffer.GetInt(vectorSlot) + 4;
        var row = first + buffer.GetInt(first);
        var field = defect switch
        {
            WireDefect.Identity => 0, WireDefect.Carrier => 1, WireDefect.Collider => 2,
            WireDefect.Face => 3, WireDefect.Source => 4, WireDefect.Direction => 5, _ => 6
        };
        var slot = Field(buffer, row, field);
        switch (defect)
        {
            case WireDefect.Identity: buffer.PutUlong(slot, 0); break;
            case WireDefect.Carrier: buffer.PutUlong(slot, 3); break; // Existing cargo cannot own this collider.
            case WireDefect.Collider:
            case WireDefect.Source: buffer.PutUlong(slot, ulong.MaxValue); break;
            case WireDefect.Face: buffer.PutByte(slot, 255); break;
            case WireDefect.Direction: buffer.PutFloat(slot, 0f); break;
            case WireDefect.ZeroTravel: buffer.PutFloat(slot, 0f); break;
            case WireDefect.NonfiniteTravel: buffer.PutFloat(slot, float.NaN); break;
            case WireDefect.OverflowTravel: buffer.PutFloat(slot, 1e30f); break;
            default: throw new ArgumentOutOfRangeException(nameof(defect));
        }
    }

    private static int Field(Google.FlatBuffers.ByteBuffer buffer, int table, int field)
    {
        var vtable = table - buffer.GetInt(table);
        var offset = buffer.GetUshort(vtable + 4 + 2 * field);
        Assert.NotEqual(0, offset);
        return table + offset;
    }

    [Theory]
    [InlineData(ParameterRunEdit.BuildOnly, true)]
    [InlineData(ParameterRunEdit.RunControl, false)]
    public void CompiledTravelParametersCannotAlsoBeRunControlled(ParameterRunEdit edit, bool accepted)
    {
        var bed = BedEntry();
        bed = bed with
        {
            Metadata = bed.Metadata with { Parameters = bed.Metadata.Parameters.Append(
                new ParameterDescriptor("travel", ParameterUnit.MetrePerRadian, ParameterKind.Real, .1f, .3f, .2f, [], RunEdit: edit)).ToArray() },
            Mechanical = bed.Mechanical! with
            { DrivenSurfaces = [bed.Mechanical.DrivenSurfaces[0] with { TravelPerRadian = MechanicalValue.FromParameter("travel") }] }
        };
        var errors = BundleValidator.Validate([new("bed", bed)]);
        if (accepted) Assert.Empty(errors);
        else Assert.Contains(errors, error => error.Code == BundleErrorCode.OutOfRange && error.Detail.Contains("DrivenSurfaces"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ABlockedLoadReactsOnTheSelectedShaft(bool blocked)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var bed = BedEntry(); var cargo = CargoEntry();
        var hinges = bed.Mechanical!.Hinges.ToArray();
        hinges[1] = hinges[1] with { Drive = new(MechanicalValue.FromParameter("speed"), MechanicalValue.Fixed(.1f)) };
        bed = bed with
        {
            Mechanical = bed.Mechanical with { Hinges = hinges },
            Colliders = blocked ? [bed.Colliders[0], Box(new(.15f, .3f, .5f), new(.3f, .3f, 0))] : bed.Colliders
        };
        using var physics = DeclaredPhysics.Scoped([bed, cargo]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Bed, bed.Ports), (Cargo, cargo.Ports)]));
        var construction = Construction(blocked);
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run(); await harness.Advance(240);
        Assert.True(harness.Committed.Bodies.TryGet(construction.Instances.Identities[new(2)].Hinges[1].Body, out var shaft));
        if (blocked)
        {
            Assert.InRange(shaft.AngularVelocity.Z, -1f, 1f); // Less than 1/6 free speed: the load must stall this torque-limited source.
            Assert.True(harness.Committed.Bodies.TryGet(new(3), out var load));
            Assert.InRange((load.Body.Cell.X + load.Body.Local.X) / 16f, -.05f, .05f);
        }
        else Assert.True(shaft.AngularVelocity.Z > 5f);
    }

    [Theory]
    [InlineData(ParameterUnit.MetrePerRadian, "m/rad")]
    [InlineData(ParameterUnit.RadianPerSecond, "rad/s")]
    [InlineData(ParameterUnit.NewtonMetre, "N·m")]
    public void ConfigurationFormatsTravelAndSourceQuantities(ParameterUnit unit, string expected) =>
        Assert.Equal(expected, Workshop.ParameterUnits(unit));

    [Fact]
    public void ConfigurationRefusesAnUndefinedUnit() =>
        Assert.Throws<System.Diagnostics.UnreachableException>(() => Workshop.ParameterUnits((ParameterUnit)255));

    [Fact]
    public async Task PopulatedBundleRoundTripPreservesSignedBindingsAndRunsDecodedDeclarations()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var bed = BedEntry(); var cargo = CargoEntry();
        var hinges = bed.Mechanical!.Hinges.ToArray();
        hinges[0] = hinges[0] with { MovingBody = hinges[0].MovingBody with
            { Colliders = [Box(new(.1f), Vector3.Zero), Box(new(.1f), new(0, 0, -.3f))] } };
        bed = bed with { Mechanical = bed.Mechanical with
        {
            Hinges = hinges,
            DrivenSurfaces =
            [
                bed.Mechanical.DrivenSurfaces[0] with { Id = new(7), TravelPerRadian = MechanicalValue.Fixed(-.2f) },
                new(new(11), new(1), new(1), BoxFace.NegativeZ, new(CoordinateKind.Revolute, 0), Vector3.UnitX, MechanicalValue.Fixed(.3f))
            ]
        } };
        var entries = PartRegistry.Read(RuntimeTestServices.Bundle, BundleSchema.WriteEntries([bed, cargo]));
        var decoded = Assert.Single(entries, entry => entry.Metadata.Id == Bed.Value);
        var rows = decoded.Mechanical!.DrivenSurfaces;
        Assert.Equal(2, rows.Count);
        Assert.Equal(new LocalDrivenSurfaceId(7), rows[0].Id);
        Assert.Equal(new PackageBodyIndex(0), rows[0].Carrier);
        Assert.Equal(new BodyColliderIndex(0), rows[0].Collider);
        Assert.Equal(BoxFace.PositiveY, rows[0].Face);
        Assert.Equal(new CoordinateReference(CoordinateKind.Revolute, 1), rows[0].Source);
        Assert.Equal(Vector3.UnitX, rows[0].LocalDirection);
        Assert.Equal(MechanicalValue.Fixed(-.2f), rows[0].TravelPerRadian);
        Assert.Equal(new LocalDrivenSurfaceId(11), rows[1].Id);
        Assert.Equal(new PackageBodyIndex(1), rows[1].Carrier);
        Assert.Equal(new BodyColliderIndex(1), rows[1].Collider);
        Assert.Equal(BoxFace.NegativeZ, rows[1].Face);
        Assert.Equal(new CoordinateReference(CoordinateKind.Revolute, 0), rows[1].Source);
        Assert.Equal(Vector3.UnitX, rows[1].LocalDirection);
        Assert.Equal(MechanicalValue.Fixed(.3f), rows[1].TravelPerRadian);
        using var physics = DeclaredPhysics.Scoped(entries);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend(entries.Select(entry => (new PartId(entry.Metadata.Id), entry.Ports))));
        await harness.Require(WorkshopCommandKind.Construct, Construction());
        await harness.Run(); await harness.Advance(120);
        Assert.True(harness.Committed.Bodies.TryGet(new(3), out var load));
        Assert.True((load.Body.Cell.X + load.Body.Local.X) / 16f < -.1f,
            "The decoded negative travel must reverse cargo despite the positive shaft drive.");
        Assert.InRange((load.Body.Cell.Y + load.Body.Local.Y) / 16f, 4.25f, 4.35f);
    }


    internal static BundleEntry ParameterBedEntry()
    {
        var bed = BedEntry();
        return bed with
        {
            Metadata = bed.Metadata with { Parameters = bed.Metadata.Parameters.Append(
                new ParameterDescriptor("travel", ParameterUnit.MetrePerRadian, ParameterKind.Real, .1f, .3f, .2f, [],
                    EditorPolicy: ParameterEditorPolicy.Reject)).ToArray() },
            Mechanical = bed.Mechanical! with { DrivenSurfaces =
                [bed.Mechanical.DrivenSurfaces[0] with { TravelPerRadian = MechanicalValue.FromParameter("travel") }] }
        };
    }

    [Fact]
    public async Task AuthoredTravelOverridesChangeCompiledTravelAndActualCargoTransport()
    {
        await using var slow = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        await using var fast = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entries = PartRegistry.Read(RuntimeTestServices.Bundle, BundleSchema.WriteEntries([ParameterBedEntry(), CargoEntry()]));
        using var physics = DeclaredPhysics.Scoped(entries);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend(entries.Select(entry => (new PartId(entry.Metadata.Id), entry.Ports))));
        async Task<float> Run(WorkshopIntegrationHarness harness, float travel)
        {
            var construction = Construction();
            var bed = (WorkshopGenericInstance)construction.Instances.Single(instance => instance.Id == new GpuBodyId(2));
            var parameters = bed.Parameters.ToDictionary(); parameters[new("travel")] = travel;
            construction = construction.WithInstance(RuntimeTestServices.Bundle, bed with { Parameters = new(parameters) });
            // A fresh installation has revision 2; the authored edit remains in its instance values.
            construction = construction with { Revision = new(2) };
            await harness.Require(WorkshopCommandKind.Construct, construction);
            Assert.Equal(travel, Assert.Single(harness.Simulation.Scene.Physics.DrivenSurfaces.ToArray()).TravelPerRadian);
            await harness.Run(); await harness.Advance(120);
            Assert.True(harness.Committed.Bodies.TryGet(new(3), out var cargo));
            Assert.InRange((cargo.Body.Cell.Y + cargo.Body.Local.Y) / 16f, 4.25f, 4.35f);
            return (cargo.Body.Cell.X + cargo.Body.Local.X) / 16f;
        }
        var slowX = await Run(slow, .1f); var fastX = await Run(fast, .3f);
        Assert.True(slowX > .1f && fastX > slowX + .2f,
            "Different authored travel must produce a visibly different load advance (20 cm) in one second.");
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(12, true)]
    [InlineData(12, false)]
    public async Task LaterSurfaceRowsDriveTheirOwnCargoWhileAnOrdinaryFaceDoesNot(int count, bool selected)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var bed = BedEntry(); var cargo = CargoEntry();
        bed = bed with
        {
            Colliders = Enumerable.Range(0, count).Select(i => Box(new(.3f, .15f, .3f), new(i - 6f, 0, 0))).ToArray(),
            Mechanical = bed.Mechanical! with { DrivenSurfaces = Enumerable.Range(0, count).Select(i =>
                bed.Mechanical.DrivenSurfaces[0] with { Id = new((uint)i + 1), Collider = new(i),
                    Face = selected ? BoxFace.PositiveY : BoxFace.NegativeY }).ToArray() }
        };
        using var physics = DeclaredPhysics.Scoped([bed, cargo]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Bed, bed.Ports), (Cargo, cargo.Ports)]));
        var x = count - 7f;
        var construction = Construction().WithInstance(RuntimeTestServices.Bundle, WorkshopInput.Generic(RuntimeTestServices.Bundle, Cargo, new(3), x, 4.3f, 0, 0, 0, 0, 1)) with { Revision = new(2) };
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run(); await harness.Advance(40);
        Assert.True(harness.Committed.Bodies.TryGet(new(3), out var load));
        var travel = (load.Body.Cell.X + load.Body.Local.X) / 16f - x;
        if (selected) Assert.True(travel > .1f, "The last installed row must carry its contacting load at least 10 cm.");
        else Assert.InRange(travel, -.05f, .05f);
    }

}
