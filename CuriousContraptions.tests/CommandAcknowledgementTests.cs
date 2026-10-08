using CuriousContraptions.Bridge;

namespace CuriousContraptions.Tests;

public class CommandAcknowledgementTests
{
    private enum Input { Enable, Disable }
    private static CommandEnvelope<Input> Command(long sequence,long generation=1)=>
        new(new(generation),new(sequence),null,Input.Enable);
    private static void Commit(SimulationCommandInbox<Input> inbox,long revision)
    {
        inbox.BeginTransaction();
        while(inbox.TryPeek(out _))inbox.Complete(CommandOutcome.Applied,new(revision));
        inbox.CommitTransaction();
    }

    [Fact]
    public void RepeatedPeekRetainsCapacityUntilTheExactHeadIsAcknowledged()
    {
        var inbox=new SimulationCommandInbox<Input>(new(1),2);
        inbox.Admit(Command(1));inbox.Admit(Command(2));Commit(inbox,1);
        Assert.Equal(2,inbox.ResultCount);
        Assert.True(inbox.TryPeekResult(out var first));
        for(var i=0;i<10;i++)
        {
            Assert.True(inbox.TryPeekResult(out var repeated));Assert.Equal(first,repeated);
            Assert.Equal(CommandAdmission.Full,inbox.Admit(Command(3)));
        }
        Assert.Throws<ArgumentException>(()=>inbox.AcknowledgeResult(default));
        Assert.Throws<InvalidOperationException>(()=>inbox.AcknowledgeResult(new(new(1),new(2))));
        Assert.Throws<InvalidOperationException>(()=>inbox.AcknowledgeResult(new(new(2),new(1))));
        Assert.Equal(2,inbox.ResultCount);
        inbox.AcknowledgeResult(first.Id);
        Assert.Equal(1,inbox.ResultCount);
        Assert.Throws<InvalidOperationException>(()=>inbox.AcknowledgeResult(first.Id));
        Assert.Equal(CommandAdmission.Accepted,inbox.Admit(Command(3)));
        Assert.True(inbox.TryPeekResult(out var second));Assert.Equal(new CommandSequence(2),second.Sequence);
        inbox.AcknowledgeResult(second.Id);
        Assert.False(inbox.TryPeekResult(out _));
        Assert.Throws<InvalidOperationException>(()=>inbox.AcknowledgeResult(second.Id));
        Assert.Equal(CommandAdmission.OutOfOrder,inbox.Admit(Command(1)));
    }

    [Fact]
    public void BarrierAndReusedSequenceCannotConfuseOldAndNewResults()
    {
        var inbox=new SimulationCommandInbox<Input>(new(1),3);
        inbox.Admit(Command(1));Commit(inbox,1);inbox.Admit(Command(2));
        Assert.True(inbox.TryPeekResult(out var old));
        inbox.AdvanceGeneration(new(2),new(1));
        inbox.Admit(Command(1,2));Commit(inbox,1);
        Assert.Equal(3,inbox.ResultCount);
        Assert.True(inbox.TryPeekResult(out var retained));Assert.Equal(old,retained);
        inbox.AcknowledgeResult(old.Id);
        Assert.True(inbox.TryPeekResult(out var invalidated));
        Assert.Equal(CommandOutcome.InvalidatedByBarrier,invalidated.Outcome);
        Assert.Equal(new WorldGeneration(1),invalidated.Generation);
        inbox.AcknowledgeResult(invalidated.Id);
        Assert.True(inbox.TryPeekResult(out var current));
        Assert.Equal(old.Sequence,current.Sequence);Assert.NotEqual(old.Id,current.Id);
        Assert.Throws<InvalidOperationException>(()=>inbox.AcknowledgeResult(old.Id));
        Assert.True(inbox.TryPeekResult(out retained));Assert.Equal(current,retained);
        inbox.AcknowledgeResult(current.Id);
        Assert.Equal(0,inbox.ResultCount);
    }

    [Fact]
    public void FailedApplicationCannotAcknowledgeOrExposeEarlierResults()
    {
        var inbox=new SimulationCommandInbox<Input>(new(1),2);
        inbox.Admit(Command(1));Commit(inbox,1);
        Assert.True(inbox.TryPeekResult(out var committed));
        inbox.Admit(Command(2));inbox.BeginTransaction();
        inbox.Complete(CommandOutcome.Applied,new(2));
        Assert.Throws<InvalidOperationException>(()=>inbox.AcknowledgeResult(committed.Id));
        Assert.Throws<InvalidOperationException>(()=>inbox.ResultCount);
        Assert.Throws<InvalidOperationException>(()=>inbox.TryPeekResult(out _));
        inbox.RollbackTransaction();
        Assert.Equal(1,inbox.ResultCount);Assert.Equal(1,inbox.PendingCount);
        Assert.True(inbox.TryPeekResult(out var retained));Assert.Equal(committed,retained);
        inbox.AcknowledgeResult(committed.Id);Commit(inbox,2);
        Assert.True(inbox.TryPeekResult(out var retried));
        Assert.Equal(new CommandSequence(2),retried.Sequence);Assert.Equal(new SimulationRevision(2),retried.Revision);
        inbox.AcknowledgeResult(retried.Id);
        Assert.False(inbox.TryPeekResult(out _));
    }

    [Fact]
    public void CommandIdentityRejectsUninitializedFields()
    {
        Assert.Throws<ArgumentException>(()=>new CommandId(default,new(1)));
        Assert.Throws<ArgumentException>(()=>new CommandId(new(1),default));
    }
}
