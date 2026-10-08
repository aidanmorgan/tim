using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public enum TransferSweepStage { Iteration, Acceptance }

/// <summary>Paired one-way mechanical transfer. Both actions are evaluated
/// together; the source cannot be reused without receiving every branch reaction.</summary>
public sealed record MechanicalTransferLoad
{
    public MechanicalTransferId Id { get; }
    public MechanicalTransferSource Source { get; }
    public MechanicalPowerPort Receiver { get; }
    public JetTransferImpedance Impedance { get; }
    public double WorkTolerance { get; }
    /// <summary>Optional spatial gate. An absent field declares a direct transfer;
    /// a present field gates the entire paired action and its source demand.</summary>
    public AirJetGeometry? Field { get; init; }

    public MechanicalTransferLoad(MechanicalTransferId id,MechanicalTransferSource source,MechanicalPowerPort receiver,
        JetTransferImpedance impedance,double workTolerance)
    {
        ArgumentNullException.ThrowIfNull(source);ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(impedance);
        if(!double.IsFinite(workTolerance)||workTolerance<=0||workTolerance*.25==0)
            throw new ArgumentOutOfRangeException(nameof(workTolerance));
        Id=id;Source=source;Receiver=receiver;Impedance=impedance;WorkTolerance=workTolerance;
    }

    public void Validate(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,IReadOnlyList<PhysicsJoint> joints,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)
    {
        Source.Participates(bodies,colliders);Source.Bind(bodies,joints);Receiver.Bind(bodies,joints);Field?.Validate(bodies,colliders);
    }


    public ScalarSweepResult Sweep(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyList<PhysicsJoint> joints,IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,
        double duration,double velocityTolerance,IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders,
        double positionTolerance,TransferSweepStage stage)
    {
        if(!Enum.IsDefined(stage))throw new ArgumentOutOfRangeException(nameof(stage));
        if(!double.IsFinite(duration)||duration<0||!double.IsFinite(velocityTolerance)||velocityTolerance<=0)
            throw new ArgumentOutOfRangeException(nameof(duration));
        var source=Source.CaptureSpeed(bodies,joints,paths,duration);
        var receiver=Source.Kind==TransferSupplyKind.Mechanical&&Source.MechanicalPort==Receiver?source:MechanicalPortSpeedPath.Capture(Receiver,bodies,joints,paths);
        if(duration>Math.Min(source.Duration,receiver.Duration))throw new ArgumentOutOfRangeException(nameof(duration));
        var result=new ScalarSweepResult(ScalarSweepStatus.Clear,duration,0);
        if(!Source.Participates(bodies,colliders)||Impedance.Conductance==0||Impedance.MaximumForce==0)return result;
        if(Field is not null)
        {
            var field=Field.Sweep(bodies,colliders,paths,duration,positionTolerance);
            result=new(field.Status,field.Time,field.Iterations);
            // The field sweep owns the next possible activation. While this
            // branch is unexposed, its slip and flow cannot change any force.
            if(!Field.IsExposed(bodies,colliders))return result;
        }
        foreach(var boundary in Enum.GetValues<MechanicalTransferBoundary>())
        {
            // The coupled endpoint inclusion owns source-flow switching during
            // iteration; certify that boundary on the converged path. Other
            // constitutive and geometric boundaries still restrict trial paths.
            if(stage==TransferSweepStage.Iteration&&boundary==MechanicalTransferBoundary.SourceFlow)continue;
            // An overflowing threshold cannot be reached by any supported finite slip.
            if(boundary==MechanicalTransferBoundary.Saturation&&
                !double.IsFinite(Impedance.MaximumForce/Impedance.Conductance))continue;
            var hit=ScalarBoundarySweep.Cast(new MechanicalTransferBoundaryPath(source,receiver,boundary,Impedance,velocityTolerance*.125),
                result.Time,velocityTolerance*.125,velocityTolerance*.5);
            result=result with {Iterations=checked(result.Iterations+hit.Iterations)};
            if(hit.Status==ScalarSweepStatus.Boundary)result=result with {Status=hit.Status,Time=hit.Time};
        }
        return result;
    }

    internal MechanicalTransferEvaluation EvaluateDemand(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyList<PhysicsJoint> joints,IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)
    {
        var source=Source.Bind(bodies,joints);var receiver=Receiver.Bind(bodies,joints);
        var speed=Source.Speed(source);var receivingSpeed=receiver.Speed;
        if(!double.IsFinite(speed)||!double.IsFinite(receivingSpeed))
            throw new InvalidOperationException("Mechanical transfer speed exceeds numeric range.");
        var response=!Source.Participates(bodies,colliders)||Source.Kind==TransferSupplyKind.StoredFlow&&speed<=0||
            (Field is not null&&!Field.IsExposed(bodies,colliders))
            ?default:Impedance.Evaluate(Math.Max(0,speed),receivingSpeed);
        return new(Id,Source.Id,Source.Kind,source,receiver,response,WorkTolerance,speed,receivingSpeed);
    }
}

/// <summary>Positive source extraction and receiver delivery are measured separately.
/// PairedWork is receiver minus source; its dissipated component is transfer loss.</summary>
public readonly record struct PredictedTransferWork(MechanicalTransferId Transfer,MechanicalSourceId Source,
    WrenchPathWorkResult SourceExtraction,WrenchPathWorkResult ReceiverDelivery,WrenchPathWorkResult PairedWork,double Impulse);

/// <summary>One stage's paired wrench. Certification uses original bodies and
/// captured paths, not the sampled bodies retained by the stage gradient.</summary>
public sealed class MechanicalTransferEvaluation
{
    public ConstraintGradient Gradient { get; }
    public JetTransferResponse Response { get; }
    private readonly double _workTolerance;
    private readonly double _sourceSpeed,_receiverSpeed;
    private readonly MechanicalTransferId _id;
    private readonly MechanicalSourceId _sourceId;
    private readonly TransferSupplyKind _kind;
    private readonly ConstraintGradient? _source;
    private readonly ConstraintGradient _receiver;
    internal MechanicalTransferEvaluation(MechanicalTransferId id,MechanicalSourceId sourceId,TransferSupplyKind kind,
        ConstraintGradient? source,ConstraintGradient receiver,JetTransferResponse response,double workTolerance,double sourceSpeed,double receiverSpeed)
    {
        _sourceSpeed=sourceSpeed;_receiverSpeed=receiverSpeed;
        _id=id;_sourceId=sourceId;_kind=kind;_source=source;_receiver=receiver;
        if((kind==TransferSupplyKind.Mechanical)!=(source is not null)||!Enum.IsDefined(kind))
            throw new ArgumentException("Source gradient does not match supply kind.");
        var terms=receiver.Terms.ToArray().Concat((source is null?Array.Empty<ConstraintTerm>():source.Terms.ToArray())
            .Select(term=>new ConstraintTerm(term.Body,-term.Linear,-term.Angular))).ToArray();
        Gradient=new(terms);Response=response;_workTolerance=workTolerance;
    }

    internal MechanicalTransferEvaluation Scale(double scale)
    {
        if(!double.IsFinite(scale)||scale<0||scale>1)throw new ArgumentOutOfRangeException(nameof(scale));
        var force=Response.Force*scale;
        if(scale>0&&Response.Force>0&&force==0)
            throw new InvalidOperationException("Scaled transfer force is below the supported numeric range.");
        double Power(double speed)
        {
            var power=force*speed;
            if(!double.IsFinite(power)||(force>0&&speed!=0&&power==0))
                throw new InvalidOperationException("Scaled transfer power exceeds the supported numeric range.");
            return power;
        }
        var response=force==0?default:new JetTransferResponse(force,Power(_sourceSpeed),
            Power(_receiverSpeed),Power(_sourceSpeed-_receiverSpeed));
        return new(_id,_sourceId,_kind,_source,_receiver,response,_workTolerance,_sourceSpeed,_receiverSpeed);
    }

    private double Impulse(double duration)
    {
        if(!double.IsFinite(duration)||duration<0)throw new ArgumentOutOfRangeException(nameof(duration));
        var impulse=Response.Force*duration;
        if(!double.IsFinite(impulse)||(Response.Force>0&&duration>0&&impulse==0))
            throw new InvalidOperationException("Transfer impulse exceeds the supported numeric range.");
        return impulse;
    }

    internal MechanicalTransferBodyImpulse[] BodyImpulses(double duration)
    {
        var impulse=Impulse(duration);
        var result=new List<MechanicalTransferBodyImpulse>();
        void Include(ConstraintGradient gradient,TransferPortRole role,double signedImpulse)
        {
            foreach(var term in gradient.Terms)
                result.Add(new(new(_id,role,term.Body.Id),_sourceId,
                    term.Linear*signedImpulse,term.Angular*signedImpulse));
        }
        if(_source is not null)Include(_source,TransferPortRole.Source,-impulse);
        Include(_receiver,TransferPortRole.Receiver,impulse);
        return result.ToArray();
    }

    public PredictedTransferWork Certify(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> original,
        IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double duration,double allocatedWorkTolerance)
    {
        if(!double.IsFinite(allocatedWorkTolerance)||allocatedWorkTolerance<=0||allocatedWorkTolerance>_workTolerance||allocatedWorkTolerance/32==0)
            throw new ArgumentOutOfRangeException(nameof(allocatedWorkTolerance));
        ArgumentNullException.ThrowIfNull(original);ArgumentNullException.ThrowIfNull(paths);
        WrenchPathWorkResult Measure(ConstraintGradient? gradient,double constantPower)
        {
        var participants=new List<WrenchPathTerm>();
        foreach(var term in gradient is null?ReadOnlySpan<ConstraintTerm>.Empty:gradient.Terms)
        {
            if(!original.TryGetValue(term.Body.Id,out var body)||body is null||body.Id!=term.Body.Id||
                !paths.TryGetValue(term.Body.Id,out var path)||path is null)
                throw new ArgumentException("Transfer certification requires its original bodies and paths.");
            participants.Add(new(body,path,new(term.Linear*Response.Force,term.Angular*Response.Force)));
        }
        return WrenchPathWork.Measure(participants,duration,allocatedWorkTolerance/32,constantPower);
        }
        var algebraicPower=_kind==TransferSupplyKind.StoredFlow?Response.SourcePower:0;
        var work=Measure(Gradient,-algebraicPower);
        if(work.Supplied+work.SuppliedErrorBound>allocatedWorkTolerance*.25)
            throw new MechanicalTransferWorkException(work,allocatedWorkTolerance*.25);
        return new(_id,_sourceId,Measure(_source,algebraicPower),Measure(_receiver,0),work,Impulse(duration));
    }
}

public sealed class MechanicalTransferWorkException : InvalidOperationException
{
    public WrenchPathWorkResult Work { get; }
    public double Tolerance { get; }
    internal MechanicalTransferWorkException(WrenchPathWorkResult work,double tolerance):
        base("Mechanical transfer path creates work beyond its tolerance.")
    {
        Work=work;Tolerance=tolerance;
    }
}
