using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class BeamCombinerTests(HeadlessFixture godot)
{
    private partial class ShortEmitter : MachinePart
    {
        public float Range { get; set; }
        public override OpticalEmitter? OpticalSource=>new(Vector3.Zero,Vector3.Right,Range,Vector3.Right);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OutletIndicatorOnlyShowsLightThatActuallyExits(bool sufficientRange)
    {
        var world=World();
        var emitter=new ShortEmitter {Position=new(-2.76f,6,0),Range=sufficientRange?6:2.1f};
        try
        {
            var combiner=(BeamCombinerPart)world.AddPart(new(){Id="combiner",Kind="beam_combiner",Position=[0,6,0]});
            world.Parts.Add(emitter);
            OpticalNetwork.Solve(world);
            Assert.Equal(Vector3.Right,combiner.InputPower);
            Assert.Equal(sufficientRange,combiner.Active);
            Assert.Equal(sufficientRange?Vector3.Right*.9f:Vector3.Zero,combiner.OutputPower);
        }
        finally{world.Parts.Remove(emitter);emitter.Free();world.Free();}
    }
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    [Theory]
    [InlineData(false,false)]
    [InlineData(false,true)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public void ThreeInputsMixWithoutAddingPowerAndResetClearsAllPaths(bool rotate,bool reverse)
    {
        var world=World();
        try
        {
            var combiner=(BeamCombinerPart)world.AddPart(new(){Id="combiner",Kind="beam_combiner",Position=[0,6,0]});
            var receiver=(LightReceiverPart)world.AddPart(new(){Id="receiver",Kind="white_receiver",Position=[4,6,0]});
            var sources=new List<MachinePart>();
            foreach(var (name,position,rotation) in new[]{
                ("red",new Vector3(-4,6,0),Vector3.Zero),
                ("green",new Vector3(0,6,4),new Vector3(0,90,0)),
                ("blue",new Vector3(0,10,0),new Vector3(0,0,-90))})
            {
                var laser=world.AddPart(new(){Id=name,Kind="laser",
                    Position=[position.X,position.Y,position.Z],Rotation=[rotation.X,rotation.Y,rotation.Z]});
                sources.Add(laser);
                var filter=world.AddPart(new(){Id=name+"filter",Kind=name+"_filter",
                    Rotation=[rotation.X,rotation.Y,rotation.Z]});
                filter.Position=new Vector3(0,6,0)+(position-new Vector3(0,6,0))*.45f;
            }
            if(rotate)
            {
                var rotation=new Transform3D(Basis.FromEuler(new(.25f,.45f,.15f)),new(0,2,0));
                foreach(var p in world.Parts)p.Transform=rotation*p.Transform;
            }
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-7,2,-4]});
            foreach(var laser in sources)Assert.True(world.Connect(battery,laser));
            world.Start();
            foreach(var laser in sources)world.Activate(laser);
            if(reverse)world.Parts.Reverse();
            for(var i=0;i<3;i++)world.Step();
            var expected=LaserPart.BeamPower*BeamCombinerPart.Retention;
            Assert.True(receiver.ReceivedPower.DistanceTo(expected)<.00001f);
            Assert.True(receiver.Active);
            Assert.True(combiner.OutputPower.DistanceTo(expected)<.00001f);
            Assert.False(receiver.HasElectricalPower(SocketIds.Supply));
            Assert.True(combiner.InputPower.DistanceTo(LaserPart.BeamPower)<.00001f);
            var outgoing=world.OpticalPaths.Where(s=>s.OriginPart==combiner.Uid).ToArray();
            Assert.Single(outgoing);
            Assert.True(outgoing[0].Power.DistanceTo(expected)<.00001f);
            world.Restore();
            Assert.Empty(world.OpticalPaths);
            Assert.Equal(Vector3.Zero,((BeamCombinerPart)world.FindPart("combiner")!).OutputPower);
            Assert.Equal(Vector3.Zero,((BeamCombinerPart)world.FindPart("combiner")!).InputPower);
            Assert.False(world.FindPart("receiver")!.Active);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void EachInputKeepsItsRemainingRangeAndNeverCreatesMissingChannels(int input)
    {
        var world=World();
        try
        {
            var combiner=world.AddPart(new(){Id="combiner",Kind="beam_combiner",Position=[0,6,0]});
            var surface=combiner.OpticalSurfaces[input];
            var start=combiner.Transform*(surface.Aperture.At+surface.Aperture.Normal*2);
            var emitter=world.AddPart(new(){Id="emitter",Kind="laser"});
            emitter.Position=start;
            var source=new OpticalEmitter(Vector3.Zero,-surface.Aperture.Normal,5,Vector3.Right);
            var trace=OpticalNetwork.Trace(world,emitter,source);
            Assert.Equal(2,trace.Segments.Count);
            Assert.Equal(surface.Id,Assert.Single(trace.Receptions).Port);
            Assert.Equal(Vector3.Right*BeamCombinerPart.Retention,trace.Segments[1].Power);
            var internalLength=trace.Segments[0].To.DistanceTo(combiner.Transform*BeamCombinerPart.Exit);
            var total=trace.Segments.Sum(s=>s.From.DistanceTo(s.To))+internalLength;
            Assert.InRange(total,4.999f,5.001f);
            var shortTrace=OpticalNetwork.Trace(world,emitter,source with{Range=2.1f});
            Assert.Single(shortTrace.Segments);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(OpticalColour.Yellow)]
    [InlineData(OpticalColour.Cyan)]
    [InlineData(OpticalColour.Magenta)]
    [InlineData(OpticalColour.White)]
    public void MixedReceiversRequireEveryMarkedChannelAndRejectContamination(OpticalColour colour)
    {
        var mask=OpticalColours.Mask(colour);
        Assert.True(OpticalColours.Accepts(mask*.3f,colour,.25f));
        for(var i=0;i<3;i++)
        {
            if(mask[i]==0)continue;
            var missing=mask;missing[i]=0;
            Assert.False(OpticalColours.Accepts(missing,colour,.25f));
            missing[i]=.249f;
            Assert.False(OpticalColours.Accepts(missing,colour,.25f));
        }
        if(colour!=OpticalColour.White)
            Assert.False(OpticalColours.Accepts(Vector3.One,colour,.25f));
    }
    [Fact]
    public void RoutedMirrorLoopKeepsOneInteractionBudgetAndLosesPower()
    {
        var world=World();
        try
        {
            var combiner=world.AddPart(new(){Id="combiner",Kind="beam_combiner",Position=[0,6,0]});
            foreach(var (id,point,angle) in new[]{
                ("one",new Vector3(3,6,0),-45f),("two",new Vector3(3,9,0),45f),
                ("three",new Vector3(0,9,0),135f)})
            {
                var mirror=world.AddPart(new(){Id=id,Kind="mirror",Rotation=[0,0,angle]});
                mirror.Position=point-mirror.Basis*mirror.OpticalSurfaces.Single().Aperture.At;
            }
            var laser=world.AddPart(new(){Id="laser",Kind="laser",Position=[-3,6,0]});
            var trace=OpticalNetwork.Trace(world,laser,laser.OpticalPreviewSource!.Value with{Range=100});
            Assert.Equal(OpticalNetwork.MaximumInteractions+1,trace.Segments.Count);
            var receptions=trace.Receptions.Where(r=>r.Receiver==combiner).ToArray();
            Assert.True(receptions.Length>=4);
            for(var i=1;i<receptions.Length;i++)Assert.True(receptions[i].Power.X<receptions[i-1].Power.X);
        }
        finally{world.Free();}
    }
    [Fact]
    public void ArtworkSumsSharedPathOnlyUntilTheShorterContributionExpires()
    {
        var merged=OpticalPathVisual.Merge([
            new(Vector3.Zero,Vector3.Right*2,Vector3.Right,"combiner"),
            new(Vector3.Zero,Vector3.Right*4,Vector3.Up,"combiner")]);
        Assert.Equal(2,merged.Count);
        Assert.Equal(new Vector3(1,1,0),merged[0].Power);
        Assert.Equal(Vector3.Up,merged[1].Power);
        Assert.Equal(Vector3.Right*2,merged[1].From);
        Assert.Equal(OpticalColours.Ink(OpticalColour.Yellow),OpticalColours.BeamInk(merged[0].Power));
        var crossing=OpticalPathVisual.Merge([
            new(Vector3.Zero,Vector3.Right,Vector3.Right,"a"),
            new(Vector3.Zero,Vector3.Up,Vector3.Up,"b")]);
        Assert.Equal(2,crossing.Count);
        Assert.All(crossing,s=>Assert.Equal(1,s.Power.X+s.Power.Y+s.Power.Z));
    }
    [Fact]
    public void BackAndHousingBlockRatherThanRouteAndOutputWallStillOccludes()
    {
        var world=World();
        try
        {
            world.AddPart(new(){Id="combiner",Kind="beam_combiner",Position=[0,6,0]});
            var laser=world.AddPart(new(){Id="laser",Kind="laser",Position=[-3,6,0]});
            var source=laser.OpticalPreviewSource!.Value;
            var offset=OpticalNetwork.Trace(world,laser,source with{At=source.At+Vector3.Up*.6f});
            Assert.Empty(offset.Receptions);Assert.Single(offset.Segments);
            laser.Position=new(3,6,0);
            var back=OpticalNetwork.Trace(world,laser,source with{At=Vector3.Zero,Direction=Vector3.Left});
            Assert.Empty(back.Receptions);Assert.Single(back.Segments);
            laser.Position=new(-3,6,0);
            world.AddPart(new(){Id="wall",Kind="wall",Position=[2,6,0],Rotation=[0,90,0]});
            var blocked=OpticalNetwork.Trace(world,laser,source);
            Assert.Equal(2,blocked.Segments.Count);
            Assert.True(blocked.Segments[1].To.X<2);
        }
        finally{world.Free();}
    }
}
