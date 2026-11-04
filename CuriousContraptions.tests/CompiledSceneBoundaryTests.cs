using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using Google.FlatBuffers;

namespace CuriousContraptions.Tests;

public sealed class CompiledSceneBoundaryTests
{
    public enum Defect { UnknownMotion, NarrowedMotion, NonfiniteMass, ForeignColliderBody, ForeignTrigger, WrongSubsteps }

    private static WorkshopConstruction Construction => new(new(2), WorkshopCadenceSettings.Default(), new(
        WorkshopInput.Generic(new PartId("ball"), new(1), 0, 6, 0, 0, 0, 0, 1),
        WorkshopInput.Generic(new PartId("switch"), new(2), 0, 1, 0, 0, 0, 0, 1)));



    [Theory]
    [InlineData(32760, false)]
    [InlineData(32768, true)]
    public void VtableFieldOffsetsRetainTheirUnsignedRange(int padding, bool aboveSignedRange)
    {
        var builder = new FlatBufferBuilder(padding + 256);
        CuriousContraptions.Wire.CapabilityVector3Record.StartCapabilityVector3Record(builder);
        CuriousContraptions.Wire.CapabilityVector3Record.AddX(builder, 4f);
        builder.Pad(padding);
        var record = CuriousContraptions.Wire.CapabilityVector3Record.EndCapabilityVector3Record(builder);
        builder.FinishSizePrefixed(record.Value, "VEC3");
        var bytes = builder.SizedByteArray();
        var buffer = new ByteBuffer(bytes);
        var table = 4 + buffer.GetInt(4);
        var vtable = table - buffer.GetInt(table);
        var relative = buffer.GetUshort(vtable + 4);
        Assert.Equal(aboveSignedRange, relative > short.MaxValue);
        Assert.Equal(table + relative, Table.__offset(4, bytes.Length - table, buffer));
        Assert.True(new Verifier(buffer).VerifyBuffer("VEC3", true,
            CuriousContraptions.Wire.CapabilityVector3RecordVerify.Verify));
        buffer.Position = 4;
        Assert.Equal(4f, CuriousContraptions.Wire.CapabilityVector3Record.GetRootAsCapabilityVector3Record(buffer).X);

        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(vtable + 4), ushort.MaxValue);
        Assert.False(new Verifier(new ByteBuffer(bytes)).VerifyBuffer("VEC3", true,
            CuriousContraptions.Wire.CapabilityVector3RecordVerify.Verify));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32768)]
    [InlineData(65536)]
    public void CompiledFramesPreserveRequiredTablesBeyondSignedShortPositions(int padding)
    {
        var scene = WorkshopSceneCompiler.Compile(Construction, new(101, 206), new(7));
        var builder = new FlatBufferBuilder(1024);
        var installation = WorkshopWireFlatBuffers.EncodeCompiledScene(builder, scene);
        // Valid unreachable data places the referenced installation on either side of the old signed-short boundary.
        CuriousContraptions.Wire.WorkshopCommand.CreateSessionVector(builder, new byte[padding]);
        var sessionBytes = new byte[RuntimeSessionId.ByteLength];
        new RuntimeSessionId(101, 206).WriteTo(sessionBytes);
        var session = CuriousContraptions.Wire.WorkshopCommand.CreateSessionVector(builder, sessionBytes);
        CuriousContraptions.Wire.WorkshopCommand.StartWorkshopCommand(builder);
        CuriousContraptions.Wire.WorkshopCommand.AddSequence(builder, 1);
        CuriousContraptions.Wire.WorkshopCommand.AddKind(builder, CuriousContraptions.Wire.CommandKind.Construct);
        CuriousContraptions.Wire.WorkshopCommand.AddEpoch(builder, 1);
        CuriousContraptions.Wire.WorkshopCommand.AddRevision(builder, 1);
        CuriousContraptions.Wire.WorkshopCommand.AddRevisionKind(builder, (uint)ExpectedRevisionKind.Exact);
        CuriousContraptions.Wire.WorkshopCommand.AddSession(builder, session);
        CuriousContraptions.Wire.WorkshopCommand.AddCadence(builder, 1);
        CuriousContraptions.Wire.WorkshopCommand.AddProjection(builder, 1);
        CuriousContraptions.Wire.WorkshopCommand.AddScene(builder, installation);
        builder.FinishSizePrefixed(CuriousContraptions.Wire.WorkshopCommand.EndWorkshopCommand(builder).Value,
            WorkshopWireFlatBuffers.CommandIdentifier);
        var bytes = builder.SizedByteArray();
        var buffer = new ByteBuffer(bytes);
        var root = new Table(4 + buffer.GetInt(4), buffer);
        var table = Child(root, 11);
        var vtable = table.bb_pos - buffer.GetInt(table.bb_pos);
        Assert.Equal(padding > 0, vtable > short.MaxValue);
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(bytes).Scene!;
        Assert.Equal(new CompiledSceneId(7), decoded.Identity);
        Assert.Equal(new GpuBodyId[] { new(1), new(2) }, decoded.Owners.OrderBy(owner => owner.Value));
        Assert.Equal(scene.Physics.Bodies.ToArray(), decoded.Physics.Bodies.ToArray());

        // A malformed required settings reference at the same byte position must still refuse.
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(Slot(table, 2)), uint.MaxValue);
        Assert.Equal(WireFaultReason.VerifierRejected,
            Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(bytes)).Reason);
    }

    [Theory]
    [InlineData(Defect.UnknownMotion)]
    [InlineData(Defect.NarrowedMotion)]
    [InlineData(Defect.NonfiniteMass)]
    [InlineData(Defect.ForeignColliderBody)]
    [InlineData(Defect.ForeignTrigger)]
    [InlineData(Defect.WrongSubsteps)]
    public async Task MalformedCompiledCapabilitiesRefuseBeforeDispatchAndLeaveInstalledWorldUsable(Defect defect)
    {
        await using var world = await WorkshopIntegrationHarness.Building(Construction);
        var before = world.Committed;
        var command = world.Command(WorkshopCommandKind.Construct, Construction with { Revision = new(3) });
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(command);
        var admitted = WorkshopWireFlatBuffers.DecodeCommand(bytes).Scene!;
        Assert.Equal(new GpuBodyId(1), admitted.Physics.Bodies.ToArray().Single(body => body.Motion == RigidMotionKind.Dynamic).Id);
        Assert.Single(admitted.Activation.Nodes.ToArray());
        Mutate(bytes, defect);
        var refusal = Assert.Throws<WireFaultException>(() => world.Router.Dispatch(bytes));
        Assert.Equal(WireFaultReason.OutOfRange, refusal.Reason);
        Assert.Equal(before, world.Committed);
        // The refused request did not consume a sequence or partially install its candidate.
        await world.Run();
        await world.Advance(2);
        Assert.Equal(2UL, world.Committed.Tick.Value);
        Assert.True(world.Committed.Bodies.TryGet(new(1), out var ball));
        Assert.True(ball.Body.Local.Y < before.Bodies[0].Body.Local.Y);
        await world.Reset();
        Assert.Equal(before.Bodies[0].Body.Local, world.Committed.Bodies[0].Body.Local);
    }

    [Fact]
    public async Task MainCompilerRefusesAnInitialHollowOverlapWithoutDispatchOrWorldChange()
    {
        using var host = new WorkshopActivationAnimationTests.NativeClient();
        var settings = WorkshopCadenceSettings.Default();
        var pipe = WorkshopInput.Generic(new PartId("pipe"), new(2), 0, 3, 0, 0, 0, 0, 1);
        var valid = new WorkshopConstruction(new(2), settings, new(
            WorkshopInput.Generic(new PartId("ball"), new(1), 0, 6, 0, 0, 0, 0, 1), pipe));
        await host.Construct(valid);
        var port = host.Client.Observation;
        var before = port.Query(port.Session, WorkshopObservationQuery.Committed).Value;
        var receipts = port.Receipts(port.Session, default).Value!.Length;
        var invalid = new WorkshopConstruction(new(3), settings, new(
            WorkshopInput.Generic(new PartId("ball"), new(1), 0, 3.65f, 0, 0, 0, 0, 1), pipe));
        var refusal = await Assert.ThrowsAsync<WorkshopEditRejectedException>(() =>
            host.Client.Execute(WorkshopCommandKind.Construct, invalid));
        Assert.Equal(WorkshopEditRejection.InitialOverlap, refusal.Reason);
        Assert.False(string.IsNullOrWhiteSpace(refusal.Message));
        Assert.Equal(receipts, port.Receipts(port.Session, default).Value!.Length);
        Assert.Equal(before, port.Query(port.Session, WorkshopObservationQuery.Committed).Value);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await host.Client.Execute(WorkshopCommandKind.Run)).Response.Result.Outcome);
        await host.Advance(2);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await host.Client.Execute(WorkshopCommandKind.Reset)).Response.Result.Outcome);
    }


    public enum PresentationDefect { NonfiniteFrom, MissingSocket, UnsupportedCurve, IncompatibleDriver, InvalidElectricalSocket }

    [Theory]
    [InlineData(PresentationDefect.InvalidElectricalSocket)]
    [InlineData(PresentationDefect.NonfiniteFrom)]
    [InlineData(PresentationDefect.MissingSocket)]
    [InlineData(PresentationDefect.UnsupportedCurve)]
    [InlineData(PresentationDefect.IncompatibleDriver)]
    public async Task MalformedPresentationRefusesAtCommandAndResponseBoundaries(PresentationDefect defect)
    {
        var construction = Construction with { Instances = new(
            Construction.Instances[0], Construction.Instances[1],
            WorkshopInput.Generic(new PartId("counter"), new(3), 4, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(new PartId("both_gate"), new(4), -4, 3, 0, 0, 0, 0, 1)) };
        await using var world = await WorkshopIntegrationHarness.Building(construction);
        var before = world.Committed;
        var original = world.Simulation.Scene;
        var tracks = original.Presentation.ToArray();
        Assert.NotEmpty(tracks);
        var trackIndex = Array.FindIndex(tracks, track => track.Declaration.Driver == (defect == PresentationDefect.InvalidElectricalSocket
            ? CuriousContraptions.Abstractions.PresentationDriver.CommittedElectricalSocket
            : CuriousContraptions.Abstractions.PresentationDriver.CommittedCount));
        Assert.NotEqual(-1, trackIndex);
        tracks[trackIndex] = tracks[trackIndex] with { Declaration = tracks[trackIndex].Declaration with { From = .25f } };
        var scene = new WorkshopCompiledScene(original.Identity, original.Revision, original.Settings,
            original.Physics, original.Activation, tracks, original.Owners, original.RunControls);
        var command = world.Command(WorkshopCommandKind.Construct, construction) with { Scene = scene };
        var request = WorkshopWireFlatBuffers.EncodeCommand(command);
        var response = WorkshopWireFlatBuffers.EncodeResponse(world.Latest with { Scene = scene });
        Assert.Equal(.25f, WorkshopWireFlatBuffers.DecodeCommand(request).Scene!.Presentation[trackIndex].Declaration.From);
        Assert.Equal(.25f, WorkshopWireFlatBuffers.DecodeResponse(response).Scene!.Presentation[trackIndex].Declaration.From);
        Poison(request, 11);
        Poison(response, 27);
        Assert.Equal(WireFaultReason.OutOfRange,
            Assert.Throws<WireFaultException>(() => world.Router.Dispatch(request)).Reason);
        Assert.Equal(WireFaultReason.OutOfRange,
            Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(response)).Reason);
        Assert.Same(original, world.Simulation.Scene);
        Assert.Equal(before, world.Committed);
        await world.Run();
        await world.Advance(2);
        Assert.True(world.Committed.Bodies[0].Body.Local.Y < before.Bodies[0].Body.Local.Y);
        await world.Reset();
        Assert.Equal(before.Bodies[0].Body.Local, world.Committed.Bodies[0].Body.Local);

        void Poison(byte[] bytes, int sceneField)
        {
            var buffer = new ByteBuffer(bytes);
            var root = new Table(4 + buffer.GetInt(4), buffer);
            var binding = Child(Rows(Child(root, sceneField), 29)[trackIndex], 3);
            switch (defect)
            {
                case PresentationDefect.InvalidElectricalSocket:
                    Write(11, (uint)CuriousContraptions.Abstractions.PortSocket.ActivationIn); break;
                case PresentationDefect.NonfiniteFrom:
                    BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(Slot(binding, 2)), float.NaN); break;
                case PresentationDefect.MissingSocket:
                    Write(1, (uint)CuriousContraptions.Abstractions.PresentationDriver.CommittedElectricalSocket); break;
                case PresentationDefect.UnsupportedCurve:
                    Write(4, (uint)CuriousContraptions.Abstractions.PresentationCurve.SineSquaredPulse); break;
                case PresentationDefect.IncompatibleDriver:
                    Write(1, (uint)CuriousContraptions.Abstractions.PresentationDriver.CommittedOscillatorPhase); break;
                default: throw new ArgumentOutOfRangeException(nameof(defect));
            }
            void Write(int field, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(Slot(binding, field)), value);
        }
    }

    private static WorkshopConstruction LeverConstruction => new(new(2), WorkshopCadenceSettings.Default(), new(
        WorkshopInput.Generic(new PartId("impact_lever"), new(1), 0, 3, 0, 0, 0, 0, 1)));

    private static WorkshopCommand Construct(WorkshopCompiledScene scene) =>
        new(new(1), WorkshopCommandKind.Construct, new(1), default, scene, Session: new(101, 206), Cadence: new(1), Projection: new(1));

    // Story 10.3a: a compiled revolute crosses the command wire with every field. The lever's own row leaves stiffness, damping and
    // collision at zero and both frames at the identity, so the row is varied in each of those fields; a codec that dropped or
    // swapped any one decodes a different declaration. Story 10.3b: the friction torque is varied from the lever's 2 N m to a value no
    // other field carries.
    [Fact]
    public void EveryRevoluteFieldCrossesTheCommandWire()
    {
        var compiled = WorkshopSceneCompiler.Compile(LeverConstruction, new(101, 206), new(7));
        var physics = compiled.Physics;
        var varied = Assert.Single(physics.Revolutes.ToArray()) with
        {
            FrameA = new(new(.05f, -.02f, .01f), System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitY, .3f)),
            FrameB = new(new(-.04f, .03f, -.02f), System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX, -.2f)),
            Lower = -.4f,
            Upper = .45f,
            Stiffness = 40f,
            Damping = 2f,
            FrictionTorque = 3.5f,
            Collision = ConnectedCollision.Enabled
        };
        var scene = new WorkshopCompiledScene(compiled.Identity, compiled.Revision, compiled.Settings,
            new PhysicsSceneDeclaration(physics.Document, physics.NextIdentity, physics.Bodies, physics.Colliders, physics.Materials,
                physics.Sensors, physics.Guides, physics.Triggers, physics.ContactWorks, physics.OrientationSensors,
                physics.Electrical.Sources.ToArray(), physics.Electrical.Bindings.ToArray(), physics.Prismatics,
                physics.SupplyGraph.Nodes.ToArray(), physics.SupplyGraph.Edges.ToArray(), physics.ContactLoadSensors, physics.PrismaticDrives,
                physics.TensileSpans, revolutes: [varied]),
            compiled.Activation, compiled.Presentation, compiled.Owners, compiled.RunControls);

        var decoded = WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(Construct(scene))).Scene!;

        Assert.Equal([varied], decoded.Physics.Revolutes.ToArray());
    }

    // Story 10.3a: the lever's compiled hinge row reaches the decoded scene unchanged, the row the running world installs.
    [Fact]
    public void TheCompiledLeverHingeRoundTripsUnchanged()
    {
        var compiled = WorkshopSceneCompiler.Compile(LeverConstruction, new(101, 206), new(7));

        var decoded = WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(Construct(compiled))).Scene!;

        Assert.Equal(Assert.Single(compiled.Physics.Revolutes.ToArray()), Assert.Single(decoded.Physics.Revolutes.ToArray()));
    }

    // Story 10.3a control: a construction without a hinge decodes with no revolutes, and its bodies, colliders and prismatics are the
    // compiled ones.
    [Fact]
    public void AConstructionWithoutAHingeCrossesTheWireWithNoRevolutesAndItsOtherTablesIntact()
    {
        var compiled = WorkshopSceneCompiler.Compile(Construction, new(101, 206), new(7));

        var decoded = WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(Construct(compiled))).Scene!;

        Assert.Empty(decoded.Physics.Revolutes.ToArray());
        Assert.Equal(compiled.Physics.Bodies.ToArray(), decoded.Physics.Bodies.ToArray());
        Assert.Equal(compiled.Physics.Colliders.ToArray(), decoded.Physics.Colliders.ToArray());
        Assert.Equal(compiled.Physics.Prismatics.ToArray(), decoded.Physics.Prismatics.ToArray());
    }

    public enum RevoluteDefect { LimitOrder, UnadmittedBody, NegativeFrictionTorque, NaNFrictionTorque, InfiniteFrictionTorque, NegativeInfiniteFrictionTorque }

    // Story 10.3a: a revolute row whose lower limit is not below its upper limit, or whose body is not in the scene, is refused at
    // both decode boundaries that carry a compiled scene, and the refused command leaves the installed lever world unchanged.
    // Story 10.3b: so is a row whose friction torque is negative, NaN, +infinity or -infinity.
    [Theory]
    [InlineData(RevoluteDefect.LimitOrder)]
    [InlineData(RevoluteDefect.UnadmittedBody)]
    [InlineData(RevoluteDefect.NegativeFrictionTorque)]
    [InlineData(RevoluteDefect.NaNFrictionTorque)]
    [InlineData(RevoluteDefect.InfiniteFrictionTorque)]
    [InlineData(RevoluteDefect.NegativeInfiniteFrictionTorque)]
    public async Task MalformedRevoluteRowsRefuseAtCommandAndResponseBoundariesAndLeaveTheInstalledWorldUnchanged(RevoluteDefect defect)
    {
        await using var world = await WorkshopIntegrationHarness.Building(LeverConstruction);
        var before = world.Committed;
        var original = world.Simulation.Scene;
        var request = WorkshopWireFlatBuffers.EncodeCommand(world.Command(WorkshopCommandKind.Construct, LeverConstruction with { Revision = new(3) }));
        var response = WorkshopWireFlatBuffers.EncodeResponse(world.Latest with { Scene = original });
        Assert.Single(WorkshopWireFlatBuffers.DecodeCommand(request).Scene!.Physics.Revolutes.ToArray());
        Assert.Single(WorkshopWireFlatBuffers.DecodeResponse(response).Scene!.Physics.Revolutes.ToArray());
        Poison(request, 11); // WorkshopCommand.scene
        Poison(response, 27); // WorkshopResponse.scene

        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => world.Router.Dispatch(request)).Reason);
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(response)).Reason);
        Assert.Same(original, world.Simulation.Scene);
        Assert.Equal(before, world.Committed);

        void Poison(byte[] bytes, int sceneField)
        {
            var buffer = new ByteBuffer(bytes);
            var row = Assert.Single(Rows(Child(new Table(4 + buffer.GetInt(4), buffer), sceneField), 30)); // CompiledSceneRecord.revolutes
            switch (defect)
            {
                case RevoluteDefect.LimitOrder: // lower := upper
                    BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(Slot(row, 5)), BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(Slot(row, 6))));
                    break;
                case RevoluteDefect.UnadmittedBody: // body_b names no scene body
                    BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(Slot(row, 2)), ulong.MaxValue);
                    break;
                case RevoluteDefect.NegativeFrictionTorque: // friction_torque := -1 (the lever writes 2, so the slot is present)
                    BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(Slot(row, 10)), -1f);
                    break;
                case RevoluteDefect.NaNFrictionTorque:
                    BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(Slot(row, 10)), float.NaN);
                    break;
                case RevoluteDefect.InfiniteFrictionTorque:
                    BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(Slot(row, 10)), float.PositiveInfinity);
                    break;
                case RevoluteDefect.NegativeInfiniteFrictionTorque:
                    BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(Slot(row, 10)), float.NegativeInfinity);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(defect));
            }
        }
    }

    /// <summary>engine-revolute-drive: the compiled lever with its hinge made Continuous (full-turn limits), a supplied drive on it and a
    /// consumer node on the lever naming that drive with its typed kind: a negative target and torque no shipped package declares (the
    /// Motor's compiled drive is covered by MotorTests).</summary>
    private static WorkshopCompiledScene DrivenLeverScene()
    {
        var compiled = WorkshopSceneCompiler.Compile(LeverConstruction, new(101, 206), new(7));
        var physics = compiled.Physics;
        var hinge = Assert.Single(physics.Revolutes.ToArray()) with
        {
            Lower = -MathF.PI, Upper = MathF.PI, Travel = CuriousContraptions.Abstractions.Mechanical.RevoluteTravel.Continuous
        };
        SupplyNodeDeclaration consumer = new(hinge.BodyA, SupplyNodeKind.Consumer, Consumers: [SupplyConsumer.Revolute(hinge.Id)]);
        return new WorkshopCompiledScene(compiled.Identity, compiled.Revision, compiled.Settings,
            new PhysicsSceneDeclaration(physics.Document, physics.NextIdentity, physics.Bodies, physics.Colliders, physics.Materials,
                physics.Sensors, physics.Guides, physics.Triggers, physics.ContactWorks, physics.OrientationSensors,
                physics.Electrical.Sources.ToArray(), physics.Electrical.Bindings.ToArray(), physics.Prismatics,
                [.. physics.SupplyGraph.Nodes.ToArray(), consumer], physics.SupplyGraph.Edges.ToArray(), physics.ContactLoadSensors,
                physics.PrismaticDrives, physics.TensileSpans, revolutes: [hinge], revoluteDrives: [new(hinge.Id, -7.5f, 3.25f)]),
            compiled.Activation, compiled.Presentation, compiled.Owners, compiled.RunControls);
    }

    // engine-revolute-drive: the hinge's travel, its drive (a negative target and a torque no other field carries) and the typed consumer
    // that names it decode unchanged from the command wire.
    [Fact]
    public void ADrivenContinuousHingeAndItsTypedConsumerCrossTheCommandWire()
    {
        var scene = DrivenLeverScene();

        var decoded = WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(Construct(scene))).Scene!.Physics;

        Assert.Equal(scene.Physics.Revolutes.ToArray(), decoded.Revolutes.ToArray());
        Assert.Equal(scene.Physics.RevoluteDrives.ToArray(), decoded.RevoluteDrives.ToArray());
        Assert.Equal(scene.Physics.SupplyGraph.Nodes.ToArray().Select(node => node.Consumers.IsDefault ? [] : node.Consumers.ToArray()),
            decoded.SupplyGraph.Nodes.ToArray().Select(node => node.Consumers.ToArray()));
    }

    public enum DriveDefect
    {
        UndefinedTravel, ContinuousNarrowLower, ContinuousWithSpring, NaNTargetSpeed, OverspeedTarget, ZeroTorque, UndefinedConsumerKind
    }

    /// <summary>A Construct command carrying <paramref name="scene"/>, encoded with every scalar written even at its default, so a zero
    /// field (such as the continuous hinge's stiffness) has a slot to poison.</summary>
    private static byte[] CommandWithEveryField(WorkshopCompiledScene scene)
    {
        var builder = new FlatBufferBuilder(1024) { ForceDefaults = true };
        var installation = WorkshopWireFlatBuffers.EncodeCompiledScene(builder, scene);
        var sessionBytes = new byte[RuntimeSessionId.ByteLength];
        new RuntimeSessionId(101, 206).WriteTo(sessionBytes);
        var session = CuriousContraptions.Wire.WorkshopCommand.CreateSessionVector(builder, sessionBytes);
        CuriousContraptions.Wire.WorkshopCommand.StartWorkshopCommand(builder);
        CuriousContraptions.Wire.WorkshopCommand.AddSequence(builder, 1);
        CuriousContraptions.Wire.WorkshopCommand.AddKind(builder, CuriousContraptions.Wire.CommandKind.Construct);
        CuriousContraptions.Wire.WorkshopCommand.AddEpoch(builder, 1);
        CuriousContraptions.Wire.WorkshopCommand.AddRevision(builder, 1);
        CuriousContraptions.Wire.WorkshopCommand.AddRevisionKind(builder, (uint)ExpectedRevisionKind.Exact);
        CuriousContraptions.Wire.WorkshopCommand.AddSession(builder, session);
        CuriousContraptions.Wire.WorkshopCommand.AddCadence(builder, 1);
        CuriousContraptions.Wire.WorkshopCommand.AddProjection(builder, 1);
        CuriousContraptions.Wire.WorkshopCommand.AddScene(builder, installation);
        builder.FinishSizePrefixed(CuriousContraptions.Wire.WorkshopCommand.EndWorkshopCommand(builder).Value,
            WorkshopWireFlatBuffers.CommandIdentifier);
        return builder.SizedByteArray();
    }

    // engine-revolute-drive: a revolute row with an undefined travel, a continuous row whose lower limit is not −π or that carries a
    // spring, a drive row whose target speed is NaN or beyond the 128 rad/s envelope or whose torque is zero, and a consumer naming an
    // undefined drive kind are each refused at the command decode boundary.
    [Theory]
    [InlineData(DriveDefect.UndefinedTravel)]
    [InlineData(DriveDefect.ContinuousNarrowLower)]
    [InlineData(DriveDefect.ContinuousWithSpring)]
    [InlineData(DriveDefect.NaNTargetSpeed)]
    [InlineData(DriveDefect.OverspeedTarget)]
    [InlineData(DriveDefect.ZeroTorque)]
    [InlineData(DriveDefect.UndefinedConsumerKind)]
    public void MalformedDriveRowsRefuseAtTheCommandBoundary(DriveDefect defect)
    {
        var request = CommandWithEveryField(DrivenLeverScene());
        Assert.Single(WorkshopWireFlatBuffers.DecodeCommand(request).Scene!.Physics.RevoluteDrives.ToArray());
        var buffer = new ByteBuffer(request);
        var scene = Child(new Table(4 + buffer.GetInt(4), buffer), 11); // WorkshopCommand.scene
        switch (defect)
        {
            case DriveDefect.UndefinedTravel: // CompiledSceneRecord.revolutes[0].travel := 2
                BinaryPrimitives.WriteUInt32LittleEndian(request.AsSpan(Slot(Assert.Single(Rows(scene, 30)), 11)), 2);
                break;
            case DriveDefect.ContinuousNarrowLower: // revolutes[0].lower := −3 (a continuous hinge's limits are exactly [−π, π])
                BinaryPrimitives.WriteSingleLittleEndian(request.AsSpan(Slot(Assert.Single(Rows(scene, 30)), 5)), -3f);
                break;
            case DriveDefect.ContinuousWithSpring: // revolutes[0].stiffness := 10
                BinaryPrimitives.WriteSingleLittleEndian(request.AsSpan(Slot(Assert.Single(Rows(scene, 30)), 7)), 10f);
                break;
            case DriveDefect.NaNTargetSpeed: // CompiledSceneRecord.revolute_drives[0].target_speed := NaN
                BinaryPrimitives.WriteSingleLittleEndian(request.AsSpan(Slot(Assert.Single(Rows(scene, 32)), 1)), float.NaN);
                break;
            case DriveDefect.OverspeedTarget:
                BinaryPrimitives.WriteSingleLittleEndian(request.AsSpan(Slot(Assert.Single(Rows(scene, 32)), 1)), MathF.BitIncrement(128f));
                break;
            case DriveDefect.ZeroTorque: // revolute_drives[0].max_torque := 0
                BinaryPrimitives.WriteSingleLittleEndian(request.AsSpan(Slot(Assert.Single(Rows(scene, 32)), 2)), 0f);
                break;
            case DriveDefect.UndefinedConsumerKind: // supply_nodes[last].consumers[0].kind := 2
                BinaryPrimitives.WriteUInt32LittleEndian(request.AsSpan(Slot(Assert.Single(Rows(Rows(scene, 18)[^1], 3)), 0)), 2);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(defect));
        }

        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(request)).Reason);
    }

    private static WorkshopConstruction TrampolineConstruction => new(new(2), WorkshopCadenceSettings.Default(), new(
        WorkshopInput.Generic(new PartId("trampoline"), new(1), 0, 3, 0, 0, 0, 0, 1)));

    public enum CompliantSurfaceDefect { CollapsedFootprint, UnadmittedBody, ZeroArrivalDepth }

    // Story 10.5: a compliant-surface row whose footprint half extent is zero, whose frame body is not in the scene, or whose arrival
    // depth is zero is refused at both decode boundaries that carry a compiled scene, and the refused command leaves the installed
    // Trampoline world unchanged.
    [Theory]
    [InlineData(CompliantSurfaceDefect.CollapsedFootprint)]
    [InlineData(CompliantSurfaceDefect.UnadmittedBody)]
    [InlineData(CompliantSurfaceDefect.ZeroArrivalDepth)]
    public async Task MalformedCompliantSurfaceRowsRefuseAtCommandAndResponseBoundariesAndLeaveTheInstalledWorldUnchanged(CompliantSurfaceDefect defect)
    {
        await using var world = await WorkshopIntegrationHarness.Building(TrampolineConstruction);
        var before = world.Committed;
        var original = world.Simulation.Scene;
        var request = WorkshopWireFlatBuffers.EncodeCommand(world.Command(WorkshopCommandKind.Construct, TrampolineConstruction with { Revision = new(3) }));
        var response = WorkshopWireFlatBuffers.EncodeResponse(world.Latest with { Scene = original });
        Assert.Single(WorkshopWireFlatBuffers.DecodeCommand(request).Scene!.Physics.CompliantSurfaces.ToArray());
        Assert.Single(WorkshopWireFlatBuffers.DecodeResponse(response).Scene!.Physics.CompliantSurfaces.ToArray());
        Poison(request, 11); // WorkshopCommand.scene
        Poison(response, 27); // WorkshopResponse.scene

        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => world.Router.Dispatch(request)).Reason);
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(response)).Reason);
        Assert.Same(original, world.Simulation.Scene);
        Assert.Equal(before, world.Committed);

        void Poison(byte[] bytes, int sceneField)
        {
            var buffer = new ByteBuffer(bytes);
            var row = Assert.Single(Rows(Child(new Table(4 + buffer.GetInt(4), buffer), sceneField), 31)); // CompiledSceneRecord.compliant_surfaces
            switch (defect)
            {
                case CompliantSurfaceDefect.CollapsedFootprint: // half_x := 0
                    BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(Slot(row, 2)), 0f);
                    break;
                case CompliantSurfaceDefect.UnadmittedBody: // body names no scene body
                    BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(Slot(row, 1)), ulong.MaxValue);
                    break;
                case CompliantSurfaceDefect.ZeroArrivalDepth: // arrival_depth := 0
                    BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(Slot(row, 8)), 0f);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(defect));
            }
        }
    }

    private static void Mutate(byte[] bytes, Defect defect)
    {
        var buffer = new ByteBuffer(bytes);
        var root = new Table(4 + buffer.GetInt(4), buffer);
        var scene = Child(root, 11); // WorkshopCommand.scene
        var bodies = Rows(scene, 6); // CompiledSceneRecord.bodies
        var body = bodies.Single(row => ReadUInt(row, 1) == (uint)RigidMotionKind.Dynamic);
        switch (defect)
        {
            case Defect.UnknownMotion: WriteUInt(body, 1, uint.MaxValue); break;
            case Defect.NarrowedMotion: WriteUInt(body, 1, 256); break;
            case Defect.NonfiniteMass:
                BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(Slot(body, 7)), float.NaN); break;
            case Defect.ForeignColliderBody:
                BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(Slot(Rows(scene, 7)[0], 1)), ulong.MaxValue); break;
            case Defect.ForeignTrigger:
                BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(Slot(Rows(scene, 22)[0], 3)), ulong.MaxValue); break;
            case Defect.WrongSubsteps: WriteUInt(scene, 25, 2); break;
            default: throw new ArgumentOutOfRangeException(nameof(defect));
        }
        void WriteUInt(Table table, int field, uint value) =>
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(Slot(table, field)), value);
    }

    private static uint ReadUInt(Table table, int field)
    {
        var offset = table.__offset(4 + field * 2);
        return offset == 0 ? 0 : table.bb.GetUint(table.bb_pos + offset);
    }
    private static int Slot(Table table, int field)
    {
        var offset = table.__offset(4 + field * 2);
        Assert.NotEqual(0, offset); return table.bb_pos + offset;
    }
    private static Table Child(Table table, int field) => new(table.__indirect(Slot(table, field)), table.bb);
    private static Table[] Rows(Table table, int field)
    {
        var offset = table.__offset(4 + field * 2);
        Assert.NotEqual(0, offset);
        var start = table.__vector(offset);
        return Enumerable.Range(0, table.__vector_len(offset))
            .Select(index => new Table(table.__indirect(start + 4 * index), table.bb)).ToArray();
    }
}
