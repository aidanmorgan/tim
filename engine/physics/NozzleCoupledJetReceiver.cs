using System;

namespace CuriousContraptions.Physics;

public enum JetReceiverMotion { Linear, Rotary }

/// <summary>Receiver declaration for the load-dependent jet impedance model.
/// Every receiver wrench returns to the declared field nozzle, including the
/// moment of a force at an offset sample and any rotor couple. Ambient exchange
/// is zero in this model; unloaded thrust and transported fluid are not modeled.</summary>
public sealed record NozzleCoupledJetReceiver
{
    public JetReceiverMotion Motion { get; }
    private readonly PhysicsBodyId _rotor;
    private readonly CollisionVector _axis;
    private readonly double _pitch;

    public NozzleCoupledJetReceiver()=>Motion=JetReceiverMotion.Linear;

    /// <param name="pitch">Signed metres of effective surface travel per radian.</param>
    public NozzleCoupledJetReceiver(PhysicsBodyId rotor,CollisionVector localAxis,double pitch)
    {
        var length=localAxis.Length;
        if(!localAxis.IsFinite||!double.IsFinite(length)||length<=0||!double.IsFinite(pitch)||pitch==0)
            throw new ArgumentException("Rotary jet conversion requires a finite axis and nonzero pitch.");
        Motion=JetReceiverMotion.Rotary;_rotor=rotor;_axis=localAxis/length;_pitch=pitch;
    }

    public MechanicalPowerPort CreatePort(AirJetGeometry field)
    {
        ArgumentNullException.ThrowIfNull(field);
        var linear=new PointPowerPort(field.Body,field.Source,field.LocalPoint,field.LocalDirection);
        return Motion switch
        {
            JetReceiverMotion.Linear=>linear,
            JetReceiverMotion.Rotary=>new CompositePowerPort([
                linear,
                new AlignedPowerPort(new AngularPowerPort(_rotor,field.Source,_axis,_pitch),
                    _rotor,field.Source,_axis,field.LocalDirection)
            ]),
            _=>throw new InvalidOperationException("Unsupported jet receiver motion.")
        };
    }

    public MechanicalTransferLoad CreateLoad(MechanicalTransferId id,MechanicalTransferSource source,
        AirJetGeometry field,JetTransferImpedance material,double workTolerance)=>
        new(id,source,CreatePort(field),material,workTolerance){Field=field};
}
