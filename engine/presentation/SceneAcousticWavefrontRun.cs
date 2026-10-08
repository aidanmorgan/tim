using System;
using System.Collections.Generic;
using Godot;
using CuriousContraptions.Bridge;

namespace CuriousContraptions.Presentation;

public enum SceneWavefrontOpacity { Uniform, Strength }
public sealed record SceneAcousticWavefronts(IReadOnlyList<MeshInstance3D> Rings,AcousticPattern Pattern,
    float MinimumDistance,float InitialRadius,float RadiusPerDistance,float Thickness,float Opacity,SceneWavefrontOpacity Response,int Capacity);
public readonly record struct AcousticWavefront(Vector3 Origin,Vector3 Direction,ToneBand Tone,AcousticPattern Pattern,float Strength,int EmissionTick);
public readonly record struct SceneWavefrontRead(int Retained,int Capacity,int HighWaterMark,ulong Accepted,ulong Presented);

/// <summary>Bounded committed acoustic occurrence consumer. Unseen occurrences retain one visible
/// frame; saturation rejects staging before physical commit. Simulation time owns propagation.</summary>
public sealed class SceneAcousticWavefrontRun
{
    public const int MaximumCapacity=65536;
    public const double MaximumRadius=1e6;
    private struct Entry
    {
        public CommittedEvent<AcousticWavefront> Occurrence;
        public readonly AcousticWavefront Pulse=>Occurrence.Payload;
        public bool Presented;
    }
    private sealed class Binding
    {
        public required MachinePart Owner;
        public required SceneAcousticWavefronts Declaration;
        public required MeshInstance3D[] Rings;
        public required Node3D[] Parents;
        public required TorusMesh[] Meshes;
        public required StandardMaterial3D[] Materials;
        public required Transform3D[] Baselines;
        public required Color[] Colours;
        public required Vector2[] Radii;
        public required Entry[] Current,Pending;
        public int Count,PendingCount,HighWaterMark;
        public ulong Accepted,Presented;
        public int Added;
    }
    private readonly Dictionary<MachinePart,Binding> _bindings=[];
    private PoseReadStamp _current,_pending;
    private bool _staged,_removed;
    private ulong _lastSequence,_pendingSequence;
    public SceneAcousticWavefrontRun(SceneAnimationAdapter adapter,IReadOnlyList<MachinePart> parts,
        IReadOnlyList<Node3D> physicalTargets,PoseReadStamp seed)
    {
        _current=seed;
        if(seed.Generation.Value==0||seed.Revision.Value!=0||seed.SimulationTime!=0)throw new ArgumentException("Expected run seed.");
        var meshes=new HashSet<TorusMesh>();
        foreach(var owner in parts)
        {
            if(owner.AcousticWavefronts is not { } d)continue;
            if(owner.AcousticPlayback is null||!Enum.IsDefined(d.Pattern)||!Enum.IsDefined(d.Response)||
                !float.IsFinite(d.MinimumDistance)||d.MinimumDistance<0||d.MinimumDistance>=AcousticPulse.Range||
                !float.IsFinite(d.InitialRadius)||d.InitialRadius<0||!float.IsFinite(d.RadiusPerDistance)||d.RadiusPerDistance<=0||
                (double)d.InitialRadius+AcousticPulse.Range*(double)d.RadiusPerDistance>MaximumRadius||
                !float.IsFinite(d.Thickness)||d.Thickness<=0||d.InitialRadius+d.MinimumDistance*d.RadiusPerDistance<=d.Thickness||
                !float.IsFinite(d.Opacity)||d.Opacity<=0||d.Opacity>1)
                throw new ArgumentException("Invalid wavefront declaration.");
            var planes=d.Pattern==AcousticPattern.Cone?1:3;
            if(d.Capacity<d.Rings.Count/planes||d.Capacity>MaximumCapacity||d.Rings.Count==0||d.Rings.Count%planes!=0)throw new ArgumentException("Wavefront ring pool is incomplete.");
            var n=d.Rings.Count;var capacity=d.Capacity;
            var b=new Binding {Owner=owner,Declaration=d,Rings=new MeshInstance3D[n],Parents=new Node3D[n],
                Meshes=new TorusMesh[n],Materials=new StandardMaterial3D[n],Baselines=new Transform3D[n],
                Colours=new Color[n],Radii=new Vector2[n],Current=new Entry[capacity],Pending=new Entry[capacity]};
            for(var i=0;i<n;i++)
            {
                var ring=d.Rings[i];
                if(!GodotObject.IsInstanceValid(ring)||!owner.IsAncestorOf(ring)||ring.GetParent() is not Node3D parent||
                    ring.Mesh is not TorusMesh mesh||ring.MaterialOverride is not StandardMaterial3D material)
                    throw new ArgumentException("Wavefront requires owned torus and material bindings.");
                foreach(var physical in physicalTargets)
                    if(ring==physical||ring.IsAncestorOf(physical))throw new InvalidOperationException("Wavefront cannot own physical geometry.");
                if(!meshes.Add(mesh))throw new ArgumentException("Wavefront geometry already has a writer.");
                adapter.Register(ring);adapter.Register(material);
                b.Rings[i]=ring;b.Parents[i]=parent;b.Meshes[i]=mesh;b.Materials[i]=material;b.Baselines[i]=ring.Transform;
                b.Colours[i]=material.AlbedoColor;b.Radii[i]=new(mesh.InnerRadius,mesh.OuterRadius);
            }
            _bindings.Add(owner,b);
        }
    }
    private void RequireLive(){if(_removed)throw new InvalidOperationException("Wavefront run removed.");}
    public void Begin(PoseReadStamp stamp)
    {
        RequireLive();
        if(_staged||stamp.Generation!=_current.Generation||stamp.Revision.Value!=checked(_current.Revision.Value+1)||
            !double.IsFinite(stamp.SimulationTime)||stamp.SimulationTime<=_current.SimulationTime)
            throw new ArgumentException("Expected adjacent wavefront stamp.");
        _pending=stamp;_staged=true;_pendingSequence=_lastSequence;
        foreach(var b in _bindings.Values)
        {
            b.PendingCount=0;b.Added=0;
            for(var i=0;i<b.Count;i++)
            {
                var e=b.Current[i];
                if(e.Presented&&Distance(e.Pulse,stamp)>=AcousticPulse.Range)continue;
                b.Pending[b.PendingCount++]=e;
            }
        }
    }
    public void Append(MachinePart owner,CommittedEventId id,AcousticPulse pulse)
    {
        RequireLive();if(!_staged)throw new InvalidOperationException("Wavefront staging not begun.");
        if(id.Stream!=SceneAcousticRun.StreamId||id.Generation!=_pending.Generation||id.Sequence.Value<=_pendingSequence)
            throw new ArgumentException("Wavefront occurrence identity is stale or belongs to another stream.");
        if(!_bindings.TryGetValue(owner,out var b)){_pendingSequence=id.Sequence.Value;return;} // Optional visual declaration absent.
        if(pulse.Pattern!=b.Declaration.Pattern||pulse.EmissionTick!=_pending.Revision.Value-1)
            throw new ArgumentException("Wavefront pulse differs from declared pattern or tick.");
        if(b.PendingCount==b.Pending.Length)throw new InvalidOperationException("Wavefront presentation capacity exhausted; present or Reset before retry.");
        b.Pending[b.PendingCount++]=new(){Occurrence=new(_pending,id,new(pulse.Origin,pulse.Direction,pulse.Tone,pulse.Pattern,pulse.Strength,pulse.EmissionTick))};
        b.Added++;_pendingSequence=id.Sequence.Value;
    }
    public void Commit(PoseReadStamp stamp)
    {
        RequireLive();if(!_staged||stamp!=_pending)throw new ArgumentException("No matching staged wavefront snapshot.");
        foreach(var b in _bindings.Values)
        {
            (b.Current,b.Pending)=(b.Pending,b.Current);b.Count=b.PendingCount;
            b.Accepted+=checked((ulong)b.Added);b.HighWaterMark=Math.Max(b.HighWaterMark,b.Count);
        }
        _current=stamp;_staged=false;_lastSequence=_pendingSequence;
    }
    public void Discard()
    {
        RequireLive();_staged=false;
        foreach(var b in _bindings.Values){b.PendingCount=0;b.Added=0;}
    }
    private static double Distance(AcousticWavefront pulse,PoseReadStamp stamp)=>
        (stamp.SimulationTime-pulse.EmissionTick*(double)MachineWorld.Tick)*AcousticPulse.Speed;
    private static void Validate(Binding b,int i)
    {
        if(!GodotObject.IsInstanceValid(b.Rings[i])||b.Rings[i].GetParent()!=b.Parents[i]||
            b.Rings[i].Mesh!=b.Meshes[i]||b.Rings[i].MaterialOverride!=b.Materials[i])
            throw new InvalidOperationException("Wavefront binding changed.");
    }
    public void Present()
    {
        RequireLive();if(_staged)throw new InvalidOperationException("Cannot present uncommitted wavefronts.");
        foreach(var b in _bindings.Values)
        {
            // Validate all targets before any writes in this binding.
            for(var i=0;i<b.Rings.Length;i++)Validate(b,i);
            var d=b.Declaration;var planes=d.Pattern==AcousticPattern.Cone?1:3;
            var retained=0;
            for(var index=0;index<b.Count;index++)
                if(!b.Current[index].Presented||Distance(b.Current[index].Pulse,_current)<AcousticPulse.Range)
                    b.Current[retained++]=b.Current[index];
            b.Count=retained;
            for(var slot=0;slot<b.Rings.Length/planes;slot++)
            {
                var active=slot<b.Count;var e=active?b.Current[slot]:default;
                var distance=active?Distance(e.Pulse,_current):0;
                // Retain brief feedback once, even if no frame occurred in its physical lifetime.
                if(active&&!e.Presented&&distance>=AcousticPulse.Range)distance=d.MinimumDistance;
                var visible=active&&b.Owner.IsVisibleInTree()&&distance>=d.MinimumDistance&&distance<AcousticPulse.Range;
                for(var plane=0;plane<planes;plane++)
                {
                    var i=slot*planes+plane;var ring=b.Rings[i];
                    if(ring.Visible!=visible)ring.Visible=visible;
                    if(!visible)continue;
                    var center=e.Pulse.Origin;
                    if(d.Pattern==AcousticPattern.Cone)center+=e.Pulse.Direction*(float)distance;
                    var local=b.Parents[i].ToLocal(center);if(ring.Position!=local)ring.Position=local;
                    if(d.Pattern==AcousticPattern.Cone)
                    {
                        var basis=b.Parents[i].GlobalBasis.Inverse()*new Basis(new Quaternion(Vector3.Up,e.Pulse.Direction));
                        if(ring.Basis!=basis)ring.Basis=basis;
                    }
                    var radius=d.InitialRadius+(float)distance*d.RadiusPerDistance;
                    var inner=radius-d.Thickness;var outer=radius+d.Thickness;
                    if(b.Meshes[i].InnerRadius!=inner)b.Meshes[i].InnerRadius=inner;
                    if(b.Meshes[i].OuterRadius!=outer)b.Meshes[i].OuterRadius=outer;
                    var alpha=d.Opacity*(d.Response==SceneWavefrontOpacity.Strength?e.Pulse.Strength:1)*(1-(float)distance/AcousticPulse.Range);
                    var colour=new Color(1,.94f,.65f,alpha);
                    if(b.Materials[i].AlbedoColor!=colour)b.Materials[i].AlbedoColor=colour;
                }
                if(visible&&!e.Presented){e.Presented=true;b.Current[slot]=e;b.Presented++;}
            }
        }
    }
    public CommittedEvent<AcousticWavefront> ReadOccurrence(MachinePart owner,int index)
    {
        RequireLive();var b=_bindings[owner];
        if((uint)index>=(uint)b.Count)throw new ArgumentOutOfRangeException(nameof(index));
        return b.Current[index].Occurrence;
    }
    public SceneWavefrontRead Read(MachinePart owner)
    {
        RequireLive();var b=_bindings[owner];return new(b.Count,b.Current.Length,b.HighWaterMark,b.Accepted,b.Presented);
    }
    public void Remove()
    {
        RequireLive();
        foreach(var b in _bindings.Values)
        for(var i=0;i<b.Rings.Length;i++)
        {
            Validate(b,i);b.Rings[i].Visible=false;b.Rings[i].Transform=b.Baselines[i];
            b.Meshes[i].InnerRadius=b.Radii[i].X;b.Meshes[i].OuterRadius=b.Radii[i].Y;b.Materials[i].AlbedoColor=b.Colours[i];
        }
        _removed=true;
    }
}
