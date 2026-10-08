using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

public enum FrictionRegime { Sticking, Sliding }

/// <summary>Physical contact velocity maps and their convective derivatives.
/// Binding to acceleration scratch states never uses scratch accelerations as slip.</summary>
public sealed class ContactForce
{
    public ContactKinematics Kinematics { get; }
    public double NormalBias { get; }
    public CollisionVector TangentBias { get; }
    public CollisionVector PhysicalSlip { get; }
    public double Friction { get; }
    public FrictionRegime Regime { get; }

    public ContactForce(ContactKinematics kinematics,double normalBias,CollisionVector tangentBias,
        CollisionVector physicalSlip,double friction,FrictionRegime regime)
    {
        ArgumentNullException.ThrowIfNull(kinematics);
        if(!double.IsFinite(normalBias)||!tangentBias.IsFinite||!physicalSlip.IsFinite||
            !double.IsFinite(friction)||friction<0||!Enum.IsDefined(regime))
            throw new ArgumentException("Contact force requires finite derivatives, slip, friction and a defined regime.");
        Kinematics=kinematics; NormalBias=normalBias; TangentBias=tangentBias;
        PhysicalSlip=physicalSlip; Friction=friction; Regime=regime;
    }

    public static ContactForce FromGap(ContactGap gap,double friction,FrictionRegime regime)
    {
        ArgumentNullException.ThrowIfNull(gap);
        var rigid=ContactKinematics.AtPoint(gap.A,gap.B,gap.Point,gap.Normal);
        var kinematics=new ContactKinematics(gap.Normal,gap.NormalJacobian.Bind(gap.A,gap.B),rigid.TangentU,rigid.TangentV);
        return new(kinematics,gap.ConvectiveAcceleration,gap.TangentialBias,gap.SurfaceSlip,friction,regime);
    }

    internal ContactConstraint Bind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> states)=>
        ContactConstraint.ForAcceleration(Kinematics.Rebind(states),NormalBias,TangentBias,PhysicalSlip,Friction,Regime);
}
