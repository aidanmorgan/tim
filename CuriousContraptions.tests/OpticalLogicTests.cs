using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class OpticalLogicTests(NativeSceneFixture godot)
{
    private partial class Source : MachinePart
    {
        public Vector3 Direction { get; set; }
        public Vector3 Power { get; set; }
        public override OpticalEmitter? OpticalSource => new(Vector3.Zero,Direction,12,Power);
    }
    public static IEnumerable<object[]> Cases()
    {
        foreach(var row in LogicGateTests.TruthRows())
            foreach(var carrier in new[]{false,true})
                yield return [..row,carrier];
    }
    private static string Catalog(LogicGateKind kind) => kind switch
    {
        LogicGateKind.And=>"optical_and", LogicGateKind.Or=>"optical_or",
        LogicGateKind.Xor=>"optical_xor", LogicGateKind.Nor=>"optical_nor",
        LogicGateKind.Nand=>"optical_nand", _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    [Theory]
    [MemberData(nameof(Cases))]
    public void ActualBeamsRespectTruthCarrierLossAndSnapshot(LogicGateKind kind,bool first,bool second,bool expected,bool carrier)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        var a=new Source {Position=new(-4,6,0),Direction=Vector3.Right,Power=first?Vector3.One:Vector3.Zero};
        var b=new Source {Position=new(0,10,0),Direction=Vector3.Down,Power=second?Vector3.One:Vector3.Zero};
        var c=new Source {Position=new(0,6,4),Direction=Vector3.Forward,Power=carrier?Vector3.One:Vector3.Zero};
        try
        {
            var gate=(OpticalLogicPart)world.AddPart(new(){Id="gate",Kind=Catalog(kind),Position=[0,6,0]});
            FixtureParts.Attach(world,a,FixturePartId.First);
            FixtureParts.Attach(world,b,FixturePartId.Second);
            FixtureParts.Attach(world,c,FixturePartId.Third);
            var initial=gate.IsOpen;
            OpticalNetwork.Solve(world);
            Assert.Equal(first,gate.First);
            Assert.Equal(second,gate.Second);
            Assert.Equal(initial,gate.IsOpen);
            gate.BeforeNetworks(world);
            OpticalNetwork.Solve(world);
            Assert.Equal(expected,gate.IsOpen);
            Assert.Equal(expected&&carrier,gate.Active);
            Assert.Equal(expected&&carrier?Vector3.One*.9f:Vector3.Zero,gate.OutputPower);
            // Controls are absorbed; only the carrier can leave the gate.
            Assert.All(world.OpticalPaths.Where(s=>s.OriginPart==gate.OpticalIdentity),s=>Assert.Equal(Vector3.One*.9f,s.Power));
            c.Power=Vector3.Zero;
            OpticalNetwork.Solve(world);
            Assert.False(gate.Active);
            Assert.Equal(Vector3.Zero,gate.OutputPower);
            // Reconfiguration restores control and pending truth state.
            gate.Configure(new(){Id="gate",Kind=Catalog(kind),Position=[0,6,0]});
            Assert.False(gate.First);
            Assert.False(gate.Second);
            Assert.Equal(LogicGate.Evaluate(kind,false,false),gate.IsOpen);
        }
        finally
        {
            world.Free();
        }
    }
}
