using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopWireTests
{
    private static WorkshopConstruction Construction => new(new(2),
        WorkshopInput.Basketball(new(0x20000000000001), 0, 4, 0, 0, 0, 0, 1));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OriginalAckRemainsExactAfterNewerReadOrSameRevisionFault(bool sameRevisionFault)
    {
        var cursor = new WorkshopClientCursor();
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Run, new(1), default, null);
        var dispatched = cursor.ReadOrder;
        var ack = new WorkshopResponse(new(1), WorkshopResponseKind.Acknowledgement,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
            new(new(1), new(0), null, new(1)));
        var original = WorkshopWire.Encode(ack);
        var later = new WorkshopResponse(default, WorkshopResponseKind.Read,
            new(sameRevisionFault ? WorkshopCommandOutcome.Faulted : WorkshopCommandOutcome.Applied,
                sameRevisionFault ? WorkshopRejection.DeviceLost : WorkshopRejection.None),
            sameRevisionFault ? WorkshopSimulationPhase.Faulted : WorkshopSimulationPhase.Running,
            new(new(1), new(sameRevisionFault ? 0UL : 1UL), null, new(sameRevisionFault ? 1UL : 2UL)));
        Assert.True(cursor.AcceptRead(later));
        var delivery = cursor.Acknowledge(command, ack, dispatched, false);
        Assert.False(delivery.Applicable);
        Assert.Equal(original, WorkshopWire.Encode(delivery.Response));
        Assert.Equal(later.Read.Revision, cursor.Revision);
    }

    [Fact]
    public void FutureReadCannotInstallEpochAndOnlyOwnedSuccessCanAdvanceIt()
    {
        var cursor = new WorkshopClientCursor();
        var future = new WorkshopResponse(default, WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Building,
            new(new(2), default, null, new(1)));
        Assert.Throws<ArgumentException>(() => cursor.AcceptRead(future));
        Assert.Equal(new SimulationEpoch(1), cursor.Epoch);
        Assert.Equal(default, cursor.Revision);
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Reset, new(1), default, null,
            RevisionKind: ExpectedRevisionKind.Any);
        var ack = future with { Sequence = new(1), Kind = WorkshopResponseKind.Acknowledgement };
        Assert.Throws<ArgumentException>(() => cursor.Acknowledge(command with { Kind = WorkshopCommandKind.Run }, ack, 0, false));
        Assert.True(cursor.Acknowledge(command, ack, 0, false).Applicable);
        Assert.Equal(new SimulationEpoch(2), cursor.Epoch);
        var retired = cursor.Acknowledge(command with { Epoch = new(2) },
            ack with { Read = ack.Read with { Epoch = new(3), Revision = new(2) } }, 0, true);
        Assert.False(retired.Applicable);
        Assert.Equal(WorkshopCommandOutcome.Applied, retired.Response.Result.Outcome);
        Assert.Equal(new SimulationEpoch(2), cursor.Epoch);
        var old = cursor.Acknowledge(command, ack with { Read = ack.Read with { Epoch = new(1) } }, 0, false);
        Assert.False(old.Applicable);
        Assert.Equal(new SimulationEpoch(1), old.Response.Read.Epoch);
        Assert.Equal(WorkshopCommandOutcome.Applied, old.Response.Result.Outcome);
    }

    [Fact]
    public void SameEpochAckCannotMoveTickBackwardsEvenWithNewerRevision()
    {
        var cursor = new WorkshopClientCursor();
        var read = new WorkshopResponse(default, WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
            new(new(1), new(7), null, new(8)));
        Assert.True(cursor.AcceptRead(read));
        var command = new WorkshopCommand(new(2), WorkshopCommandKind.Run, new(1), new(8), null);
        var ack = read with { Sequence = new(2), Kind = WorkshopResponseKind.Acknowledgement,
            Read = read.Read with { Tick = new(6), Revision = new(9) } };
        Assert.False(cursor.Acknowledge(command, ack, cursor.ReadOrder, false).Applicable);
        Assert.Equal(new AuthorityRevision(8), cursor.Revision);
        Assert.False(cursor.AcceptRead(read with { Read = read.Read with { Tick = new(6), Revision = new(9) } }));
    }

    [Fact]
    public void AuthorityRevisionIsSeparateAndAnyControlSurvivesLaterTicks()
    {
        var exact = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(3), new(9), Construction);
        Assert.True(WorkshopWire.Matches(exact, new(3), new(9)));
        Assert.False(WorkshopWire.Matches(exact, new(3), new(10)));
        var reset = new WorkshopCommand(new(2), WorkshopCommandKind.Reset, new(3), default, null,
            RevisionKind: ExpectedRevisionKind.Any);
        Assert.True(WorkshopWire.Matches(WorkshopWire.DecodeCommand(WorkshopWire.Encode(reset)), new(3), new(1234)));
        Assert.False(WorkshopWire.Matches(reset, new(4), new(1234)));
        Assert.Throws<ArgumentException>(() => WorkshopWire.Encode(reset with { Revision = new(1) }));
        Assert.Throws<ArgumentException>(() => WorkshopWire.Encode(exact with { Revision = default, RevisionKind = ExpectedRevisionKind.Any }));
        var response = new WorkshopResponse(new(1), WorkshopResponseKind.Acknowledgement,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Building,
            new(new(3), new(0), null, new(ulong.MaxValue)));
        var bytes = WorkshopWire.Encode(response);
        Assert.Equal(144, bytes.Length);
        Assert.Equal(response, WorkshopWire.DecodeResponse(bytes));
        bytes[56] = 1;
        Assert.Throws<ArgumentException>(() => WorkshopWire.DecodeResponse(bytes));
    }

    [Fact]
    public void CancelWireRetainsExactOriginalIdentityAndRejectsMissingTarget()
    {
        var identity = new WorkshopCommandIdentity(new(0x20000000000001), new(19), new(23));
        var command = new WorkshopCommand(new(8), WorkshopCommandKind.Cancel, new(19), new(23), null, identity);
        Assert.Equal(command, WorkshopWire.DecodeCommand(WorkshopWire.Encode(command)));
        Assert.Throws<ArgumentException>(() => WorkshopWire.Encode(command with { Target = null }));
        var bytes = WorkshopWire.Encode(command);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(40), 0);
        Assert.Throws<ArgumentException>(() => WorkshopWire.DecodeCommand(bytes));
    }

    [Fact]
    public void InvalidQuaternionAndOutOfDeckDescriptorReject()
    {
        Assert.Throws<ArgumentException>(() => new CanonicalRotation((Half)1, (Half)1, (Half)1, (Half)1).Validate());
        Assert.Throws<ArgumentException>(() => new CanonicalRotation((Half).001, (Half)0, (Half)0, (Half)0).Validate());
        var bytes = WorkshopGpuAbi.Admission(Construction, new(2));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(32), 1024);
        Assert.Throws<ArgumentException>(() => WorkshopGpuAbi.Read(bytes));
    }

    [Fact]
    public void CanonicalConstructionWireRetainsEveryBitAndWideIdentity()
    {
        var ball = Construction.Ball!.Value with
        {
            Local = new(BitConverter.UInt16BitsToHalf(0x8000), (Half)0.25, (Half)0),
            Rotation = new(BitConverter.UInt16BitsToHalf(0x8000), (Half)0, (Half)0, (Half)1)
        };
        var command = new WorkshopCommand(new(0x20000000000001), WorkshopCommandKind.Construct,
            new(0x30000000000001), new(1), Construction with { Ball = ball });
        var bytes = WorkshopWire.Encode(command);
        Assert.Equal(bytes, WorkshopWire.Encode(WorkshopWire.DecodeCommand(bytes)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ObsoleteResponseSchemaRejectsEmptyAndBall(bool withBall)
    {
        var body = withBall ? new CanonicalBody(new(1), 3, 8, default, default, default) : (CanonicalBody?)null;
        var response = new WorkshopResponse(new(0), WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
            new(new(3), new(8), body));
        var bytes = WorkshopWire.Encode(response);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(52), 17);
        Assert.Throws<ArgumentException>(() => WorkshopWire.DecodeResponse(bytes));
    }

    [Theory]
    [InlineData(80, 0x8000u)]
    [InlineData(82, 0x7e00u)]
    [InlineData(88, uint.MaxValue)]
    [InlineData(92, 0x7e00u)]
    [InlineData(100, uint.MaxValue)]
    [InlineData(104, 2u)]
    [InlineData(108, 1u)]
    public void MalformedOpaqueDescriptorCannotCommit(int offset, uint value)
    {
        var bytes = WorkshopGpuAbi.Admission(Construction, new(2));
        if (offset is 80 or 82 or 92) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), (ushort)value);
        else BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
        Assert.Throws<ArgumentException>(() => WorkshopGpuAbi.Read(bytes));
    }

    [Fact]
    public async Task ActualRouterReplaysOnlyExactPendingAndCompletedCommand()
    {
        var held = new TaskCompletionSource<byte[]>();
        var applied = new List<WorkshopCommand>();
        var router = new WorkshopCommandRouter((command, reserved) => { applied.Add(command); return held.Task; });
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1), Construction);
        var bytes = WorkshopWire.Encode(command);
        var first = router.Dispatch(bytes);
        var duplicate = router.Dispatch((byte[])bytes.Clone());
        Assert.Same(first, duplicate);
        await Assert.ThrowsAsync<ArgumentException>(() => router.Dispatch(WorkshopWire.Encode(command with { Kind = WorkshopCommandKind.Run, Construction = null })));
        await Assert.ThrowsAsync<ArgumentException>(() => router.Dispatch(WorkshopWire.Encode(command with { Epoch = new(2) })));
        var different = command with { Construction = Construction with { Ball = null } };
        await Assert.ThrowsAsync<ArgumentException>(() => router.Dispatch(WorkshopWire.Encode(different)));
        bytes[16] ^= 1; // caller cannot change the retained identity after admission.
        await Assert.ThrowsAsync<ArgumentException>(() => router.Dispatch(bytes));
        held.SetResult([1, 2, 3]);
        Assert.Equal(new byte[] { 1, 2, 3 }, await first);
        Assert.Same(first, router.Dispatch(WorkshopWire.Encode(command)));
        Assert.Single(applied);
    }

    [Fact]
    public async Task MalformedAndOutOfOrderInputsDoNotConsumeNextReliableIdentity()
    {
        var seen = new List<CommandSequence>();
        var router = new WorkshopCommandRouter((command, reserved) => { seen.Add(command.Sequence); return Task.FromResult(Array.Empty<byte>()); });
        var next = new WorkshopCommand(new(1), WorkshopCommandKind.Run, new(1), new(1), null);
        var malformed = WorkshopWire.Encode(next);
        BinaryPrimitives.WriteUInt32LittleEndian(malformed.AsSpan(12), 99);
        await Assert.ThrowsAsync<ArgumentException>(() => router.Dispatch(malformed));
        await Assert.ThrowsAsync<ArgumentException>(() => router.Dispatch(WorkshopWire.Encode(next with { Sequence = new(2) })));
        await router.Dispatch(WorkshopWire.Encode(next));
        Assert.Equal(new[] { new CommandSequence(1) }, seen);
    }
}
