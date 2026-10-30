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
            original.Physics, original.Activation, tracks, original.Owners, original.EditableSources);
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
