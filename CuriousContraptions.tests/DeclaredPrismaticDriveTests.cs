using CuriousContraptions.Abstractions;
using System.Buffers.Binary;
using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>C-Gate (Stories 9A.19 + 9A.21): the supplied prismatic drive is declaration data the generic engine law executes, the
/// Blocked reading needs something in the way, and cold startup and ordinary engine reuse preserve lifecycle state. Every case steps the
/// production engine; gate cases go through the production compiler and WorkshopSimulation.</summary>
public sealed class DeclaredPrismaticDriveTests
{
    private const float H = 1f / 480f; // the canonical physical substep
    private static WorkshopConnection Power(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.Supply, target, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);

    /// <summary>One substep state of a body: its centre of mass and linear velocity, from the committed motion pieces.</summary>
    private readonly record struct Substep(Vector3 Centre, Vector3 Velocity);

    /// <summary>Every committed motion piece of <paramref name="owner"/> in one tick's payload, in substep order.</summary>
    private static List<Substep> Pieces(ReadOnlySpan<byte> motion, ulong owner)
    {
        var result = new List<Substep>();
        var count = BinaryPrimitives.ReadUInt32LittleEndian(motion);
        for (var i = 0; i < count; i++)
        {
            var piece = motion.Slice(PhysicsMotionRead.HeaderBytes + i * PhysicsMotionRead.PieceBytes, PhysicsMotionRead.PieceBytes).ToArray();
            if (BinaryPrimitives.ReadUInt64LittleEndian(piece.AsSpan(PhysicsMotionRead.OwnerOffset)) != owner) continue;
            float F(int at) => BinaryPrimitives.ReadSingleLittleEndian(piece.AsSpan(at));
            int I(int at) => BinaryPrimitives.ReadInt32LittleEndian(piece.AsSpan(at));
            const int cell = PhysicsMotionRead.CellOffset, remainder = PhysicsMotionRead.CentreRemainderOffset;
            result.Add(new(new((I(cell) + F(remainder)) / 16f, (I(cell + 4) + F(remainder + 4)) / 16f, (I(cell + 8) + F(remainder + 8)) / 16f),
                new(F(PhysicsMotionRead.VelocityOffset), F(PhysicsMotionRead.VelocityOffset + 4), F(PhysicsMotionRead.VelocityOffset + 8))));
        }
        return result;
    }

    private static ReadOnlySpan<byte> Motion(byte[] world)
    {
        var layout = PhysicsGpuAbi.Layout(world);
        return world.AsSpan(layout.MotionOffset, PhysicsMotionRead.ByteLength(layout.Dynamic));
    }

    /// <summary>The drive force each substep applied, inferred from the committed motion: m·a plus the slider's own spring and
    /// damper and minus its weight along the axis (no contact acts on the moving body), with the axial speed it acted at.
    /// Substeps touching a stop are skipped, where the stop rows act too.</summary>
    private static List<(float Force, float Speed, float Coordinate)> Inferred(IReadOnlyList<Substep> path, Vector3 origin, Vector3 axis,
        float mass, float stiffness, float damping, float gravityAlongAxis, float lower, float upper)
    {
        var result = new List<(float, float, float)>();
        for (var i = 1; i < path.Count; i++)
        {
            float X(Substep s) => Vector3.Dot(s.Centre - origin, axis);
            float V(Substep s) => Vector3.Dot(s.Velocity, axis);
            var (x0, x1, v0, v1) = (X(path[i - 1]), X(path[i]), V(path[i - 1]), V(path[i]));
            if (MathF.Min(x0, x1) < lower + 2e-3f || MathF.Max(x0, x1) > upper - 2e-3f) continue;
            var force = mass * (v1 - v0) / H + stiffness * (x0 + x1) / 2f + damping * (v0 + v1) / 2f - mass * gravityAlongAxis;
            result.Add((force, v0, x1));
        }
        return result;
    }

    // ---- 9A.19: the package declares the gate's drive, and the generic law executes it ----

    [Fact]
    public void TheGatePackageDeclaresItsDriveAndItsPowerInRouteBindsThatSlider()
    {
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 3, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(2), 3, 3, 0);
        var scene = WorkshopPhysicsCompiler.Compile(new(new(2), WorkshopCadenceSettings.Default(), new(gate, battery))
            { Connections = new(Power(battery.Id, gate.Id)) }, new(1, 2));
        var declared = TestBundle.Entry(PartId.PoweredGate).Mechanical!.Sliders.Single().Drive!.Value;
        Assert.Equal(new PrismaticDriveDescriptor(MechanicalValue.Fixed(1.55f), MechanicalValue.Fixed(2.8f), MechanicalValue.Fixed(14f),
            MechanicalValue.Fixed(60f), MechanicalValue.Fixed(120f)), declared);
        var joint = Assert.Single(scene.Prismatics.ToArray());
        Assert.Equal(new PrismaticDriveDeclaration(joint.Id, 1.55f, 2.8f, 14f, 60f, 120f), Assert.Single(scene.PrismaticDrives.ToArray()));
        Assert.Equal(joint.Id, Assert.Single(scene.SupplyGraph.Nodes.ToArray().Single(node => node.Owner == gate.Id).Consumers));
        // The admitted world carries the same drive; a slider without one (the springboard) carries none.
        var world = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        Assert.Equal(scene.PrismaticDrives[0], PhysicsGpuAbi.ReadPrismaticDrive(world, 0));
        var spring = WorkshopInput.Generic(PartId.Springboard, new(1), 0, 3, 0, 0, 0, 0, 1);
        var passive = WorkshopPhysicsCompiler.Compile(new(new(2), WorkshopCadenceSettings.Default(), new(spring)), new(1, 2));
        Assert.Single(passive.Prismatics.ToArray());
        Assert.Empty(passive.PrismaticDrives.ToArray());
        Assert.Null(PhysicsGpuAbi.ReadPrismaticDrive(PhysicsGpuAbi.Admission(passive, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1))), 0));
    }

    [Fact]
    public async Task ThePoweredGateOpensToItsTargetWithinItsDeclaredSpeedEffortAndPowerOnEverySubstep()
    {
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 3, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(2), 3, 3, 0);
        var reads = await WorkshopPhysicsEngineTests.Run(new(new(2), WorkshopCadenceSettings.Default(), new(gate, battery))
            { Connections = new(Power(battery.Id, gate.Id)) }, 240);
        var blade = new WorkshopInstances(gate, battery).Identities[gate.Id].Sliders[0].Body;
        var path = reads.SelectMany(read => Pieces(read.Motion!.Bytes, blade.Value)).ToList();
        var forces = Inferred(path, new(0, 3, 0), Vector3.UnitY, 1f, 14f, 4f, -9.81f, 0f, 1.55f);
        Assert.True(forces.Count > 60, $"the stroke is observed on {forces.Count} substeps");
        Assert.All(path, s => Assert.True(s.Velocity.Y <= 2.8f + 1e-3f, $"speed {s.Velocity.Y} within 2.8 m/s"));
        Assert.All(forces, f => Assert.True(MathF.Abs(f.Force) <= 60f + .5f, $"effort {f.Force} N within 60 N"));
        Assert.All(forces, f => Assert.True(MathF.Abs(f.Force * f.Speed) <= 120f + 1f, $"power {f.Force * f.Speed} W within 120 W"));
        // Measured 107.5 W at the 2.8 m/s cruise (spring 14·x, damper 4·v and weight held): the unloaded gate stays under its
        // 120 W cap, so the newly enforced cap leaves its trajectory unchanged.
        Assert.InRange(forces.Max(f => f.Force * f.Speed), 60f, 120f);
        var open = reads.FindIndex(read => read.Presentation.Actuators[0].Coordinate >= 1.55f - 1e-3f);
        Assert.True(open > 0 && open < 180, $"open within 180 ticks (tick {open + 1})");
        Assert.All(reads.Skip(open), read => Assert.InRange(read.Presentation.Actuators[0].Coordinate, 1.55f - 1e-3f, 1.55f + 1e-3f));
        Assert.Equal(2.8f, path.Max(s => s.Velocity.Y), 5e-2f); // the profile reaches its declared cruise speed (measured 2.777)
    }

    /// <summary>A synthetic 1 kg slider whose every drive value differs from the gate's and binds: effort while accelerating from
    /// rest, power while cruising, the stopping profile at its own target short of the upper limit (stiffness 3 ≠ acceleration 8).</summary>
    private static (LawWorld World, GpuBodyId Moving) Synthetic(bool drive)
    {
        var scene = new LawScene();
        var frame = scene.StaticBox(new(0, 5, 0), new(.2f, .2f, .2f));
        var moving = scene.Box(new(0, 5, 0), new(.25f, .25f, .25f), 1f, LawMaterial.Domino);
        var joint = scene.Prismatic(frame, moving, ConstraintFrame.Identity, ConstraintFrame.Identity, -.2f, 2f, 3f, .5f, ConnectedCollision.Disabled);
        if (drive) scene.Drive(joint, 1.2f, 1.6f, 8f, 16f, 12f);
        return (scene.Admit(), moving);
    }

    [Fact]
    public void ASyntheticDriveReachesItsOwnTargetOnItsOwnProfileWithItsEffortAndPowerBinding()
    {
        var (world, moving) = Synthetic(drive: true);
        var path = new List<Substep>();
        var final = world.Step(480, (_, state) => path.AddRange(Pieces(Motion(state.World), moving.Value)), supplied: true)[moving];
        var forces = Inferred(path, new(0, 5, 0), Vector3.UnitY, 1f, 3f, .5f, LawScene.Gravity, -.2f, 2f);
        Assert.All(forces, f => Assert.True(MathF.Abs(f.Force) <= 16f + .5f, $"effort {f.Force} N within 16 N"));
        Assert.All(forces, f => Assert.True(MathF.Abs(f.Force * f.Speed) <= 12f + .5f, $"power {f.Force * f.Speed} W within 12 W"));
        // Effort binds from rest: the declared 8 m/s² is not reached ((16 − 9.81)/1 ≈ 6.2 m/s²).
        var early = path.Take(24).Zip(path.Skip(1).Take(24), (a, b) => (b.Velocity.Y - a.Velocity.Y) / H).Max();
        Assert.InRange(early, 5.5f, 6.6f);
        Assert.True(forces.Take(12).All(f => f.Force > 15.5f), "the effort bound binds while accelerating from rest");
        // Power binds while cruising: 12 W at 10–13 N holds the speed well under the declared 1.6 m/s.
        var peak = path.Max(s => s.Velocity.Y);
        Assert.InRange(peak, .8f, 1.3f);
        Assert.Contains(forces, f => MathF.Abs(f.Force * f.Speed) > 11.5f);
        // Its own stopping profile, √(2·8·d): 0.08 m short of the target it still cruises (power-limited, about 0.88 m/s); a profile
        // on its stiffness, √(2·3·d) = 0.69 m/s, would already be braking there.
        var approach = path.First(s => s.Centre.Y - 5f >= 1.2f - .08f);
        Assert.True(approach.Velocity.Y > .8f, $"still cruising 0.08 m short of the target at {approach.Velocity.Y} m/s");
        // Its own target, short of its upper limit, at rest there.
        Assert.Equal(1.2f, final.Position.Y - 5f, 3e-3f);
        Assert.True(MathF.Abs(final.Velocity.Y) < 1e-2f, $"at rest on the target, {final.Velocity.Y}");
        // Held without chatter: over every substep of its last 240 ticks (the second half of the run) the axial speed stays below
        // 1 mm/s. The stopping command is capped at |d|/h, the speed that closes the remaining distance in one substep h; without that
        // cap the infinitely steep √(2a|d|) at the target overshoots each substep and the hold cycles at about 1 cm/s.
        var held = path.Skip(path.Count / 2).Max(s => MathF.Abs(s.Velocity.Y));
        Assert.True(held < 1e-3f, $"held on the target: every late substep under 1 mm/s, max {held}");
        Assert.True(path.Max(s => s.Centre.Y - 5f) <= 1.2f + 5e-3f, "the stopping profile does not overshoot");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OneOwnerRouteDrivesBothChildrenWithinTheirOwnPhysicalBounds(bool enabled)
    {
        var fixture = new LawScene(bench: false);
        var frame = fixture.StaticBox(new(0,5,0), new(.1f));
        var battery = fixture.StaticBox(new(4,5,0), new(.1f));
        var moving = new GpuBodyId[2];
        var joints = new PrismaticConstraintId[2];
        float[] targets = [1.2f,.6f], speeds = [1.6f,.8f], efforts = [16f,20f], powers = [12f,8f];
        for (var i=0;i<2;i++)
        {
            var x = i==0 ? -2f : 2f;
            moving[i] = fixture.Box(new(x,5,0),new(.1f),1,LawMaterial.Domino);
            joints[i] = fixture.Prismatic(frame,moving[i],new(new(x,0,0),Quaternion.Identity),
                ConstraintFrame.Identity,-.2f,2,3,.5f,ConnectedCollision.Disabled);
            fixture.Drive(joints[i],targets[i],speeds[i],8,efforts[i],powers[i]);
        }
        var baseScene = fixture.Admit().Scene;
        var source = new ElectricalSourceDeclaration(new(1000),battery,new(1000),new(100),1,
            enabled ? ElectricalEnable.Enabled : ElectricalEnable.Disabled);
        SupplyNodeDeclaration[] nodes = [new(battery,SupplyNodeKind.Source,source.Id),
            new(frame,SupplyNodeKind.Consumer,Consumers:[..joints])];
        SupplyEdge[] edges = [new(battery,frame,WorkshopSocket.PowerIn)];
        var graph = new CompiledSupplyGraph(nodes,edges,[source],baseScene.Prismatics,baseScene.PrismaticDrives,baseScene.Bodies);
        Assert.Throws<ArgumentException>(() => new CompiledSupplyGraph(
            [nodes[0],nodes[1] with { Consumers=[joints[0],joints[0]] }],edges,[source],baseScene.Prismatics,baseScene.PrismaticDrives,baseScene.Bodies));
        var scene = new PhysicsSceneDeclaration(baseScene.Document,1001,baseScene.Bodies,baseScene.Colliders,
            baseScene.Materials,[],[],prismatics:baseScene.Prismatics,prismaticDrives:baseScene.PrismaticDrives,
            electricalSources:[source],supplyNodes:nodes,supplyEdges:edges);
        var current = PhysicsGpuAbi.Admission(scene,new(1),new(SimulationCadence.Hz120,PhysicalStepProfile.Canonical480Hz,new(1)));
        var candidate = new byte[current.Length];
        var engine = new WorkshopPhysicsEngine();
        var interactions = PhysicsInteractionSnapshot.Empty;
        var paths = new[] { new List<Substep>(),new List<Substep>() };
        for (var tick=0;tick<480;tick++)
        {
            var input = PhysicsTickInputs.Compose(graph,2,new([source.Enabled]),_=>false);
            interactions = engine.Advance(current,candidate,input,interactions);
            (current,candidate)=(candidate,current);
            for(var i=0;i<2;i++) paths[i].AddRange(Pieces(Motion(current),moving[i].Value));
        }
        for(var i=0;i<2;i++)
        {
            var final = LawState.Read(scene,current,moving[i]);
            Assert.Equal(enabled ? targets[i] : -.2f,final.Position.Y-5,3e-3f);
            if (!enabled) { Assert.True(paths[i].Max(point=>point.Centre.Y)<=5.001f); continue; }
            Assert.True(paths[i].Max(point=>point.Velocity.Y)<=speeds[i]+1e-3f);
            var forces = Inferred(paths[i],new(i==0 ? -2 : 2,5,0),Vector3.UnitY,1,3,.5f,LawScene.Gravity,-.2f,2);
            Assert.True(forces.Count>60);
            Assert.All(forces,force=>Assert.True(MathF.Abs(force.Force)<=efforts[i]+.5f));
            Assert.All(forces,force=>Assert.True(MathF.Abs(force.Force*force.Speed)<=powers[i]+.5f));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AValidatedPackageCompilesBothDrivenChildrenAndBothFollowTheirOwnTargets(bool enabled)
    {
        // Fresh process-like assembly statics preserve production's one-bundle lifetime. No runtime
        // registry replacement or solver hook; the public scenario installs through the real boundary.
        var assemblyPath = typeof(DeclaredPrismaticDriveTests).Assembly.Location;
        var context = new NativeBundleContext(Path.GetDirectoryName(assemblyPath)!);
        try
        {
            var assembly = context.LoadFromAssemblyPath(assemblyPath);
            var scenario = assembly.GetType(typeof(NativeTwoDriveScenario).FullName!)!
                .GetMethod(nameof(NativeTwoDriveScenario.Run))!;
            scenario.Invoke(null, [TestBundle.Json, enabled]);
        }
        finally { context.Unload(); }
    }

    private sealed class NativeBundleContext(string directory) : System.Runtime.Loader.AssemblyLoadContext(isCollectible: true)
    {
        protected override System.Reflection.Assembly? Load(System.Reflection.AssemblyName name)
        {
            if (name.Name is not { } simple || !simple.StartsWith("CuriousContraptions", StringComparison.Ordinal)) return null;
            return LoadFromAssemblyPath(Path.Combine(directory, simple + ".dll"));
        }
    }

    [Fact]
    public void ASuppliedBoxWithTheGatesBladeAndLimitsButNoDeclaredDriveNeverActuates()
    {
        LawWorld Blade(bool drive)
        {
            var scene = new LawScene();
            var frame = scene.StaticBox(new(0, 5, 0), new(.4f, .1f, .4f), colliderOffset: new(0, 2.5f, 0));
            var blade = scene.Box(new(0, 5, 0), new(.06f, .72f, .72f), 1f, LawMaterial.Domino);
            var joint = scene.Prismatic(frame, blade, ConstraintFrame.Identity, ConstraintFrame.Identity, 0f, 1.55f, 14f, 4f, ConnectedCollision.Disabled);
            if (drive) scene.Drive(joint, 1.55f, 2.8f, 14f, 60f, 120f);
            return scene.Admit();
        }
        var passive = Blade(drive: false);
        var bladeId = passive.Scene.Prismatics[0].BodyB;
        passive.Step(180, (tick, state) => Assert.True(state[bladeId].Position.Y - 5f <= 1e-3f,
            $"tick {tick}: a closed route without a declared drive moves nothing ({state[bladeId].Position.Y - 5f})"), supplied: true);
        // Control: the same box with the gate's declared drive opens on the same supply.
        var driven = Blade(drive: true);
        Assert.Equal(1.55f, driven.Step(180, supplied: true)[bladeId].Position.Y - 5f, 1e-3f);
        Assert.True(driven.Step(180, supplied: false)[bladeId].Position.Y - 5f <= 1e-3f, "and stays closed unsupplied");
    }

    [Fact]
    public void AdmissionRefusesAnInvalidDriveAndARouteToAnUndrivenSlider()
    {
        PhysicsSceneDeclaration Admit(Func<PrismaticDriveDeclaration, PrismaticDriveDeclaration> change, bool dynamicMoving = true)
        {
            var scene = new LawScene(bench: false);
            var frame = scene.StaticBox(new(0, 5, 0), new(.2f, .2f, .2f));
            var moving = dynamicMoving ? scene.Box(new(0, 5, 0), new(.25f, .25f, .25f), 1f, LawMaterial.Domino)
                : scene.StaticBox(new(0, 5, 0), new(.25f, .25f, .25f));
            var joint = dynamicMoving
                ? scene.Prismatic(frame, moving, ConstraintFrame.Identity, ConstraintFrame.Identity, -.2f, 2f, 3f, .5f, ConnectedCollision.Disabled)
                : scene.Prismatic(scene.Box(new(0, 8, 0), new(.25f, .25f, .25f), 1f, LawMaterial.Domino), moving, ConstraintFrame.Identity,
                    ConstraintFrame.Identity, -.2f, 2f, 3f, .5f, ConnectedCollision.Disabled);
            scene.Drive(joint, 1.2f, 1.6f, 8f, 16f, 12f);
            var drive = new PrismaticDriveDeclaration(joint, 1.2f, 1.6f, 8f, 16f, 12f);
            return scene.Admit().Scene is var admitted && change(drive) == drive ? admitted
                : new(new(1, 2), admitted.NextIdentity, admitted.Bodies, admitted.Colliders, admitted.Materials, [], [],
                    prismatics: admitted.Prismatics, prismaticDrives: [change(drive)]);
        }
        Assert.Single(Admit(drive => drive).PrismaticDrives.ToArray());
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { Target = 2.5f }));     // beyond the upper limit
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { Target = -.3f }));    // below the lower limit
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { MaxSpeed = 0f }));
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { MaxSpeed = 65f }));   // beyond the envelope's 64 m/s
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { StoppingAcceleration = -1f }));
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { MaxEffort = float.NaN }));
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { MaxPower = float.PositiveInfinity }));
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { Joint = new(999) }));
        Assert.Throws<ArgumentException>(() => Admit(drive => drive, dynamicMoving: false)); // the drive moves a static body
        // A route may drive only a slider that declares a drive: the springboard's passive slider is never a consumer.
        var spring = WorkshopInput.Generic(PartId.Springboard, new(1), 0, 3, 0, 0, 0, 0, 1);
        var passive = WorkshopPhysicsCompiler.Compile(new(new(2), WorkshopCadenceSettings.Default(), new(spring)), new(1, 2));
        Assert.Throws<ArgumentException>(() => new CompiledSupplyGraph([new(spring.Id, SupplyNodeKind.Consumer, Consumers: [passive.Prismatics[0].Id])],
            [], [], passive.Prismatics, passive.PrismaticDrives, passive.Bodies));
    }

    [Fact]
    public void TheCommittedWorldKeepsItsDriveAndAdmitsOnlyABinaryContactLane()
    {
        var (world, _) = Synthetic(drive: true);
        var layout = PhysicsGpuAbi.Layout(world.Admitted);
        var record = layout.PrismaticOffset(0);
        Assert.Equal(new PrismaticDriveDeclaration(world.Scene.Prismatics[0].Id, 1.2f, 1.6f, 8f, 16f, 12f), PhysicsGpuAbi.ReadPrismaticDrive(world.Admitted, 0));
        Assert.False(PhysicsGpuAbi.ReadPrismaticMovingContact(world.Admitted, 0));
        byte[] Changed(int offset, uint value)
        {
            var copy = (byte[])world.Admitted.Clone();
            BinaryPrimitives.WriteUInt32LittleEndian(copy.AsSpan(record + offset), value);
            return copy;
        }
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadPrismatic(Changed(PhysicsGpuAbi.PrismaticMovingContactOffset, 2), 0));
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadPrismatic(Changed(PhysicsGpuAbi.PrismaticDriveKindOffset, 2), 0));
        Assert.True(PhysicsGpuAbi.ReadPrismaticMovingContact(Changed(PhysicsGpuAbi.PrismaticMovingContactOffset, 1), 0));
        // A drive-less record must carry zero drive values; a driven one must keep the declared values valid.
        var (passive, _) = Synthetic(drive: false);
        var undriven = (byte[])passive.Admitted.Clone();
        BinaryPrimitives.WriteSingleLittleEndian(undriven.AsSpan(record + PhysicsGpuAbi.PrismaticDriveOffset), 1f);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadPrismatic(undriven, 0));
        var unbounded = (byte[])world.Admitted.Clone();
        BinaryPrimitives.WriteSingleLittleEndian(unbounded.AsSpan(record + PhysicsGpuAbi.PrismaticDriveOffset), 3f); // target beyond the limit
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadPrismaticDrive(unbounded, 0));
    }

    // ---- Blocked: something must be in the way (CAT-051 Q3, coordinator decision 2) ----

    [Theory]
    [InlineData(180.0, 9.81f / 14f)]          // inverted: the blade hangs at the spring-gravity equilibrium, 0.70 m open
    [InlineData(135.0, 9.81f * .70710677f / 14f)] // tilted: 0.50 m
    public async Task AnUnpoweredInvertedOrTiltedMountRestingInItsInteriorWithNothingInTheWayIsNotBlocked(double degrees, float equilibrium)
    {
        var half = degrees * Math.PI / 360;
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 8, 0, 0, 0, (float)(Math.Sin(half)), (float)(Math.Cos(half)));
        var reads = await WorkshopPhysicsEngineTests.Run(new(new(2), WorkshopCadenceSettings.Default(), new(gate)), 720);
        var drives = reads.Select(read => read.Presentation.Actuators[0]).ToArray();
        var rest = drives[^1];
        // Recorded for CAT-051 Q1 (fail-closed spring): such a mount rests open, short of both stops, and nothing touches it.
        Assert.Equal(equilibrium, rest.Coordinate, 2e-3f);
        Assert.True(MathF.Abs(rest.Speed) < ActuatorDriveRead.RestSpeed, $"at rest, {rest.Speed}");
        Assert.All(drives, drive =>
        {
            Assert.False(drive.MovingContact, "nothing touches the blade");
            Assert.False(drive.Blocked, $"not blocked: {drive}");
        });
        // The kinematic part of the predicate alone would read Blocked here; only the contact term tells them apart.
        Assert.True(rest.Coordinate > rest.Lower + ActuatorDriveRead.StopSlop && rest.Coordinate < rest.Upper - ActuatorDriveRead.StopSlop);
    }

    /// <summary>Review F4: the moving-contact lane covers every substep of the tick, not only the last. A ball bounces off a sprung
    /// plate; on every tick where it struck the plate in an earlier substep and had left by the last, the lane still reads 1.</summary>
    [Fact]
    public void TheMovingContactLaneRecordsAContactInAnySubstepOfTheTick()
    {
        var scene = new LawScene();
        var frame = scene.StaticBox(new(0, 2, 0), new(.6f, .05f, .6f), colliderOffset: new(0, -.4f, 0));
        var plate = scene.Box(new(0, 2, 0), new(.5f, .05f, .5f), 1f, new(1f, 0f, .1f, 0f));
        scene.Prismatic(frame, plate, ConstraintFrame.Identity, ConstraintFrame.Identity, -.25f, 0f, 400f, .2f, ConnectedCollision.Disabled);
        var ball = scene.Sphere(new(0, 2.6f, 0), .1f, .2f, new(1f, 0f, .1f, 0f), new(0, -4f, 0), default, 0f, LawScene.Gravity);
        var world = scene.Admit();
        var early = 0;
        world.Step(120, (tick, state) =>
        {
            var path = Pieces(Motion(state.World), ball.Value);
            var flip = path.FindIndex(s => s.Velocity.Y > 0f);
            if (flip <= 0 || flip == path.Count - 1 || path[^1].Velocity.Y <= 0f) return;
            early++;
            Assert.True(PhysicsGpuAbi.ReadPrismaticMovingContact(state.World, 0),
                $"tick {tick}: the ball struck the plate in substep {flip} and left by the last; the lane must record it");
        });
        Assert.True(early > 0, "guard: at least one strike lands before the tick's last substep");
    }

    [Fact]
    public void BlockedNeedsContactInteriorAndRestTogether()
    {
        var rest = new ActuatorDriveRead(new(1), new(2), false, true, .75f, -1.1e-5f, 0f, 1.55f);
        Assert.True(rest.Blocked);
        Assert.False((rest with { MovingContact = false }).Blocked);
        Assert.False((rest with { Speed = ActuatorDriveRead.RestSpeed }).Blocked);
        Assert.False((rest with { Coordinate = ActuatorDriveRead.StopSlop }).Blocked);
        Assert.False((rest with { Coordinate = 1.55f - ActuatorDriveRead.StopSlop }).Blocked);
        Assert.True((rest with { Coordinate = 2f * ActuatorDriveRead.StopSlop, SupplyClosed = true }).Blocked); // one state, either direction
    }

    // ---- D6: a ball resting against the closed, tilted blade passes through when the gate is enabled during Run ----

    [Theory]
    [InlineData(false)] // authored Disabled, enabled in Run
    [InlineData(true)]  // authored Enabled, disabled before the ball arrives, re-enabled
    public async Task ABallRestingAgainstTheClosedTiltedBladePassesDownstreamWhenEnabledDuringRun(bool authoredEnabled)
    {
        const double tilt = -15;
        var half = tilt * Math.PI / 360;
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 3, 0, 0, 0, (float)(Math.Sin(half)), (float)(Math.Cos(half)));
        // Declared geometry: ball radius .34, blade thickness .12, sleeve bore .70-.05=.65 m
        // (the collar bore is .78-.13=.65 too). D6 requires a resting ball, so place its center
        // tangent to both the upstream blade and lower bore, not inside the closed blade.
        const float radius = .34f, bladeHalfThickness = .06f, boreRadius = .65f;
        const float initialAlong = -(bladeHalfThickness + radius), initialUp = -(boreRadius - radius);
        var (sin, cos) = MathF.SinCos((float)(tilt * Math.PI / 180));
        var ball = WorkshopInput.Generic(PartId.Basketball, new(2),
            (float)(cos * initialAlong - sin * initialUp), (float)(3f + sin * initialAlong + cos * initialUp), 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(3), 3, 3, 0, enabled: authoredEnabled ? ElectricalEnable.Enabled : ElectricalEnable.Disabled);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(gate, ball, battery))
            { Connections = new(Power(battery.Id, gate.Id)) };
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), WorkshopCadenceSettings.Default(),
            new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(construction)).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
        if (authoredEnabled)
            Assert.Equal(WorkshopCommandOutcome.Applied, simulation.ConfigureElectrical(new(battery.Id, ElectricalEnable.Disabled)).Outcome);
        CanonicalBody Ball(WorkshopRead read) =>
            Enumerable.Range(0, read.Bodies.Count).Select(i => read.Bodies[i].Body).Single(row => row.Id == ball.Id);
        Vector3 LocalCentre(WorkshopRead read)
        {
            var body = Ball(read);
            var (dx, dy) = ((body.Cell.X + (float)body.Local.X) / 16f, (body.Cell.Y + (float)body.Local.Y) / 16f - 3f);
            return new(cos * dx + sin * dy, -sin * dx + cos * dy, (body.Cell.Z + (float)body.Local.Z) / 16f);
        }
        float Along(WorkshopRead read) => LocalCentre(read).X;
        void Resting(WorkshopRead read)
        {
            var velocity = Ball(read).Velocity;
            Assert.True(new Vector3(velocity.X, velocity.Y, velocity.Z).Length() < .01f,
                "The pre-enable ball must be resting (the 0.01 m/s game-grade rest envelope).");
            var angular = Enumerable.Range(0, read.Bodies.Count).Select(i => read.Bodies[i])
                .Single(row => row.Body.Id == ball.Id).AngularVelocity;
            Assert.True(new Vector3(angular.X, angular.Y, angular.Z).Length() < .02f,
                "The pre-enable ball must also remain within the 0.02 rad/s angular rest envelope.");
        }
        var initial = LocalCentre(simulation.Committed);
        // 1 um covers f32 authoring/transform roundoff at the 3 m mount, below the .1 mm placement slop.
        Assert.InRange(-initial.X - bladeHalfThickness - radius, -1e-6f, 1e-6f);
        Assert.InRange(boreRadius - MathF.Sqrt(initial.Y * initial.Y + initial.Z * initial.Z) - radius, -1e-6f, 1e-6f);
        for (var tick = 0; tick < 180; tick++) Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
        var held = Along(simulation.Committed);
        Assert.InRange(held, -.5f, -.39f); // guard: resting upstream against the closed blade, inside the entry mouth
        Resting(simulation.Committed);
        Assert.False(simulation.Committed.Presentation.Actuators[0].SupplyClosed);
        Assert.Equal(WorkshopCommandOutcome.Applied, simulation.ConfigureElectrical(new(battery.Id, ElectricalEnable.Enabled)).Outcome);
        var along = new List<float>();
        for (var tick = 0; tick < 300; tick++)
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
            along.Add(Along(simulation.Committed));
        }
        Assert.True(along.Min() >= held - .02f, $"never thrown back toward the entry mouth (min {along.Min()}, held {held})");
        Assert.True(along[^1] > .49f, $"the ball ends downstream of the exit mouth, {along[^1]}");
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,
            WorkshopSaveCodec.Encode(new(construction, new(10))));
        Assert.Equal(construction.Instances, decoded.Construction.Instances);
        Assert.Equal(construction.Connections, decoded.Construction.Connections);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
        Assert.Equal(construction.Instances, simulation.Construction.Instances);
        Assert.InRange(Along(simulation.Committed), initialAlong - 1e-5f, initialAlong + 1e-5f);
        await RepeatPhysicalOutcome(simulation);
        await using var fresh = await EventRingProducerTests.Ring.Building(decoded.Construction);
        await RepeatPhysicalOutcome(fresh.Simulation);

        async Task RepeatPhysicalOutcome(WorkshopSimulation replay)
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await replay.Run()).Outcome);
            if (authoredEnabled)
                Assert.Equal(WorkshopCommandOutcome.Applied, replay.ConfigureElectrical(new(battery.Id, ElectricalEnable.Disabled)).Outcome);
            for (var tick = 0; tick < 180; tick++)
                Assert.Equal(WorkshopCommandOutcome.Applied, (await replay.Advance()).Outcome);
            Assert.InRange(Along(replay.Committed), -.5f, -.39f);
            Resting(replay.Committed);
            Assert.False(replay.Committed.Presentation.Actuators[0].SupplyClosed);
            Assert.Equal(WorkshopCommandOutcome.Applied, replay.ConfigureElectrical(new(battery.Id, ElectricalEnable.Enabled)).Outcome);
            for (var tick = 0; tick < 300; tick++)
                Assert.Equal(WorkshopCommandOutcome.Applied, (await replay.Advance()).Outcome);
            Assert.True(Along(replay.Committed) > .49f);
            Assert.InRange(Along(replay.Committed), along[^1] - 1e-5f, along[^1] + 1e-5f);
        }
    }


    [Fact]
    public void AReusedEnginePreservesEveryBodyTrajectoryAndStaticSupportInTheNextScene()
    {
        // Ordinary scene reuse must clear previous motion before static supports and passive dynamics are admitted.
        var scene = new LawScene();
        var frame = scene.StaticBox(new(0, 1, 0), new(1f, .1f, 1f));
        var plate = scene.Box(new(0, 1.3f, 0), new(.3f, .1f, .3f), .5f, LawMaterial.Domino);
        scene.Prismatic(frame, plate, new(new(0, .3f, 0), Quaternion.Identity), ConstraintFrame.Identity, -.15f, .1f, 50f, .5f, ConnectedCollision.Enabled);
        var ball = scene.Ball(LawBall.Basketball, new(-.6f, 1.5f, 0), velocity: new(1f, 0, 0));
        var world = scene.Admit();
        var admitted = PhysicsGpuAbi.ReadDynamicBodies(world.Admitted);
        Assert.True(admitted.TryGet(plate, out var admittedPlate));
        var staticDeclarations = world.Scene.Bodies.ToArray().Where(body => body.Motion == RigidMotionKind.Static).ToArray();
        Assert.Equal(2, staticDeclarations.Length);
        var fresh = Observe(new WorkshopPhysicsEngine(), "fresh");
        var reused = new WorkshopPhysicsEngine();
        var previousScene = new LawScene(bench: false);
        var first = previousScene.Sphere(new(-3, 3, 0), .2f, 1f, LawMaterial.Domino,
            new(1, 2, 0), new(0, 0, 3), drag: 0, gravity: 0);
        var second = previousScene.Sphere(new(3, 3, 0), .2f, 1f, LawMaterial.Domino,
            new(-2, 1, 0), new(1, 2, 0), drag: 0, gravity: 0);
        var previousWorld = previousScene.Admit();
        var moved = previousWorld.Step(1, engine: reused);
        foreach (var id in new[] { first, second })
        {
            Assert.NotEqual(previousWorld.Initial(id).Position, moved[id].Position);
            Assert.NotEqual(previousWorld.Initial(id).Rotation, moved[id].Rotation);
            Assert.True(moved[id].Speed > 0);
            Assert.True(moved[id].Spin > 0);
        }
        var replay = Observe(reused, "reused");
        for (var tick = 0; tick < 120; tick++)
        {
            foreach (var id in new[] { plate, ball })
            {
                var expected = fresh[tick].Dynamic[id];
                var actual = replay[tick].Dynamic[id];
                var at = $"tick {tick + 1}, body {id.Value}";
                Same(expected.Body.Id, actual.Body.Id, $"{at}: identity");
                Same(expected.Body.Epoch, actual.Body.Epoch, $"{at}: epoch");
                Same(expected.Body.Tick, actual.Body.Tick, $"{at}: tick");
                Same(expected.Body.Cell, actual.Body.Cell, $"{at}: canonical cell");
                Same(expected.Body.Local, actual.Body.Local, $"{at}: canonical local position");
                Same(expected.Rotation, actual.Rotation, $"{at}: rotation");
                Same(expected.Body.Velocity, actual.Body.Velocity, $"{at}: velocity");
                Same(expected.AngularVelocity, actual.AngularVelocity, $"{at}: angular velocity");
                Same(expected.LocalCentreOfMass, actual.LocalCentreOfMass, $"{at}: centre of mass");
            }
            foreach (var id in new[] { plate, ball })
                for (var sample = 0; sample < 17; sample++)
                {
                    var expected = fresh[tick].Presentation[id][sample];
                    var actual = replay[tick].Presentation[id][sample];
                    var at = $"tick {tick + 1}, body {id.Value}, physical ordinal {tick * 4 + sample * .25f}: presentation";
                    Same(expected.Id, actual.Id, $"{at} identity");
                    Same(expected.Cell, actual.Cell, $"{at} cell");
                    Same(expected.Local, actual.Local, $"{at} local position");
                    Same(expected.Rotation, actual.Rotation, $"{at} rotation");
                }
            Same(fresh[tick].MovingContact, replay[tick].MovingContact,
                $"tick {tick + 1}, joint {world.Scene.Prismatics[0].Id.Value}: moving contact");
            foreach (var declared in staticDeclarations)
                Same(fresh[tick].Static[declared.Id], replay[tick].Static[declared.Id],
                    $"tick {tick + 1}, static body {declared.Id.Value}: state");
        }

        List<(Dictionary<GpuBodyId, PhysicsBodyRead> Dynamic, Dictionary<GpuBodyId, LawBody> Static, Dictionary<GpuBodyId, PresentedBody[]> Presentation, bool MovingContact)> Observe(
            WorkshopPhysicsEngine engine, string history)
        {
            var observations = new List<(Dictionary<GpuBodyId, PhysicsBodyRead> Dynamic, Dictionary<GpuBodyId, LawBody> Static, Dictionary<GpuBodyId, PresentedBody[]> Presentation, bool MovingContact)>();
            var plateDescended = false;
            var ballMovedHorizontally = false;
            world.Step(120, (tick, state) =>
            {
                Assert.Equal(observations.Count + 1, tick);
                // The production reader validates committed time, finite values and the 64 m/s / 128 rad/s envelope.
                // Copy typed values now: the callback's World buffers are reused on subsequent ticks.
                var reads = PhysicsGpuAbi.ReadDynamicBodies(state.World);
                Assert.Equal(2, reads.Count);
                var dynamicBodies = new Dictionary<GpuBodyId, PhysicsBodyRead>();
                foreach (var id in new[] { plate, ball })
                {
                    Assert.True(reads.TryGet(id, out var body), $"{history}, tick {tick}: missing body {id.Value}");
                    Assert.Equal(admittedPlate.Body.Epoch, body.Body.Epoch);
                    Assert.Equal((ulong)tick, body.Body.Tick);
                    var physical = state[id];
                    Assert.True(physical.Finite, $"{history}, tick {tick}, body {id.Value}: finite state");
                    Assert.InRange(physical.Speed, 0, 64);
                    Assert.InRange(physical.Spin, 0, 128);
                    // The game-grade quaternion norm error is at most 1e-4; replay itself is exact numeric equality.
                    Assert.InRange(MathF.Abs(physical.Rotation.Length() - 1f), 0f, 1e-4f);
                    dynamicBodies.Add(id, body);
                }
                var layout = PhysicsGpuAbi.Layout(state.World);
                var motion = PhysicsMotionRead.Decode(state.World.AsSpan(layout.MotionOffset, layout.MotionBytes), reads, new((ulong)tick));
                Assert.Equal(8, motion.Count); // Two declared dynamic owners, four pieces each.
                Assert.Equal(4u, motion.Substeps);
                Assert.Equal((uint)((tick - 1) * 4), motion.FirstOrdinal);
                Assert.Equal((uint)(tick * 4), motion.LastOrdinal);
                var presentation = new Dictionary<GpuBodyId, PresentedBody[]>();
                foreach (var id in new[] { plate, ball })
                {
                    var poses = new PresentedBody[17];
                    // Sample each substep's boundary and three interior phases through the real presentation seam.
                    for (var sample = 0; sample < 16; sample++)
                    {
                        var ordinal = new PhysicalTime(motion.FirstOrdinal + (uint)((sample + 2) / 4), ((sample + 2) % 4 - 2) * 1024f);
                        Assert.True(motion.TrySample(id, ordinal, out poses[sample]),
                            $"{history}, tick {tick}, body {id.Value}: missing presentation at {ordinal}");
                        Same(id, poses[sample].Id, $"{history}, tick {tick}: presented owner");
                    }
                    // The interval is right-open: its final exact endpoint is the committed body pose.
                    Assert.False(motion.TrySample(id, new(motion.LastOrdinal), out _));
                    var committed = dynamicBodies[id];
                    poses[16] = new(id, committed.Body.Cell, committed.Body.Local, committed.Rotation);
                    presentation.Add(id, poses);
                }
                var staticBodies = new Dictionary<GpuBodyId, LawBody>();
                foreach (var declared in staticDeclarations)
                {
                    var identityOffset = world.Scene.Layout.GetBodyOffset(declared.Id) + PhysicsGpuAbi.RecordIdentityOffset;
                    var committedId = new GpuBodyId(BinaryPrimitives.ReadUInt64LittleEndian(state.World.AsSpan(identityOffset)));
                    Same(declared.Id, committedId, $"{history}, tick {tick}: static committed identity");
                    var body = state[declared.Id];
                    Assert.Equal(declared.Cell, body.Cell);
                    Assert.Equal(declared.Local, body.Local);
                    Assert.Equal(declared.Id == frame ? new Vector3(0, 1, 0) : new Vector3(0, LawScene.BenchY, 0), body.Position);
                    Assert.Equal(Quaternion.Identity, body.Rotation);
                    Assert.Equal(Vector3.Zero, body.Velocity);
                    Assert.Equal(Vector3.Zero, body.Angular);
                    staticBodies.Add(declared.Id, body);
                }
                plateDescended |= state[plate].Position.Y < world.Initial(plate).Position.Y;
                ballMovedHorizontally |= state[ball].Position.X > world.Initial(ball).Position.X;
                Same(world.Scene.Prismatics[0], PhysicsGpuAbi.ReadPrismatic(state.World, 0),
                    $"{history}, tick {tick}, joint {world.Scene.Prismatics[0].Id.Value}: declaration");
                Assert.Null(PhysicsGpuAbi.ReadPrismaticDrive(state.World, 0));
                var movingContact = PhysicsGpuAbi.ReadPrismaticMovingContact(state.World, 0);
                observations.Add((dynamicBodies, staticBodies, presentation, movingContact));
            }, engine: engine);
            Assert.Equal(120, observations.Count);
            // Gravity loads the passive plate below its rest anchor; the ball starts with positive horizontal velocity.
            Assert.True(plateDescended, $"{history}: the gravity-loaded plate descends");
            Assert.True(ballMovedHorizontally, $"{history}: the moving basketball advances horizontally");
            return observations;
        }

        static void Same<T>(T expected, T actual, string at) =>
            Assert.True(EqualityComparer<T>.Default.Equals(expected, actual), $"{at}: expected {expected}, actual {actual}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AColdDeviceStartsTheFirstRealBallAndResetReplaysItsFirstTick(bool prepareExplicitly)
    {
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()), new(101, 206));
        if (prepareExplicitly) await device.Prepare();
        var settings = WorkshopCadenceSettings.Default();
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), settings, new(1),
            new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        NoEmittedEvents(device);
        Assert.Empty(simulation.Construction.Instances);
        Assert.Equal(0UL, simulation.Committed.Tick.Value);
        Assert.Equal(0, simulation.Committed.Bodies.Count);
        NoSyntheticActivity(simulation.Committed);
        var construction = new WorkshopConstruction(new(2), settings,
            new(WorkshopInput.Generic(PartId.Basketball, new(1), 0, 4, 0, 0, 0, 0, 1)));
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(construction)).Outcome);
        NoEmittedEvents(device);
        var authored = Ball(simulation.Committed);
        Assert.Equal(new GpuBodyId(1), authored.Body.Id);
        Assert.Equal(0f, authored.Body.Velocity.X);
        Assert.Equal(0f, authored.Body.Velocity.Y);
        Assert.Equal(0f, authored.Body.Velocity.Z);
        Assert.Equal(4f, (authored.Body.Cell.Y + authored.Body.Local.Y) / 16f);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
        NoEmittedEvents(device);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
        NoEmittedEvents(device);
        var first = Ball(simulation.Committed);
        Assert.Equal(1UL, simulation.Committed.Tick.Value);
        Assert.Equal(1UL, first.Body.Tick);
        Assert.Equal(simulation.Committed.Epoch.Value, first.Body.Epoch);
        Assert.True((first.Body.Cell.Y + first.Body.Local.Y) / 16f < 4f);
        Assert.True(first.Body.Velocity.Y < 0f);
        NoSyntheticActivity(simulation.Committed);
        var firstEpoch = simulation.Committed.Epoch;
        var firstRevision = simulation.Committed.Revision;
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
        NoEmittedEvents(device);
        Assert.Equal(firstEpoch.Value + 1, simulation.Committed.Epoch.Value);
        Assert.Equal(firstRevision.Value + 1, simulation.Committed.Revision.Value);
        Assert.Equal(0UL, simulation.Committed.Tick.Value);
        SamePhysicalBody(authored, Ball(simulation.Committed));
        Assert.Equal(construction.Revision, simulation.Construction.Revision);
        var originalInstance = Assert.IsType<WorkshopGenericInstance>(Assert.Single(construction.Instances));
        var restoredInstance = Assert.IsType<WorkshopGenericInstance>(Assert.Single(simulation.Construction.Instances));
        Assert.Equal(originalInstance, restoredInstance); // Typed part, identity, pose, lock and parameter values.
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
        NoEmittedEvents(device);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
        NoEmittedEvents(device);
        Assert.Equal(1UL, simulation.Committed.Tick.Value);
        Assert.Equal(firstEpoch.Value + 1, simulation.Committed.Epoch.Value);
        Assert.Equal(firstRevision.Value + 3, simulation.Committed.Revision.Value);
        var replay = Ball(simulation.Committed);
        Assert.Equal(simulation.Committed.Epoch.Value, replay.Body.Epoch);
        Assert.Equal(1UL, replay.Body.Tick);
        SamePhysicalBody(first, replay);
        NoSyntheticActivity(simulation.Committed);
    }

    [Fact]
    public async Task AnEmptyBenchAppliesItsOrdinaryLifecycleWithoutProducingDynamicBodiesOrEvents()
    {
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()), new(102, 207));
        var settings = WorkshopCadenceSettings.Default();
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), settings, new(1),
            new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        NoEmittedEvents(device);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(new(new(2), settings, new([])))).Outcome);
        NoEmittedEvents(device);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
        NoEmittedEvents(device);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
        NoEmittedEvents(device);
        Assert.Equal(1UL, simulation.Committed.Tick.Value);
        Assert.Empty(simulation.Construction.Instances);
        Assert.Equal(0, simulation.Committed.Bodies.Count);
        NoSyntheticActivity(simulation.Committed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
        NoEmittedEvents(device);
        Assert.Equal(0UL, simulation.Committed.Tick.Value);
        Assert.Equal(WorkshopSimulationPhase.Building, simulation.Phase);
        Assert.Equal(0, simulation.Committed.Bodies.Count);
        NoSyntheticActivity(simulation.Committed);
    }

    private static void NoEmittedEvents(WorkshopGpuDevice device)
    {
        var publication = device.DrainEvents();
        // An empty publication means there was nothing to publish; otherwise decode the public event records.
        if (publication.Length > 0) Assert.Empty(EventRingProducerTests.Decode(publication));
    }

    private static PhysicsBodyRead Ball(WorkshopRead read)
    {
        Assert.Equal(1, read.Bodies.Count);
        Assert.True(read.Bodies.TryGet(new(1), out var ball));
        return ball;
    }

    private static void SamePhysicalBody(PhysicsBodyRead expected, PhysicsBodyRead actual)
    {
        Assert.Equal(expected.Body.Id, actual.Body.Id);
        Assert.Equal(expected.Body.Cell, actual.Body.Cell);
        Assert.Equal(expected.Body.Local, actual.Body.Local);
        Assert.Equal(expected.Body.Velocity, actual.Body.Velocity);
        Assert.Equal(expected.Rotation, actual.Rotation);
        Assert.Equal(expected.AngularVelocity, actual.AngularVelocity);
        Assert.Equal(expected.LocalCentreOfMass, actual.LocalCentreOfMass);
    }

    private static void NoSyntheticActivity(WorkshopRead read)
    {
        Assert.Equal(0, read.Captures.Count);
        Assert.Equal(0, read.Activations.Count);
        Assert.Equal(0, read.Timers.Count);
        Assert.Equal(0, read.ContactWorks.Count);
        Assert.Equal(0, read.Electrical.Count);
        Assert.Equal(0, read.Memory.Count);
    }
}

public static class NativeTwoDriveScenario
{
    public static void Run(string bundleJson, bool enabled)
    {
        var entries = CuriousContraptions.Bundle.BundleSchema.Read(bundleJson).Elements;
        var template = entries.Single(entry => entry.Metadata.Id == "powered_gate");
        var shape = new CuriousContraptions.Abstractions.ColliderDescriptor(
            CuriousContraptions.Abstractions.ColliderShapeKind.Box, new(.1f), Vector3.Zero, Quaternion.Identity, new(0, 0, 0, .1f), 0);
        float[] targets = [1.2f, .6f];
        var sliders = targets.Select((target, index) => new PrismaticSliderDescriptor(
            new(new(index == 0 ? -2 : 2, 0, 0), Quaternion.Identity), new(Vector3.Zero, Quaternion.Identity),
            -.2f, 2, MechanicalValue.Fixed(3), MechanicalValue.Fixed(.5f), MechanicalValue.Fixed(0), false,
            new(MassFrameDeclaration.Constant(1, Vector3.Zero, Vector3.One, Quaternion.Identity, 0, new(0, -9.81f, 0)), [shape]),
            new(MechanicalValue.Fixed(target), MechanicalValue.Fixed(1.6f), MechanicalValue.Fixed(8),
                MechanicalValue.Fixed(20), MechanicalValue.Fixed(20)))).ToArray();
        var entry = template with
        {
            Metadata = template.Metadata with { Id = "native_two_drives", PaletteOrder = entries.Max(e => e.Metadata.PaletteOrder) + 1 },
            Footprint = new(3, 2, 2), Colliders = [shape], ColliderBindings = [], TubeMouths = [], TubeMouthBindings = [],
            Mechanical = template.Mechanical! with { Sliders = sliders },
            Presentation = new(1, CuriousContraptions.Abstractions.PresentationAxes.None, [],
                [new("Visual/First", new(1), Vector3.Zero), new("Visual/Second", new(2), Vector3.Zero)], [], [])
        };
        var native = CuriousContraptions.Bundle.BundleSchema.Read(CuriousContraptions.Bundle.BundleSchema.WriteEntries(
            [entry, entries.Single(e => e.Metadata.Id == "battery")])).Elements;
        Assert.Empty(CuriousContraptions.Bundle.BundleValidator.Validate(native.Select(e => new CuriousContraptions.Bundle.NamedEntry(e.Metadata.Id, e)).ToArray()));
        DeclaredPhysics.Install(native);
        DeclaredPorts.Install(ElementPortCatalogue.FromBundle(native));
        var owner = WorkshopInput.Generic(new(entry.Metadata.Id), new(1), 0, 5, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(2), 4, 5, 0, enabled: enabled ? ElectricalEnable.Enabled : ElectricalEnable.Disabled);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(owner, battery))
            { Connections = new(new WorkshopConnection(battery.Id, WorkshopSocket.Supply, owner.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)) };
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var owned = scene.Prismatics.ToArray().Where(joint => joint.BodyA == owner.Id).ToArray();
        Assert.Equal(2, owned.Length);
        Assert.Equal(2, owned.Select(joint => joint.BodyB).Distinct().Count());
        var route = Assert.Single(scene.SupplyGraph.Nodes.ToArray(), node => node.Owner == owner.Id);
        Assert.Equal(owned.Select(joint => joint.Id).OrderBy(id => id.Value), route.Consumers.OrderBy(id => id.Value));
        var current = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var candidate = new byte[current.Length];
        var engine = new WorkshopPhysicsEngine();
        var interactions = PhysicsInteractionSnapshot.Empty;
        for (var tick = 0; tick < 480; tick++)
        {
            var sources = PhysicsGpuAbi.ReadElectrical(current);
            var input = PhysicsTickInputs.Compose(scene.SupplyGraph, owned.Length, WorkshopTickControls.For(sources, (_, value) => value), _ => false);
            interactions = engine.Advance(current, candidate, input, interactions);
            (current, candidate) = (candidate, current);
        }
        for (var index = 0; index < 2; index++)
        {
            var state = LawState.Read(scene, current, owned[index].BodyB);
            // Millimetre target neighborhood is the existing drive-position acceptance.
            Assert.InRange(state.Position.Y - 5, (enabled ? targets[index] : -.2f) - .003f,
                (enabled ? targets[index] : -.2f) + .003f);
            Assert.InRange(MathF.Abs(state.Velocity.Y), 0, .02f);
        }
    }

}
