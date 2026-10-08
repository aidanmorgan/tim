using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

internal enum PredictionStage { Midpoint, Endpoint }

/// <summary>Bounded scratch owned by one pure prediction. Each body retains only
/// its latest exact wrench/horizon path and two sampled states. Nothing survives
/// the prediction call, so world topology, Reset and load revisions cannot leak
/// into another prediction. Returned paths remain immutable and retainable;
/// sampled bodies are borrowed read-only for the current capture.</summary>
internal sealed class PredictionSamples
{
    private sealed class Entry(PhysicsBody body)
    {
        internal readonly PhysicsBody Source=body;
        private readonly PhysicsBodySnapshot _source=body.Snapshot();
        private readonly ulong _revision=body.PoseRevision;
        internal BodyWrench Wrench;
        internal BodyTrajectory? Path;
        internal PhysicsBody? Midpoint,Endpoint;
        internal PhysicsBodySnapshot MidpointState,EndpointState;
        internal void Validate()
        {
            if(Source.PoseRevision!=_revision||Source.Snapshot()!=_source)
                throw new InvalidOperationException("Prediction source changed during evaluation.");
            if(Midpoint is not null&&Midpoint.Snapshot()!=MidpointState||
                Endpoint is not null&&Endpoint.Snapshot()!=EndpointState)
                throw new InvalidOperationException("Borrowed prediction sample was modified.");
        }
    }
    private readonly Entry[] _entries;
    internal PredictionSamples(IReadOnlyList<PhysicsBody> bodies)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        _entries=new Entry[bodies.Count];
        var identities=new HashSet<PhysicsBodyId>();
        for(var i=0;i<bodies.Count;i++)
        {
            ArgumentNullException.ThrowIfNull(bodies[i]);
            if(!identities.Add(bodies[i].Id))throw new ArgumentException("Duplicate prediction body identity.");
            _entries[i]=new(bodies[i]);
        }
    }
    internal Dictionary<PhysicsBodyId,BodyTrajectory> Capture(
        IReadOnlyDictionary<PhysicsBodyId,BodyWrench> forces,double horizon)
    {
        ArgumentNullException.ThrowIfNull(forces);
        if(!double.IsFinite(horizon)||horizon<=0)throw new ArgumentOutOfRangeException(nameof(horizon));
        if(forces.Count!=_entries.Length)throw new ArgumentException("Prediction requires one wrench per owned body.");
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>(_entries.Length);
        foreach(var entry in _entries)
        {
            entry.Validate();
            if(!forces.TryGetValue(entry.Source.Id,out var force))
                throw new ArgumentException("Prediction wrench identity is not owned.");
            if(entry.Path is null||entry.Path.Duration!=horizon||entry.Wrench!=force)
            {
                entry.Path=entry.Source.CreateTrajectory(horizon,force);
                entry.Wrench=force;
                entry.Midpoint=null;entry.Endpoint=null;
            }
            paths.Add(entry.Source.Id,entry.Path);
        }
        return paths;
    }
    internal Dictionary<PhysicsBodyId,PhysicsBody> Sample(
        IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,PredictionStage stage)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if(!Enum.IsDefined(stage))throw new ArgumentOutOfRangeException(nameof(stage));
        if(paths.Count!=_entries.Length)throw new ArgumentException("Sample requires the current complete path set.");
        var samples=new Dictionary<PhysicsBodyId,PhysicsBody>(_entries.Length);
        foreach(var entry in _entries)
        {
            entry.Validate();
            if(entry.Path is null||!paths.TryGetValue(entry.Source.Id,out var path)||!ReferenceEquals(path,entry.Path))
                throw new ArgumentException("Sample requires the current owned path.");
            PhysicsBody sample;
            switch(stage)
            {
                case PredictionStage.Midpoint:
                    if(entry.Midpoint is null)
                    {
                        entry.Midpoint=path.SampleBody(path.Duration*.5);
                        entry.MidpointState=entry.Midpoint.Snapshot();
                    }
                    sample=entry.Midpoint;break;
                case PredictionStage.Endpoint:
                    if(entry.Endpoint is null)
                    {
                        entry.Endpoint=path.SampleBody(path.Duration);
                        entry.EndpointState=entry.Endpoint.Snapshot();
                    }
                    sample=entry.Endpoint;break;
                default:throw new ArgumentOutOfRangeException(nameof(stage));
            }
            samples.Add(entry.Source.Id,sample);
        }
        return samples;
    }
}
