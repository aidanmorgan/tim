using Godot;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class AcousticWavefrontBindingTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Battery=new("battery");
    private static CommittedEventId Id(int sequence)=>new(SceneAcousticRun.StreamId,new(1),new((ulong)sequence));
    private static PoseReadStamp Seed=>new(new(1),new(0),0);
    private static PoseReadStamp Stamp(int tick)=>new(new(1),new(tick),tick*(double)MachineWorld.Tick);
    private partial class Source:BatteryPart
    {
        public List<AcousticPulse> Pulses=[];
        public override IReadOnlyList<AcousticPulse> AcousticPulses=>Pulses;
        public SceneAcousticBinding? Playback;
        public SceneAcousticWavefronts? Waves;
        public override SceneAcousticBinding? AcousticPlayback=>Playback;
        public override SceneAcousticWavefronts? AcousticWavefronts=>Waves;
    }
    private Source Attach(MachineWorld world,AcousticPattern pattern)
    {
        var source=new Source{Definition=world.Registry.Definitions[Battery.Value]};
        source.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value,Position=[0,6,0]});world.AttachPart(source);
        var player=new AudioStreamPlayer3D();source.AddChild(player);
        source.Playback=new(player,new Dictionary<ToneBand,AudioStreamWav>{{ToneBand.Mid,AcousticAudio.Create(ToneBand.Mid,AcousticVoice.Bell)}});
        var rings=new List<MeshInstance3D>();
        for(var i=0;i<(pattern==AcousticPattern.Cone?1:3);i++){var ring=PartArt.Ring(source,1,.01f,new Color(1,1,1));ring.Visible=false;rings.Add(ring);}
        source.Waves=new(rings,pattern,.1f,.5f,1,.01f,.5f,SceneWavefrontOpacity.Strength,2);
        return source;
    }
    [Theory]
    [InlineData(AcousticPattern.Cone,ToneBand.Low)]
    [InlineData(AcousticPattern.Cone,ToneBand.Mid)]
    [InlineData(AcousticPattern.Cone,ToneBand.High)]
    [InlineData(AcousticPattern.Omnidirectional,ToneBand.Low)]
    [InlineData(AcousticPattern.Omnidirectional,ToneBand.Mid)]
    [InlineData(AcousticPattern.Omnidirectional,ToneBand.High)]
    public void HiddenAndSkippedFramesRetainEveryOccurrenceAndCapacityRejectsBeforeCommit(AcousticPattern pattern,ToneBand tone)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var source=Attach(world,pattern);var rings=source.Waves!.Rings;var adapter=new SceneAnimationAdapter(2*rings.Count);
            var waves=new SceneAcousticWavefrontRun(adapter,[source],[],Seed);
            var origin=new Vector3(2,4,3);var direction=Vector3.Forward;
            for(var tick=1;tick<=100;tick++)
            {
                waves.Begin(Stamp(tick));
                if(tick<=2)waves.Append(source,Id(tick),new(origin,direction,tone,tick-1,pattern,.5f));
                waves.Commit(Stamp(tick));
            }
            Assert.Equal(2UL,waves.Read(source).Accepted);Assert.Equal(0UL,waves.Read(source).Presented);
            var occurrence=waves.ReadOccurrence(source,0);Assert.Equal(Id(1),occurrence.Id);Assert.Equal(Stamp(1),occurrence.Stamp);
            Assert.Equal(origin,occurrence.Payload.Origin);Assert.Equal(direction,occurrence.Payload.Direction);
            Assert.Equal(tone,occurrence.Payload.Tone);Assert.Equal(pattern,occurrence.Payload.Pattern);Assert.Equal(.5f,occurrence.Payload.Strength);
            waves.Begin(Stamp(101));
            Assert.Throws<InvalidOperationException>(()=>waves.Append(source,Id(3),new(origin,direction,tone,100,pattern,.5f)));
            Assert.Throws<InvalidOperationException>(waves.Present);waves.Discard();
            Assert.Equal(2UL,waves.Read(source).Accepted);Assert.Equal(2,waves.Read(source).Retained);
            source.Visible=false;waves.Present();Assert.All(rings,r=>Assert.False(r.Visible));Assert.Equal(0UL,waves.Read(source).Presented);
            source.Visible=true;waves.Present();Assert.Equal(1UL,waves.Read(source).Presented);
            foreach(var ring in rings)
            {
                Assert.True(ring.Visible);Assert.True(((StandardMaterial3D)ring.MaterialOverride).AlbedoColor.A>0);
                var center=origin+(pattern==AcousticPattern.Cone?direction*.1f:Vector3.Zero);
                Assert.InRange(ring.GlobalPosition.DistanceTo(center),0,1e-5);
                if(pattern==AcousticPattern.Cone)Assert.InRange((ring.GlobalBasis*Vector3.Up).DistanceTo(direction),0,1e-5);
            }
            waves.Present();Assert.Equal(2UL,waves.Read(source).Presented);
            waves.Present();Assert.All(rings,r=>Assert.False(r.Visible));Assert.Equal(0,waves.Read(source).Retained);
            waves.Begin(Stamp(101));waves.Append(source,Id(3),new(origin,direction,tone,100,pattern,.5f));waves.Commit(Stamp(101));
            Assert.Equal(3UL,waves.Read(source).Accepted);Assert.Equal(2,waves.Read(source).HighWaterMark);
            waves.Remove();adapter.Reset();Assert.All(rings,r=>Assert.False(r.Visible));
            Assert.Throws<InvalidOperationException>(waves.Present);
        }
        finally{world.Free();}
    }
    public enum Fault { Pattern, OpacityMode, Capacity, PhysicalTarget, DuplicateTarget, ForeignTarget, Radius, SharedMesh, RadiusOverflow, CapacityOverflow }
    [Theory]
    [InlineData(Fault.Pattern)]
    [InlineData(Fault.OpacityMode)]
    [InlineData(Fault.Capacity)]
    [InlineData(Fault.PhysicalTarget)]
    [InlineData(Fault.DuplicateTarget)]
    [InlineData(Fault.ForeignTarget)]
    [InlineData(Fault.Radius)]
    [InlineData(Fault.SharedMesh)]
    [InlineData(Fault.RadiusOverflow)]
    [InlineData(Fault.CapacityOverflow)]
    public void InvalidDeclarationsReject(Fault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var source=Attach(world,AcousticPattern.Cone);var d=source.Waves!;
            source.Waves=fault switch
            {
                Fault.Pattern=>d with{Pattern=(AcousticPattern)99},
                Fault.OpacityMode=>d with{Response=(SceneWavefrontOpacity)99},
                Fault.Capacity=>d with{Capacity=0},
                Fault.DuplicateTarget=>d with{Rings=[d.Rings[0],d.Rings[0]]},
                Fault.ForeignTarget=>d,
                Fault.Radius=>d with{InitialRadius=float.NaN},
                Fault.PhysicalTarget=>d,
                Fault.SharedMesh=>d,
                Fault.RadiusOverflow=>d with{RadiusPerDistance=float.MaxValue},
                Fault.CapacityOverflow=>d with{Capacity=SceneAcousticWavefrontRun.MaximumCapacity+1},
                _=>throw new ArgumentOutOfRangeException(nameof(fault))
            };
            if(fault==Fault.SharedMesh)
            {
                var other=PartArt.Ring(source,1,.01f,new Color(1,1,1));other.Mesh=d.Rings[0].Mesh;
                source.Waves=d with{Rings=[d.Rings[0],other]};
            }
            if(fault==Fault.ForeignTarget){source.RemoveChild(d.Rings[0]);world.AddChild(d.Rings[0]);}
            var action=()=>new SceneAcousticWavefrontRun(new(8),[source],fault==Fault.PhysicalTarget?[d.Rings[0]]:[],Seed);
            if(fault==Fault.PhysicalTarget)Assert.Throws<InvalidOperationException>(()=>action());
            else Assert.Throws<ArgumentException>(()=>action());
        }
        finally{world.Free();}
    }
    public enum InvalidOccurrence { Stream, Generation, Sequence }
    [Theory]
    [InlineData(InvalidOccurrence.Stream)]
    [InlineData(InvalidOccurrence.Generation)]
    [InlineData(InvalidOccurrence.Sequence)]
    public void OccurrenceIdentityRejectsBeforeRetention(InvalidOccurrence fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var source=Attach(world,AcousticPattern.Cone);var waves=new SceneAcousticWavefrontRun(new(2),[source],[],Seed);
            var pulse=new AcousticPulse(Vector3.Zero,Vector3.Right,ToneBand.Mid,0,AcousticPattern.Cone,1);
            var id=new CommittedEventId(fault==InvalidOccurrence.Stream?new(99):SceneAcousticRun.StreamId,
                fault==InvalidOccurrence.Generation?new(2):new(1),fault==InvalidOccurrence.Sequence?default:new(1));
            waves.Begin(Stamp(1));Assert.Throws<ArgumentException>(()=>waves.Append(source,id,pulse));
            waves.Append(source,Id(1),pulse);Assert.Throws<ArgumentException>(()=>waves.Append(source,Id(1),pulse));
            waves.Commit(Stamp(1));Assert.Equal(1UL,waves.Read(source).Accepted);Assert.Equal(Id(1),waves.ReadOccurrence(source,0).Id);
        }
        finally{world.Free();}
    }
    [Fact]
    public void RetentionSaturationRollsBackWorldBeforeAudioOrWavePublication()
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var source=Attach(world,AcousticPattern.Cone);
            for(var i=0;i<3;i++)source.Pulses.Add(new(Vector3.Zero,Vector3.Right,ToneBand.Mid,0,AcousticPattern.Cone,1));
            world.Start();var before=world.Physics.Capture().BodyStates.ToArray();
            Assert.Throws<InvalidOperationException>(world.Step);Assert.Equal(0,world.Ticks);
            Assert.Equal(before,world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(0UL,world.ReadAcousticWavefronts(source).Accepted);Assert.False(source.Playback!.Player.Playing);
            Assert.Throws<InvalidOperationException>(world.Step);Assert.Equal(0,world.Ticks);
            source.Pulses.RemoveAt(2);world.Step();Assert.Equal(1,world.Ticks);
            Assert.Equal(2UL,world.ReadAcousticWavefronts(source).Accepted);Assert.True(source.Playback.Player.Playing);
            world.Restore();world.Start();world.Step();Assert.Equal(1,world.Ticks);
        }
        finally{world.Free();}
    }
    [Fact]
    public void DiscardAndBindingInvalidationPreserveCurrentSnapshot()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var source=Attach(world,AcousticPattern.Cone);var ring=source.Waves!.Rings[0];
            var waves=new SceneAcousticWavefrontRun(new(2),[source],[],Seed);
            waves.Begin(Stamp(1));
            Assert.Throws<ArgumentException>(()=>waves.Append(source,Id(1),new(Vector3.Zero,Vector3.Up,ToneBand.Mid,0,AcousticPattern.Omnidirectional,1)));
            waves.Discard();Assert.Equal(0UL,waves.Read(source).Accepted);
            Assert.Throws<ArgumentException>(()=>waves.Begin(Stamp(2)));
            waves.Begin(Stamp(1));waves.Append(source,Id(1),new(Vector3.Zero,Vector3.Right,ToneBand.Mid,0,AcousticPattern.Cone,1));
            waves.Discard();waves.Present();Assert.False(ring.Visible);Assert.Equal(0UL,waves.Read(source).Accepted);
            source.RemoveChild(ring);world.AddChild(ring);
            Assert.Throws<InvalidOperationException>(waves.Present);
        }
        finally{world.Free();}
    }
}
