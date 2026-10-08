using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

public enum FanParameter { Powered, Force, Reach, Width }

/// <summary>A finite cylindrical field with an explicit conserved source and impedance material.</summary>
public readonly record struct AirflowEmitter(Vector3 At,Vector3 Direction,float Reach,float Width,
    MechanicalTransferSource Supply,JetTransferImpedance Material);

public enum AirflowResponse { BodyForce, Rotary }

public readonly record struct AirflowSample(Vector3 At,float Weight,BodySlot Body,AirflowResponse Response,MechanicalReceiverSlot Slot,JointSlot? RotaryJoint);

public sealed record RotaryAirflowCapture(JointSlot Joint,RotaryCaptureMaterial Material,double ForceTolerance);

public static class AirflowNetwork
{
    public static readonly MechanicalSourceSlot SourceSlot=new();
    public const double ReferenceFlowSpeed=12;
    public const double TransferWorkTolerance=1e-8;
    public const double ControlForceTolerance=1e-7;
    public static void Step(MachineWorld world)
    {
        var assembly=world.PhysicsAssembly;
        var bodies=assembly.Bodies.ToArray().ToDictionary(body=>body.Id);
        var colliders=bodies.Keys.ToDictionary(id=>id,id=>world.Physics.Collider(id).Declaration);
        bool Enabled(SceneBodyKey key)=>colliders[assembly.Body(key).Id].Participation==CollisionParticipation.Enabled;
        var sources=world.Parts.OrderBy(p=>p.Uid,StringComparer.Ordinal)
            .Select(p=>(Part:p,Emitter:p.CreateAirflowSource(world))).Where(p=>p.Emitter.HasValue)
            .Select(p=>(p.Part,Emitter:p.Emitter!.Value))
            .Where(p=>Enabled(new(p.Part,MachinePart.RootBody))).ToArray();
        var targets=world.Parts.Select(p=>(Part:p,Samples:p.AirflowSamples))
            .Where(p=>p.Samples.Count>0&&Enabled(new(p.Part,MachinePart.RootBody))).ToArray();
        var loads=new List<MechanicalTransferLoad>();
        var rotaryLoads=new List<RotaryCaptureDeclaration>();
        for(var i=0;i<targets.Length;i++)
        {
            var target=targets[i].Part;
            var samples=targets[i].Samples;
            if(samples.Any(s=>s.Body is null||s.Slot is null||!s.At.IsFinite()||!float.IsFinite(s.Weight)||s.Weight<=0||!Enum.IsDefined(s.Response))||Mathf.Abs(samples.Sum(s=>s.Weight)-1)>.0001f)
                throw new ArgumentException("Airflow sample weights must be positive, finite and sum to one.");
            var captures=target.RotaryAirflowCaptures;
            if(captures.Any(capture=>capture is null||capture.Joint is null||capture.Material is null)||
                captures.Select(capture=>capture.Joint).Distinct().Count()!=captures.Count)
                throw new ArgumentException("Rotary airflow requires distinct typed capture joints.");
            var branches=captures.ToDictionary(capture=>capture.Joint,_=>new List<RotaryCaptureTransfer>());
            foreach(var sample in samples)
            {
                if(sample.Response==AirflowResponse.Rotary
                    ?sample.RotaryJoint is null||!branches.ContainsKey(sample.RotaryJoint)
                    :sample.RotaryJoint is not null)
                    throw new ArgumentException("Airflow response does not match its rotary binding.");
                var key=new SceneBodyKey(target,sample.Body);
                if(!Enabled(key))continue;
                if(sample.Response==AirflowResponse.BodyForce&&assembly.Body(key).MotionType!=PhysicsMotionType.Dynamic)
                    throw new ArgumentException("Body-force airflow samples require a dynamic body.");
                foreach(var source in sources)
                {
                    if(source.Part==target)continue;
                    var exclusions=bodies.Values.Where(body=>
                    {
                        var owner=assembly.Owner(body.Id);
                        return owner is not null&&(owner.PhysicsOwner==source.Part.PhysicsOwner||owner.PhysicsOwner==target.PhysicsOwner);
                    }).Select(body=>body.Id);
                    var emitter=source.Emitter;
                    var field=new AirJetGeometry(assembly.Body(new(source.Part,MachinePart.RootBody)).Id,
                        assembly.Body(key).Id,SceneGeometryAdapter.CaptureVector(emitter.At-source.Part.LocalCenterOfMass),
                        SceneGeometryAdapter.CaptureVector(emitter.Direction),SceneGeometryAdapter.CaptureVector(sample.At),
                        emitter.Reach,emitter.Width,exclusions);
                    var sourceKey=new SceneMechanicalSourceKey(new(source.Part,MachinePart.RootBody),SourceSlot);
                    var receiverKey=new SceneMechanicalReceiverKey(key,sample.Slot);
                    var weight=(double)world.Pressure*sample.Weight;
                    var material=new JetTransferImpedance(emitter.Material.Conductance*weight,
                        emitter.Material.MaximumForce*weight);
                    var id=world.TransferBindings.Transfer(sourceKey,receiverKey);
                    switch(sample.Response)
                    {
                        case AirflowResponse.BodyForce:
                            loads.Add(new NozzleCoupledJetReceiver().CreateLoad(id,emitter.Supply,field,
                                material,TransferWorkTolerance));
                            break;
                        case AirflowResponse.Rotary:
                            branches[sample.RotaryJoint!].Add(new(id,emitter.Supply,field,material,TransferWorkTolerance));
                            break;
                        default:throw new ArgumentOutOfRangeException(nameof(sample));
                    }
                }
            }
            foreach(var capture in captures)
                rotaryLoads.Add(new(world.PhysicsAssembly.JointId(new(target,capture.Joint)),
                    capture.Material,branches[capture.Joint],capture.ForceTolerance));
        }
        foreach(var load in loads)world.AddTransferLoad(load);
        foreach(var load in rotaryLoads)world.AddRotaryLoad(load);
    }

    /// <summary>Mean receiver force from the last committed physics substep.
    /// Reads only accepted transfer impulses; callers supply that substep's duration.</summary>
    public static Vector3 ReadReceiverForce(MachineWorld world,MachinePart target,double duration)
    {
        if(!double.IsFinite(duration)||duration<=0)throw new ArgumentOutOfRangeException(nameof(duration));
        var impulse=default(CollisionVector);
        foreach(var sample in target.AirflowSamples)
        {
            var receiver=new SceneMechanicalReceiverKey(new(target,sample.Body),sample.Slot);
            var body=world.PhysicsAssembly.Body(receiver.Body).Id;
            foreach(var report in world.Physics.TransferBodyImpulses)
                if(report.Key.Role==TransferPortRole.Receiver&&report.Key.Body==body&&
                    world.TransferBindings.Receiver(report.Key.Transfer)==receiver)
                    impulse+=report.Linear;
        }
        var force=impulse/duration;
        var result=new Vector3((float)force.X,(float)force.Y,(float)force.Z);
        if(!result.IsFinite())throw new InvalidOperationException("Airflow force exceeds the presentation range.");
        return result;
    }
}
