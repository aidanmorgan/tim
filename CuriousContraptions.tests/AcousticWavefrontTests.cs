using Godot;
using System.Text.Json;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class AcousticWavefrontTests(NativeSceneFixture godot)
{
    public enum Emitter { Bell, Speaker, Chimes }
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Bell=new("bell"),Speaker=new("speaker"),Chimes=new("wind_chimes"),Battery=new("battery");
    private partial class Probe:BatteryPart
    {
        public Action? Before;
        public bool Fail;
        public override void BeforeNetworks(MachineWorld world)=>Before?.Invoke();
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(Fail)throw new InvalidOperationException("Injected wavefront transaction failure.");}
    }
    private static void Attach(MachineWorld world,MachinePart part,CatalogueId id,FixturePartId fixture,Vector3 point)
    {
        part.Definition=world.Registry.Definitions[id.Value];
        part.Configure(new(){Id=FixtureParts.Id(fixture),Kind=id.Value,Position=[point.X,point.Y,point.Z]});world.AttachPart(part);
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    [Theory]
    [InlineData(Emitter.Bell,true)]
    [InlineData(Emitter.Bell,false)]
    [InlineData(Emitter.Speaker,true)]
    [InlineData(Emitter.Speaker,false)]
    [InlineData(Emitter.Chimes,true)]
    [InlineData(Emitter.Chimes,false)]
    public void EachEmitterPublishesOnlyCommittedWavesAndRestoresConstruction(Emitter kind,bool enabled)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            MachinePart part=kind switch {Emitter.Bell=>new BellPart(),Emitter.Speaker=>new SpeakerPart(),
                Emitter.Chimes=>new WindChimesPart(),_=>throw new ArgumentOutOfRangeException(nameof(kind))};
            var catalogue=kind switch {Emitter.Bell=>Bell,Emitter.Speaker=>Speaker,Emitter.Chimes=>Chimes,_=>throw new ArgumentOutOfRangeException()};
            Attach(world,part,catalogue,FixturePartId.First,new(0,6,0));
            var probe=new Probe();Attach(world,probe,Battery,FixturePartId.Second,new(-5,6,0));
            if(kind==Emitter.Speaker&&enabled)Assert.True(world.Connect(probe,SocketId.Supply,part,SocketId.PowerIn,ConnectionDomain.Electrical));
            var declaration=part.AcousticWavefronts!;var rings=declaration.Rings;
            var transforms=rings.Select(r=>r.Transform).ToArray();var radii=rings.Select(r=>((TorusMesh)r.Mesh).OuterRadius).ToArray();
            var saved=Saved(world);world.Start();
            if(kind==Emitter.Speaker){world.Activate(part);world.Step();}
            else probe.Before=()=>part.ObserveContact(new(new(part,kind==Emitter.Chimes?WindChimesPart.TubeBody(ChimeTubeId.Right):MachinePart.RootBody),
                new(probe,MachinePart.RootBody),new(0,6,0),default,enabled?2:.01,1),world);
            probe.Fail=true;Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(0UL,world.ReadAcousticWavefronts(part).Accepted);Assert.All(rings,r=>Assert.False(r.Visible));
            probe.Fail=false;world.Step();probe.Before=null;
            Assert.Equal(enabled?1UL:0UL,world.ReadAcousticWavefronts(part).Accepted);
            Assert.All(rings,r=>Assert.False(r.Visible));
            var pulse=enabled?Assert.Single(part.AcousticPulses):null;
            for(var i=0;i<12;i++)world.Step();
            world.Running=false;var physical=world.Physics.Capture().BodyStates.ToArray();var tick=world.Ticks;
            world.PresentFrame(.03,1);
            var visible=rings.Where(r=>r.Visible).ToArray();Assert.Equal(enabled?(kind==Emitter.Speaker?1:3):0,visible.Length);
            if(pulse is not null)
            {
                var distance=(world.Ticks-pulse.EmissionTick)*(double)MachineWorld.Tick*AcousticPulse.Speed;
                var center=pulse.Origin+(kind==Emitter.Speaker?pulse.Direction*(float)distance:Vector3.Zero);
                foreach(var ring in visible)
                {
                    Assert.InRange(ring.GlobalPosition.DistanceTo(center),0,2e-5);
                    Assert.InRange(Math.Abs(((TorusMesh)ring.Mesh).OuterRadius-(declaration.InitialRadius+distance*declaration.RadiusPerDistance+declaration.Thickness)),0,2e-5);
                    Assert.True(((StandardMaterial3D)ring.MaterialOverride).AlbedoColor.A>0);
                }
            }
            var pose=rings.Select(r=>r.Transform).ToArray();var sizes=rings.Select(r=>((TorusMesh)r.Mesh).OuterRadius).ToArray();
            part.Active=!enabled;((List<AcousticPulse>)part.AcousticPulses).Clear();
            world.PresentFrame(.4,1);Assert.Equal(pose,rings.Select(r=>r.Transform).ToArray());Assert.Equal(sizes,rings.Select(r=>((TorusMesh)r.Mesh).OuterRadius).ToArray());
            part.Visible=false;world.PresentFrame(.4,1);Assert.All(rings,r=>Assert.False(r.Visible));
            part.Visible=true;world.PresentFrame(0,1);Assert.Equal(visible.Length,rings.Count(r=>r.Visible));
            Assert.Equal(tick,world.Ticks);Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            world.Restore();Assert.Equal(saved,Saved(world));
            var restored=world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(transforms,restored.AcousticWavefronts!.Rings.Select(r=>r.Transform).ToArray());
            Assert.Equal(radii,restored.AcousticWavefronts.Rings.Select(r=>((TorusMesh)r.Mesh).OuterRadius).ToArray());
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.Start();world.PresentFrame(.1,1);
            restored=world.FindPart(FixtureParts.Id(FixturePartId.First))!;Assert.Equal(0UL,world.ReadAcousticWavefronts(restored).Accepted);
            Assert.All(restored.AcousticWavefronts!.Rings,r=>Assert.False(r.Visible));
        }
        finally{world.Free();}
    }
}
