using CuriousContraptions.Bridge;

namespace CuriousContraptions.Tests;

public class SimulationCommandInboxTests
{
    private enum Input { Disable, Enable }
    private static CommandEnvelope<Input> Command(long sequence,Input input=Input.Enable,long generation=1)=>
        new(new(generation),new(sequence),new(0),input);

    [Fact]
    public void ApplicationResultsAreHiddenUntilCommitAndRollbackRestoresPendingOrder()
    {
        var inbox=new SimulationCommandInbox<Input>(new(1),3);
        Assert.Equal(CommandAdmission.Accepted,inbox.Admit(Command(1)));
        Assert.Equal(CommandAdmission.Accepted,inbox.Admit(Command(2,Input.Disable)));
        var state=new SimulationState<Input>(Input.Disable);
        var transaction=new SimulationTransaction([inbox,state]);
        transaction.Begin();
        Assert.True(inbox.TryPeek(out var first));state.Value=first.Payload;
        inbox.Complete(CommandOutcome.Applied,new(1));
        Assert.Throws<InvalidOperationException>(()=>inbox.TryPeekResult(out _));
        Assert.Throws<InvalidOperationException>(()=>inbox.Admit(Command(3)));
        transaction.Rollback();
        Assert.Equal(Input.Disable,state.Value);
        Assert.Equal(2,inbox.PendingCount);
        Assert.False(inbox.TryPeekResult(out _));
        transaction.Begin();
        Assert.True(inbox.TryPeek(out first));Assert.Equal(new CommandSequence(1),first.Sequence);
        state.Value=first.Payload;inbox.Complete(CommandOutcome.Applied,new(1));
        Assert.True(inbox.TryPeek(out var second));Assert.Equal(Input.Disable,second.Payload);
        state.Value=second.Payload;inbox.Complete(CommandOutcome.Applied,new(2));
        Assert.False(inbox.TryPeek(out _));transaction.Commit();
        Assert.True(inbox.TryPeekResult(out var one));Assert.Equal(new SimulationRevision(1),one.Revision);
        inbox.AcknowledgeResult(one.Id);
        Assert.True(inbox.TryPeekResult(out var two));Assert.Equal(new CommandSequence(2),two.Sequence);
        inbox.AcknowledgeResult(two.Id);
        Assert.False(inbox.TryPeekResult(out _));
        Assert.Equal(CommandAdmission.OutOfOrder,inbox.Admit(Command(1)));
    }

    [Fact]
    public void LaggingReaderCannotBlockBarrierInvalidationOrLoseOldResults()
    {
        var inbox=new SimulationCommandInbox<Input>(new(1),2);
        Assert.Equal(CommandAdmission.Accepted,inbox.Admit(Command(1)));
        inbox.BeginTransaction();inbox.Complete(CommandOutcome.Applied,new(1));inbox.CommitTransaction();
        Assert.Equal(CommandAdmission.Accepted,inbox.Admit(Command(2)));
        Assert.Equal(CommandAdmission.Full,inbox.Admit(Command(3)));
        Assert.Equal(2,inbox.HighWater);
        inbox.AdvanceGeneration(new(2),new(1));
        Assert.Equal(CommandAdmission.WrongGeneration,inbox.Admit(Command(3)));
        Assert.Equal(CommandAdmission.Full,inbox.Admit(Command(1,generation:2)));
        Assert.True(inbox.TryPeekResult(out var applied));Assert.Equal(CommandOutcome.Applied,applied.Outcome);
        inbox.AcknowledgeResult(applied.Id);
        Assert.Equal(CommandAdmission.Accepted,inbox.Admit(Command(1,generation:2)));
        Assert.True(inbox.TryPeekResult(out var cancelled));
        inbox.AcknowledgeResult(cancelled.Id);
        Assert.Equal(CommandOutcome.InvalidatedByBarrier,cancelled.Outcome);
        Assert.Equal(new WorldGeneration(1),cancelled.Generation);
        inbox.BeginTransaction();inbox.Complete(CommandOutcome.StaleRevision,new(0));inbox.CommitTransaction();
        Assert.True(inbox.TryPeekResult(out var rejected));Assert.Equal(CommandOutcome.StaleRevision,rejected.Outcome);
        inbox.AcknowledgeResult(rejected.Id);
        Assert.Equal(new WorldGeneration(2),rejected.Generation);
    }

    [Fact]
    public void RollbackPreservesUnreadCommittedResultsAcrossRingWrap()
    {
        var inbox=new SimulationCommandInbox<Input>(new(1),2);
        for(var i=1;i<=8;i++)
        {
            Assert.Equal(CommandAdmission.Accepted,inbox.Admit(Command(i)));
            inbox.BeginTransaction();inbox.Complete(CommandOutcome.Applied,new(i));inbox.CommitTransaction();
            Assert.True(inbox.TryPeekResult(out var consumed));
            inbox.AcknowledgeResult(consumed.Id);
        }
        inbox.Admit(Command(9));inbox.BeginTransaction();
        inbox.Complete(CommandOutcome.InvalidCapability,new(9));inbox.CommitTransaction();
        inbox.Admit(Command(10));inbox.BeginTransaction();
        inbox.Complete(CommandOutcome.Applied,new(10));inbox.RollbackTransaction();
        Assert.True(inbox.TryPeekResult(out var prior));Assert.Equal(new CommandSequence(9),prior.Sequence);
        inbox.AcknowledgeResult(prior.Id);
        Assert.False(inbox.TryPeekResult(out _));
        inbox.BeginTransaction();Assert.True(inbox.TryPeek(out var pending));
        Assert.Equal(new CommandSequence(10),pending.Sequence);
        inbox.Complete(CommandOutcome.InvalidMode,new(9));inbox.CommitTransaction();
        Assert.True(inbox.TryPeekResult(out var result));Assert.Equal(CommandOutcome.InvalidMode,result.Outcome);
        inbox.AcknowledgeResult(result.Id);
    }

    [Fact]
    public void InvalidBoundariesAndWrongPhasesRejectWithoutConsumption()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new WorldGeneration(0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new CommandSequence(0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new SimulationRevision(-1));
        Assert.Throws<ArgumentException>(()=>new SimulationCommandInbox<Input>(default,1));
        var inbox=new SimulationCommandInbox<Input>(new(1),1);
        Assert.Throws<ArgumentException>(()=>inbox.Admit(default));
        Assert.Throws<InvalidOperationException>(()=>inbox.TryPeek(out _));
        inbox.Admit(Command(1));inbox.BeginTransaction();
        Assert.Throws<ArgumentOutOfRangeException>(()=>inbox.Complete((CommandOutcome)999,new(0)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>inbox.Complete(CommandOutcome.InvalidatedByBarrier,new(0)));
        Assert.Throws<InvalidOperationException>(()=>inbox.AdvanceGeneration(new(2),new(0)));
        Assert.Equal(1,inbox.PendingCount);inbox.RollbackTransaction();
        Assert.Throws<ArgumentOutOfRangeException>(()=>inbox.AdvanceGeneration(new(1),new(0)));
    }

    [Fact]
    public void WarmedAdmissionCommitReadCyclesAllocateNoManagedMemory()
    {
        var inbox=new SimulationCommandInbox<Input>(new(1),4);
        void Cycle(long sequence)
        {
            inbox.Admit(Command(sequence));inbox.BeginTransaction();
            inbox.Complete(CommandOutcome.Applied,new(sequence));inbox.CommitTransaction();
            inbox.TryPeekResult(out var result);inbox.AcknowledgeResult(result.Id);
        }
        for(var i=1;i<=100;i++)Cycle(i);
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=101;i<=2100;i++)Cycle(i);
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
    }
}
