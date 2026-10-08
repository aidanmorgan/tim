namespace CuriousContraptions.Tests;

public class PublicationAllocationStressTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(256)]
    public void CounterPublicationKeepsZeroAllocationAcrossRepeatedColdCollections(int count)
    {
        var publication=new CommittedCounterBufferTests();
        for(var attempt=0;attempt<24;attempt++)
        {
            GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
            GC.WaitForPendingFinalizers();
            publication.WarmedCounterPublicationAllocatesNothing(count);
        }
    }
}
