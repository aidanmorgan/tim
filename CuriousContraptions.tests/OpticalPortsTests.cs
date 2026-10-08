using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class OpticalPortsTests(NativeSceneFixture godot)
{
    private partial class Probe : MachinePart
    {
        public OpticalSurface[] Surfaces { get; set; }=[];
        public OpticalEmitter? Emission { get; set; }
        public IReadOnlyDictionary<OpticalPortId,Vector3> Reading { get; private set; }=new Dictionary<OpticalPortId,Vector3>();
        public int Commits { get; private set; }
        public Action? OnCommit { get; set; }
        public override IReadOnlyList<OpticalSurface> OpticalSurfaces=>Surfaces;
        public override OpticalEmitter? OpticalSource=>Emission;
        public override void ReceiveOpticalPower(IReadOnlyDictionary<OpticalPortId,Vector3> power)
        {
            Reading=new Dictionary<OpticalPortId,Vector3>(power);Commits++;OnCommit?.Invoke();
        }
    }
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    private static OpticalSurface Target(OpticalPortId id,float z)=>
        new(id,new(new(0,0,z),Vector3.Left,.4f),OpticalInteraction.Absorb,Vector3.One);
    private static Probe Source(float z,Vector3 power)=>new()
    {
        Position=new(-3,6,z),Emission=new(Vector3.Zero,Vector3.Right,10,power)
    };
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultipleInputsAggregateIndependentlyAndCommitOnce(bool reverse)
    {
        var world=World();
        var target=new Probe {Position=new(0,6,0),Surfaces=[Target(OpticalPortId.First,-1),Target(OpticalPortId.Second,1)]};
        var red=Source(-1,Vector3.Right);var green=Source(1,Vector3.Up);var extra=Source(-1,Vector3.Right*.5f);
        try
        {
            FixtureParts.Attach(world,target,reverse?FixturePartId.Fourth:FixturePartId.First);
            FixtureParts.Attach(world,red,reverse?FixturePartId.Third:FixturePartId.Second);
            FixtureParts.Attach(world,green,reverse?FixturePartId.Second:FixturePartId.Third);
            FixtureParts.Attach(world,extra,reverse?FixturePartId.First:FixturePartId.Fourth);
            var trace=OpticalNetwork.Trace(world,red,red.Emission!.Value);
            Assert.Equal(OpticalPortId.First,Assert.Single(trace.Receptions).Port);
            Assert.Empty(target.Reading);Assert.Equal(0,target.Commits);
            OpticalNetwork.Solve(world);
            Assert.Equal(Vector3.Right*1.5f,target.Reading[OpticalPortId.First]);
            Assert.Equal(Vector3.Up,target.Reading[OpticalPortId.Second]);
            Assert.Equal(1,target.Commits);
            green.Emission=null;
            OpticalNetwork.Solve(world);
            Assert.Equal(Vector3.Zero,target.Reading[OpticalPortId.Second]);
            Assert.Equal(Vector3.Right*1.5f,target.Reading[OpticalPortId.First]);
            target.Visible=false;
            OpticalNetwork.Solve(world);
            Assert.All(target.Reading.Values,p=>Assert.Equal(Vector3.Zero,p));
        }
        finally{world.Free();}
    }
    [Fact]
    public void RotatedPortGeometryAndOcclusionDoNotLeakBetweenInputs()
    {
        var world=World();
        var target=new Probe {Position=new(0,6,0),Surfaces=[Target(OpticalPortId.First,-1),Target(OpticalPortId.Second,1)]};
        var first=Source(-1,Vector3.One);var second=Source(1,Vector3.One);
        try
        {
            FixtureParts.Attach(world,target,FixturePartId.First);
            FixtureParts.Attach(world,first,FixturePartId.Second);
            FixtureParts.Attach(world,second,FixturePartId.Third);
            var around=new Transform3D(Basis.FromEuler(new(.2f,.3f,.4f)),new(0,2,0));
            foreach(var p in new Probe[]{target,first,second})p.Transform=around*p.Transform;
            var blocker=world.AddPart(new(){Id="blocker",Kind="wall",Position=[-1.5f,6,-1]});
            blocker.Transform=around*blocker.Transform;
            OpticalNetwork.Solve(world);
            Assert.Equal(Vector3.Zero,target.Reading[OpticalPortId.First]);
            Assert.Equal(Vector3.One,target.Reading[OpticalPortId.Second]);
            blocker.Visible=false;
            OpticalNetwork.Solve(world);
            Assert.Equal(Vector3.One,target.Reading[OpticalPortId.First]);
            Assert.Equal(Vector3.One,target.Reading[OpticalPortId.Second]);
        }
        finally{world.Free();}
    }
    [Fact]
    public void AllRaysAreTracedBeforeAnyReceiverCommits()
    {
        var world=World();
        var target=new Probe {Position=new(0,6,0),Surfaces=[Target(OpticalPortId.First,-1),Target(OpticalPortId.Second,1)]};
        var first=Source(-1,Vector3.Right);var second=Source(1,Vector3.Up);
        try
        {
            first.OnCommit=()=>second.Emission=null;
            FixtureParts.Attach(world,first,FixturePartId.First);
            FixtureParts.Attach(world,target,FixturePartId.Second);
            FixtureParts.Attach(world,second,FixturePartId.Third);
            OpticalNetwork.Solve(world);
            Assert.Equal(Vector3.Up,target.Reading[OpticalPortId.Second]);
            OpticalNetwork.Solve(world);
            Assert.Equal(Vector3.Zero,target.Reading[OpticalPortId.Second]);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)]
    public void InvalidAperturesAreRejectedRatherThanSilentlySubstituted(int fault)
    {
        var world=World();
        var target=new Probe();
        var source=Source(0,Vector3.One);
        try
        {
            var valid=Target(OpticalPortId.Main,0);
            var invalid=fault switch
            {
                0=>valid with{Id=(OpticalPortId)99},
                1=>valid,
                2=>valid with{Aperture=valid.Aperture with{Radius=0}},
                3=>valid with{Aperture=valid.Aperture with{Normal=Vector3.Zero}},
                4=>valid with{Transmission=new(1.1f,1,1)},
                5=>valid with{Transmission=new(float.NaN,1,1)},
                6=>valid with{Interaction=(OpticalInteraction)99},
                7=>valid with{Aperture=valid.Aperture with{At=new(float.PositiveInfinity,0,0)}},
                8=>valid with{Interaction=OpticalInteraction.Route}, // No routed output declared.
                _=>throw new ArgumentOutOfRangeException(nameof(fault))
            };
            target.Surfaces=fault==1?[valid,invalid]:[invalid];
            FixtureParts.Attach(world,source,FixturePartId.First);
            FixtureParts.Attach(world,target,FixturePartId.Second);
            Assert.Throws<InvalidOperationException>(()=>OpticalNetwork.Solve(world));
            Assert.Equal(0,target.Commits);
        }
        finally{world.Free();}
    }
}
