using System;
using System.Collections.Generic;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public readonly record struct GpuPassageSensorId(ulong Value);

public readonly record struct PassageSensorDeclaration(
    GpuPassageSensorId Id,
    GpuBodyId Frame,
    CellOrigin Cell,
    LocalPosition Local,
    CanonicalRotation Rotation,
    Metres Radius,
    Metres RearmClearance)
{
    public void Validate()
    {
        if (Id.Value == 0 || Frame.Value == 0) throw new ArgumentException("Invalid passage sensor identity.");
        Rotation.Validate();
        PhysicsDeclarationBounds.Range(Radius.Value, 0.01f, 10f);
        PhysicsDeclarationBounds.Range(RearmClearance.Value, 0.001f, 10f);
    }
}

public enum PassageSensorPhase : uint { Unarmed, Armed, Fired }

public readonly record struct PassageSensorRead(
    GpuPassageSensorId Id,
    GpuBodyId Frame,
    GpuBodyId Body,
    GpuColliderId Collider,
    PassageSensorPhase Phase,
    uint EventOrdinal,
    float EventPhase,
    LinearSpeed Speed);

public readonly record struct PassagePairKey(GpuPassageSensorId Sensor, GpuBodyId Body);
public readonly record struct PassageBodyState(GpuPassageSensorId Sensor, GpuBodyId Body,
    PassageSensorPhase Phase, ulong PassedCount);

/// <summary>Immutable candidate state: one previous pose per dynamic participant and only actual remembered interactions.</summary>
public sealed class PassageSensorTracker
{
    private readonly record struct Participant(GpuBodyId Body, GpuColliderId Collider, float Radius, Vector3 Previous);
    public static readonly PassageSensorTracker Empty = new([], [], [], [], []);
    private readonly PassageSensorDeclaration[] _sensors;
    private readonly Participant[] _participants;
    private readonly HashSet<GpuBodyId> _admittedBodies;
    private readonly PassageBodyState[] _states;
    private readonly PassageSensorRead[] _fired;
    public IReadOnlyList<PassageSensorDeclaration> Sensors => _sensors;
    public IReadOnlyList<PassageBodyState> States => _states;
    public int ParticipantCount => _participants.Length;
    /// <summary>All actual passes, including multiple bodies at one aperture; network observations reduce these per sensor.</summary>
    public IReadOnlyList<PassageSensorRead> Fired => _fired;

    private PassageSensorTracker(PassageSensorDeclaration[] sensors, Participant[] participants,
        HashSet<GpuBodyId> admittedBodies, PassageBodyState[] states, PassageSensorRead[] fired)
    {
        _sensors=sensors; _participants=participants; _admittedBodies=admittedBodies; _states=states; _fired=fired;
    }

    public static PassageSensorTracker Create(ReadOnlySpan<PassageSensorDeclaration> sensors,
        PhysicsSceneDeclaration scene, PhysicsBodyReadSet bodies)
    {
        ArgumentNullException.ThrowIfNull(scene);
        var declaredBodies = new Dictionary<GpuBodyId, RigidMotionKind>(16);
        foreach (var declared in scene.Bodies) declaredBodies.Add(declared.Id, declared.Motion);
        var sensorList=sensors.ToArray();
        var seen=new HashSet<GpuPassageSensorId>(4);
        foreach(var sensor in sensorList)
        {
            sensor.Validate();
            if (!seen.Add(sensor.Id) || !declaredBodies.TryGetValue(sensor.Frame,out var motion) || motion!=RigidMotionKind.Static)
                throw new ArgumentException("Passage policies require unique identities and admitted static frames.");
        }
        Array.Sort(sensorList,(a,b)=>a.Id.Value.CompareTo(b.Id.Value));
        var participants=new List<Participant>(16);
        var readBodies=new HashSet<GpuBodyId>(16);
        for(var b=0;b<bodies.Count;b++)
        {
            var body=bodies[b];
            if (!readBodies.Add(body.Body.Id)) throw new ArgumentException("Duplicate passage participant.",nameof(bodies));
            if (!declaredBodies.TryGetValue(body.Body.Id,out var motion))
                throw new ArgumentException("Passage read names a body outside its admitted scene.",nameof(bodies));
            if (motion!=RigidMotionKind.Dynamic || sensorList.Length==0) continue;
            var radius = 0f;
            var colliderId = default(GpuColliderId);
            foreach (var collider in scene.Colliders)
            {
                if (collider.Body != body.Body.Id) continue;
                if (colliderId.Value == 0 || collider.Id.Value < colliderId.Value) colliderId = collider.Id;
                var reach = collider.Shape switch
                {
                    ColliderShapeKind.Sphere => collider.Radius.Value,
                    ColliderShapeKind.Box => MathF.Sqrt(collider.HalfExtents.X * collider.HalfExtents.X +
                        collider.HalfExtents.Y * collider.HalfExtents.Y + collider.HalfExtents.Z * collider.HalfExtents.Z),
                    ColliderShapeKind.Cylinder or ColliderShapeKind.Tube => MathF.Sqrt(collider.Radius.Value * collider.Radius.Value +
                        collider.HalfExtents.Y * collider.HalfExtents.Y),
                    ColliderShapeKind.Frustum => MathF.Sqrt(MathF.Pow(MathF.Max(collider.Radius.Value, collider.HalfExtents.Z), 2) +
                        collider.HalfExtents.Y * collider.HalfExtents.Y),
                    ColliderShapeKind.Torus => collider.Radius.Value + collider.HalfExtents.Y,
                    _ => throw new ArgumentException("Passage participants require finite collider shapes.", nameof(scene))
                };
                var offset = collider.Pose.Translation;
                radius = MathF.Max(radius, reach + new Vector3(offset.X, offset.Y, offset.Z).Length());
            }
            if (colliderId.Value == 0 || !(radius > 0f))
                throw new ArgumentException("Passage participant has no owned geometry.", nameof(scene));

            participants.Add(new(body.Body.Id,colliderId,radius,RenderPosition(body.Body.Cell,body.Body.Local)));
        }
        if (sensorList.Length!=0) foreach(var declared in declaredBodies)
            if(declared.Value==RigidMotionKind.Dynamic && !readBodies.Contains(declared.Key))
                throw new ArgumentException("Passage participant is missing.",nameof(bodies));
        participants.Sort((a,b)=>a.Body.Value.CompareTo(b.Body.Value));
        return sensorList.Length == 0 ? Empty : new(sensorList,participants.ToArray(),new(declaredBodies.Keys),[],[]);
    }

    public PassageSensorTracker Advance(SimulationTick tick, PhysicsBodyReadSet bodies, PhysicsMotionRead motion,
        Span<PassageSensorRead> outputs, out int outputCount)
    {
        if (outputs.Length<_sensors.Length) throw new ArgumentException("Passage output requires one slot per policy.",nameof(outputs));
        outputCount=0;
        if (_sensors.Length == 0) return this;
        ArgumentNullException.ThrowIfNull(motion);
        motion.ValidateBinding(bodies, tick);
        if (motion.Substeps is not (2 or 4 or 8)) throw new ArgumentException("Passage advance requires a canonical committed motion interval.", nameof(motion));
        var bodySlots=new Dictionary<GpuBodyId,int>(16);
        for(var b=0;b<bodies.Count;b++)
        {
            if (!_admittedBodies.Contains(bodies[b].Body.Id)) throw new ArgumentException("Passage read names a foreign body.",nameof(bodies));
            bodySlots.Add(bodies[b].Body.Id,b);
        }
        var participants=new Participant[_participants.Length];
        for(var b=0;b<_participants.Length;b++)
        {
            var participant=_participants[b];
            if (!bodySlots.TryGetValue(participant.Body,out var slot)) throw new ArgumentException("Passage participant is missing.",nameof(bodies));
            var body=bodies[slot];
            participants[b]=participant with { Previous=RenderPosition(body.Body.Cell,body.Body.Local) };
        }
        var previous=new Dictionary<PassagePairKey,PassageBodyState>(16);
        foreach(var state in _states) previous.Add(new(state.Sensor,state.Body),state);
        var nextStates=new List<PassageBodyState>(Math.Max(16, _states.Length));
        var fired=new List<PassageSensorRead>(4);
        uint substeps=motion.Substeps;
        foreach(var sensor in _sensors)
        {
            var detectorPos=RenderPosition(sensor.Cell,sensor.Local);
            var qInv=Quaternion.Conjugate(new(sensor.Rotation.X,sensor.Rotation.Y,sensor.Rotation.Z,sensor.Rotation.W));
            var boreRadiusSq=sensor.Radius.Value*sensor.Radius.Value;
            var observed=false;
            for(var b=0;b<_participants.Length;b++)
            {
                var participant=_participants[b];
                var local=Vector3.Transform(participant.Previous-detectorPos,qInv);
                var rearmThreshold=-sensor.RearmClearance.Value-participant.Radius;
                var found=previous.TryGetValue(new(sensor.Id,participant.Body),out var state);
                var phase=found ? state.Phase : local.X<rearmThreshold ? PassageSensorPhase.Armed : PassageSensorPhase.Unarmed;
                var passedCount=found ? state.PassedCount : 0;
                for(uint s=0;s<substeps;s++)
                {
                    var ordinal=checked(motion.FirstOrdinal+s+1);
                    var bodyPos=participants[b].Previous;
                    if(s!=substeps-1)
                    {
                        if (!motion.TrySample(participant.Body,ordinal,out var sampled))
                            throw new ArgumentException("Passage motion is missing a required participant sample.", nameof(motion));
                        bodyPos=RenderPosition(sampled.Cell,sampled.Local);
                    }
                    var next=Vector3.Transform(bodyPos-detectorPos,qInv);
                    if(next.X<rearmThreshold) phase=PassageSensorPhase.Armed;
                    if(phase==PassageSensorPhase.Armed && local.X<=0 && next.X>0)
                    {
                        var distance=next.X-local.X;
                        var t=distance>1e-6f ? Math.Clamp(-local.X/distance,0,1) : 0;
                        var y=local.Y+t*(next.Y-local.Y);
                        var z=local.Z+t*(next.Z-local.Z);
                        phase=PassageSensorPhase.Unarmed;
                        if(y*y+z*z<=boreRadiusSq)
                        {
                            passedCount=checked(passedCount+1);
                            var speed=Math.Clamp(MathF.Abs(distance*WorkshopCadenceSettings.PhysicalFrequency),0,4096);
                            var read=new PassageSensorRead(sensor.Id,sensor.Frame,participant.Body,participant.Collider,
                                PassageSensorPhase.Fired,ordinal,0,new(speed));
                            fired.Add(read);
                            if(!observed) { outputs[outputCount++]=read; observed=true; }
                        }
                    }
                    local=next;
                }
                // Upstream arming is inferable from the single previous pose. Only arming carried into the
                // clearance band needs pair memory; earned counts survive until the owning Reset.
                if(passedCount!=0 || (phase==PassageSensorPhase.Armed && local.X>=rearmThreshold))
                    nextStates.Add(new(sensor.Id,participant.Body,phase,passedCount));
            }
        }
        return new(_sensors,participants,_admittedBodies,nextStates.ToArray(),fired.ToArray());
    }

    private static Vector3 RenderPosition(CellOrigin cell, LocalPosition local) =>
        new((cell.X+local.X)/16f,(cell.Y+local.Y)/16f,(cell.Z+local.Z)/16f);
}
