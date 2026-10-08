namespace CuriousContraptions.Tests;

public class BinaryCircuitTests
{
    public static IEnumerable<object[]> TruthRows()
    {
        foreach(var row in LogicGateTests.TruthRows())
            foreach(var supply in new[]{false,true})yield return [..row,supply];
    }
    [Theory]
    [MemberData(nameof(TruthRows))]
    public void AllOperationsRequireIndependentSupply(LogicGateKind operation,bool first,bool second,bool expected,bool supply)
    {
        var circuit=new BinaryCircuit(6,[new(0),new(1),new(2)],[new(new(4),new(5))],[],
            [new(operation,new(0),new(1),new(2),new(4))]);
        var result=new bool[6];
        circuit.Solve([first,second,supply],[],result);
        Assert.Equal(supply&&expected,result[4]);Assert.Equal(result[4],result[5]);
        circuit.Solve([false,false,false],[],result);
        Assert.All(result,value=>Assert.False(value));
    }

    [Theory]
    [InlineData(LogicGateKind.And)]
    [InlineData(LogicGateKind.Or)]
    public void MonotoneFeedbackIsTheLeastFixedPointAcrossContactChanges(LogicGateKind operation)
    {
        var circuit=new BinaryCircuit(5,[new(0),new(1)],[
            new(new(3),new(2)),new(new(2),new(3))],[new(new(0),new(2))],
            [new(operation,new(2),new(0),new(1),new(4))]);
        var result=new bool[5];
        circuit.Solve([true,true],[true],result);
        Assert.True(result[2]);Assert.True(result[3]);Assert.True(result[4]);
        circuit.Solve([false,true],[true],result);
        Assert.False(result[2]);Assert.False(result[3]);Assert.False(result[4]);
        circuit.Solve([true,true],[false],result);
        Assert.False(result[2]);Assert.False(result[3]);
        Assert.Equal(operation==LogicGateKind.Or,result[4]);
    }

    [Theory]
    [InlineData(LogicGateKind.Xor)]
    [InlineData(LogicGateKind.Nor)]
    [InlineData(LogicGateKind.Nand)]
    public void PotentialContactFeedbackIsRejectedBeforeAnySolve(LogicGateKind operation)
    {
        var error=Assert.Throws<CircuitFeedbackException>(()=>new BinaryCircuit(4,[new(2)],[],
            [new(new(3),new(0))],[new(operation,new(0),new(1),new(2),new(3))]));
        Assert.Equal(new CircuitNodeId[]{new(0),new(3)},error.Nodes.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReconvergenceSettlesBeforeNonmonotoneConsumers(bool reverse)
    {
        CircuitWire[] wires=[new(new(0),new(3)),new(new(0),new(4)),new(new(4),new(5))];
        if(reverse)Array.Reverse(wires);
        var circuit=new BinaryCircuit(7,[new(0),new(1)],wires,[],
            [new(LogicGateKind.Xor,new(3),new(5),new(1),new(6))]);
        var result=new bool[7];circuit.Solve([true,true],[],result);
        Assert.True(result[3]);Assert.True(result[5]);Assert.False(result[6]);
    }

    [Fact]
    public void DeepDependencyChainHasNoCallStackLimitAndAllocatesNothingPerSolve()
    {
        const int count=12000;
        var wires=new CircuitWire[count-1];
        for(var i=0;i<wires.Length;i++)wires[i]=new(new(i+1),new(i));
        var circuit=new BinaryCircuit(count,[new(count-1)],wires,[],[]);
        var result=new bool[count];bool[] sources=[true];
        for(var i=0;i<100;i++)circuit.Solve(sources,[],result);
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<1000;i++)circuit.Solve(sources,[],result);
        var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Console.WriteLine($"Binary circuit: {count} nodes, 1000 warmed solves, {allocated} managed bytes.");
        Assert.Equal(0,allocated);Assert.All(result,value=>Assert.True(value));
        circuit.Solve([false],[],result);Assert.All(result,value=>Assert.False(value));
    }

    [Fact]
    public void OwnsDeclarationsAndRejectedSolveLeavesOutputUnchanged()
    {
        CircuitNodeId[] sources=[new(0)];CircuitWire[] wires=[new(new(0),new(1))];
        CircuitContact[] contacts=[new(new(1),new(2))];
        var circuit=new BinaryCircuit(3,sources,wires,contacts,[]);
        sources[0]=new(2);wires[0]=new(new(2),new(1));contacts[0]=new(new(2),new(0));
        var result=new bool[3];circuit.Solve([true],[true],result);
        Assert.All(result,value=>Assert.True(value));
        Assert.Throws<ArgumentException>(()=>circuit.Solve([], [true],result));
        Assert.Throws<ArgumentException>(()=>circuit.Solve([true],[],result));
        Assert.Throws<ArgumentException>(()=>circuit.Solve([true],[true],new bool[2]));
        Assert.All(result,value=>Assert.True(value));
        circuit.Solve([false],[false],result);Assert.All(result,value=>Assert.False(value));
    }

    [Fact]
    public void SourcesAndContactsMayAliasResultWithoutChangingInputsMidSolve()
    {
        var circuit=new BinaryCircuit(2,[new(0),new(1)],[],[new(new(0),new(1)),new(new(1),new(0))],[]);
        bool[] source=[true,false];bool[] contacts=[true,false];
        circuit.Solve(source,contacts,source);Assert.Equal(new[]{true,true},source);
        source=[true,false];circuit.Solve(source,contacts,contacts);Assert.Equal(new[]{true,true},contacts);
    }

    [Fact]
    public void RejectsInvalidDeclarationsAndAcceptsEmptyTopology()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new CircuitNodeId(-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new BinaryCircuit(-1,[],[],[],[]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new BinaryCircuit(1,[new(1)],[],[],[]));
        Assert.Throws<ArgumentException>(()=>new BinaryCircuit(1,[new(0),new(0)],[],[],[]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new BinaryCircuit(1,[],[new(new(0),new(1))],[],[]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new BinaryCircuit(1,[],[],[new(new(1),new(0))],[]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new BinaryCircuit(4,[],[],[],
            [new((LogicGateKind)999,new(0),new(1),new(2),new(3))]));
        Assert.Throws<ArgumentException>(()=>new BinaryCircuit(4,[],[],[],
            [new(LogicGateKind.And,new(0),new(0),new(2),new(3))]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new BinaryCircuit(4,[],[],[],
            [new(LogicGateKind.And,new(0),new(1),new(2),new(4))]));
        new BinaryCircuit(0,[],[],[],[]).Solve([],[],[]);
    }
}

